using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
using NGDP.Utilities;

namespace NGDP.NGDP
{
    public class Encoding
    {
        private Dictionary<int, Entry> Records { get; } = new Dictionary<int, Entry>(50000);

        public int Count => Records.Count;

        public bool Loaded { get; private set; }

        public void FromDisk(string fileName)
        {
            using (var reader = File.Open(fileName, FileMode.Open, FileAccess.Read))
                FromStream(reader);
        }

        public void FromNetworkResource(string host, string queryString)
        {
            using (var blte = new BLTE(host))
            {
                blte.Send(queryString);
                if (!blte.Failed)
                    FromStream(blte);
            }
        }

        public bool TryGetValue(int hashCode, out Entry value)
            => Records.TryGetValue(hashCode, out value);

        public bool TryGetValue(byte[] hash, out Entry value)
            => Records.TryGetValue(ByteArrayComparer.Instance.GetHashCode(hash), out value);

        public void Clear() => Records.Clear();

        private void FromStream(Stream fileStream)
        {
            using (var reader = new EndianBinaryReader(EndianBitConverter.Little, fileStream))
            {
                if (reader.ReadInt16() != 0x4E45) // EN
                    return;

                reader.ReadByte(); // Unknown
                var checksumSize = reader.ReadByte();
                reader.ReadByte(); // Checksum size in table B, which we won't be using
                reader.ReadUInt16(); // Flags for table A
                reader.ReadUInt16(); // Flags for table B, which we won't be using

                reader.BitConverter = EndianBitConverter.Big;
                var tableEntryCount = reader.ReadUInt32();
                reader.ReadUInt32(); // Entry count for table B

                reader.ReadByte(); // Unknown
                var stringBlockSize = reader.ReadInt32(); // String block size (which we won't use)

                // Skip string block and hash headers
                reader.Seek(stringBlockSize + (int)((16 + 16) * tableEntryCount), SeekOrigin.Current);

                var chunkStart = reader.BaseStream.Position;

                reader.BitConverter = EndianBitConverter.Little;

                for (var i = 0; i < tableEntryCount; ++i)
                {
                    ushort keyCount;
                    while ((keyCount = reader.ReadUInt16()) != 0)
                    {
                        reader.BitConverter = EndianBitConverter.Big;

                        // ReSharper disable once UseObjectOrCollectionInitializer
                        var encoding = new Entry();

                        reader.BaseStream.Position += 4; // File size

                        var encodingHash = reader.ReadBytes(checksumSize);
                        encoding.Key = reader.ReadBytes(checksumSize);

                        reader.Seek(checksumSize * (keyCount - 1), SeekOrigin.Current);

                        Records[ByteArrayComparer.Instance.GetHashCode(encodingHash)] = encoding;
                        reader.BitConverter = EndianBitConverter.Little;
                    }

                    const int CHUNK_SIZE = 4096;
                    reader.Seek(CHUNK_SIZE - (int)((reader.BaseStream.Position - chunkStart) % CHUNK_SIZE), SeekOrigin.Current);
                }
            }

            Loaded = true;

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced);
        }

        public class Entry
        {
            // public uint Filesize { get; set; }
            // public byte[] Hash { get; set; }
            public byte[] Key { get; set; }
        }
    }
}
