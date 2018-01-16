using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using NGDP.Network;
using NGDP.Utilities;

namespace NGDP.NGDP
{
    public class BLTE : Stream
    {
        private AsyncClient _client;
        private EndianBinaryReader _networkReader;
        private MemoryStream _dataStream = new MemoryStream();
        private Chunk[] Chunks { get; set; }
        private int _currentChunk;

        public string URL => _client.URL;

        public int StatusCode => _client?.StatusCode ?? 400;
        public int ContentLength => _client?.ContentLength ?? 0;

        public bool Failed => _client?.Failed ?? true;

        public int DecompressedLength => Chunks.Sum(chunk => chunk.Header.DecompressedSize);
        public WebHeaderCollection ResponseHeaders => _client.ResponseHeaders;

        #region Stream override
        public override bool CanRead => _dataStream.CanRead;
        public override bool CanSeek => _dataStream.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _dataStream.Length;
        public override long Position
        {
            get { return _dataStream.Position; }
            set { Seek(value, SeekOrigin.Begin); }
        }

        public override void Flush()
        {
            throw new InvalidOperationException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (origin)
            {
                case SeekOrigin.Begin:
                    CheckReadNeeded(offset - _dataStream.Position);
                    break;
                case SeekOrigin.Current:
                    CheckReadNeeded(offset);
                    break;
                case SeekOrigin.End:
                    CheckReadNeeded(_dataStream.Length - offset);
                    break;
            }
            return _dataStream.Seek(offset, origin);
        }

        public override void SetLength(long value) => _dataStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckReadNeeded(count);
            return _dataStream.Read(buffer, offset, count);
        }

        public new void CopyTo(Stream stream)
        {
            _dataStream.CopyTo(stream);
        }

        public void PipeTo(Stream stream)
        {
            var oldPosition = _dataStream.Position;
            ReadToEnd();
            _dataStream.Position = 0;
            CopyTo(stream);
            _dataStream.Position = oldPosition;
        }

        public Stream ReadToEnd()
        {
            while (_currentChunk < Chunks.Length)
                ReadChunk();
            _dataStream.Position = 0;
            return _dataStream;
        }

        private void CheckReadNeeded(long count)
        {
            if (count <= 0)
                return;

            // Save current read position before we start writing to the stream again ...
            var readPosition = _dataStream.Position;

            while (count > 0 && _dataStream.Length - _dataStream.Position < count)
            {
                var downloadedDataSize = ReadChunk((int) count);
                if (downloadedDataSize == 0)
                    break;

                count -= downloadedDataSize;
            }

            // .. And restore it
            _dataStream.Position = readPosition;
        }

        #endregion

        public BLTE(string host)
        {
            _client = new AsyncClient(host);
        }

        public void AddHeader(string headerKey, string headerValue) => _client.RequestHeaders.Add(headerKey, headerValue);

        public void Send(string queryString)
        {
            _client.Send(queryString);

            if (_client.Failed)
                return;

            // Don't wrap in an using statement.
            _networkReader = new EndianBinaryReader(EndianBitConverter.Little, _client.Stream);

            // Header
            var signature = _networkReader.ReadInt32();
            if (signature != 0x45544c42)
                throw new InvalidOperationException($"File {queryString.Split('/').Last()} is not a valid BTLE archive!");

            _networkReader.BitConverter = EndianBitConverter.Big;
            var headerSize = _networkReader.ReadInt32();

            var chunkCount = 1;
            if (headerSize > 0)
            {
                var flagsCount = _networkReader.ReadBytes(4);
                chunkCount = (flagsCount[1] << 16) | (flagsCount[2] << 8) | flagsCount[3];
            }

            if (chunkCount == 0)
                throw new InvalidOperationException($"Incorrect number of chunks in BLTE file {queryString.Split('/').Last()}");

            if (headerSize > 0)
            {
                Chunks = new Chunk[chunkCount];
                for (var i = 0; i < chunkCount; ++i)
                {
                    Chunks[i] = new Chunk();
                    Chunks[i].Header.CompressedSize = _networkReader.ReadInt32() - 1;
                    Chunks[i].Header.DecompressedSize = _networkReader.ReadInt32();
                    Chunks[i].Header.Checksum = _networkReader.ReadBytes(16);
                }
            }
            else
            {
                Chunks = new Chunk[1];
                Chunks[0] = new Chunk();
                Chunks[0].Header.CompressedSize = _client.ContentLength - 8;
                Chunks[0].Header.DecompressedSize = _client.ContentLength - 8 - 1;
            }

            _currentChunk = 0;

            _dataStream.Capacity = DecompressedLength;
        }

        /// <summary>
        /// Reads a chunk from the network stream.
        /// In the case of a large non-zlibbed nor recursive nor encrypted chunk,
        /// and if <see cref="maxDataRead"/> is larger than 0, the code will only
        /// query what is needed from the network stream.
        /// </summary>
        /// <param name="maxDataRead">Max amount of data to read.</param>
        /// <returns>The amount of bytes read.</returns>
        private int ReadChunk(int maxDataRead = -1)
        {
            if (Chunks == null || _currentChunk >= Chunks.Length)
                return 0;

            Debug.Assert(Chunks[_currentChunk].Header.CompressedSize != 0,
                $"(Chunks[{_currentChunk}].Header.CompressedSize = {Chunks[_currentChunk].Header.CompressedSize}) == 0");

            if (Chunks[_currentChunk].EncodingMode == 0xFF)
                Chunks[_currentChunk].EncodingMode = _networkReader.ReadByte();
            switch (Chunks[_currentChunk].EncodingMode)
            {
                case (byte) 'N':
                {
                    // Compute the amount of bytes read. If maxDataRead = -1, read the whole block.
                    // If trying to read more than chunk size, cap to it (obviously)
                    var readSize = maxDataRead;
                    if (readSize <= 0 || readSize > Chunks[_currentChunk].Header.CompressedSize)
                        readSize = Chunks[_currentChunk].Header.CompressedSize;

                    var blockData = _networkReader.ReadBytes(readSize);
                    _dataStream.Write(blockData, 0, blockData.Length);

                    // Update the size of remaining data in header.
                    Chunks[_currentChunk].Header.CompressedSize -= blockData.Length;

                    // Move on to next chunk if we're done with this block.
                    if (Chunks[_currentChunk].Header.CompressedSize == 0)
                        _currentChunk += 1;
                    return blockData.Length;
                }
                case (byte) 'Z':
                {
                    // Save old read position.
                    var oldPosition = _dataStream.Position;

                    var blockData = _networkReader.ReadBytes(Chunks[_currentChunk].Header.CompressedSize);
                    using (var memoryStream = new MemoryStream(blockData, 2, blockData.Length - 2))
                    using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
                        deflateStream.CopyTo(_dataStream);

                    // Advance to next chunk
                    Chunks[_currentChunk].Header.CompressedSize = 0;
                    _currentChunk += 1;

                    // Return the amount of bytes actually written to the inflated stream
                    return (int)(_dataStream.Position - oldPosition);
                }
                case (byte) 'E':
                    throw new NotImplementedException("Salsa20, ARC4 or RC4 encryptions are not implemented!");
                case (byte) 'F':
                    throw new NotImplementedException("Recursive BLTE parsing is not implemented!");
                default:
                    throw new InvalidOperationException($"Encryption type {(char) Chunks[_currentChunk].EncodingMode} is not implemented!");
            }

            // Dead code here.
        }

        private class ChunkInfoEntry
        {
            public int DecompressedSize { get; set; }
            public int CompressedSize { get; set; }
            public byte[] Checksum { get; set; }
        }

        private class Chunk
        {
            public ChunkInfoEntry Header { get; } = new ChunkInfoEntry();
            public byte EncodingMode { get; set; } = 0xFF;
        }
    }
}
