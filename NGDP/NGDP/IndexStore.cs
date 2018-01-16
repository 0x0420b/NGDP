using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NGDP.Network;
using NGDP.Utilities;

namespace NGDP.NGDP
{
    public class IndexStore
    {
        public Dictionary<int, Record> Records { get; } = new Dictionary<int, Record>(200000);

        public byte[][] Archives { get; private set; }

        public bool TryGetValue(byte[] hash, out Record record) => TryGetValue(ByteArrayComparer.Instance.GetHashCode(hash), out record);
        public bool TryGetValue(int hashCode, out Record record) => Records.TryGetValue(hashCode, out record);

        public int Count => Records.Count;
        public void Clear() => Records.Clear();

        public void FromStream(string host, byte[][] archives)
        {
            Archives = archives;

            Parallel.ForEach(Archives, archiveHash =>
            {
                using (var client = new AsyncClient(host))
                {
                    client.LogRequest = false;
                    client.Send($"/tpr/wow/data/{archiveHash[0]:x2}/{archiveHash[1]:x2}/{archiveHash.ToHexString()}.index");

                    if (client.Failed)
                        return;

                    using (var reader = new BinaryReader(client.Stream))
                    {
                        var fileData = reader.ReadBytes(client.ContentLength);
                        using (var memoryStream = new MemoryStream(fileData, false))
                        using (var chunkReader = new EndianBinaryReader(EndianBitConverter.Little, memoryStream))
                        {
                            memoryStream.Seek(-12, SeekOrigin.End);
                            var recordCount = chunkReader.ReadInt32();

                            memoryStream.Seek(0, SeekOrigin.Begin);

                            chunkReader.BitConverter = EndianBitConverter.Big;
                            while (recordCount != 0)
                            {
                                // ReSharper disable once UseObjectOrCollectionInitializer
                                var record = new Record();

                                record.Hash = chunkReader.ReadBytes(16);
                                if (record.Hash.All(b => b == 0))
                                {
                                    if (memoryStream.Position % 4096 == 0)
                                        continue;

                                    var chunkPosition = (memoryStream.Position / 4096) * 4096;
                                    memoryStream.Position = chunkPosition + 4096; // Skip to next chunk
                                }
                                else
                                {
                                    record.Size = chunkReader.ReadInt32();
                                    record.Offset = chunkReader.ReadInt32();
                                    record.ArchiveIndex = Array.IndexOf(Archives, archiveHash);

                                    Records[ByteArrayComparer.Instance.GetHashCode(record.Hash)] = record;

                                    --recordCount;
                                }
                            }
                        }
                    }
                }
            });
        }

        public class Record
        {
            public byte[] Hash { get; set; }
            public int ArchiveIndex { get; set; }
            public int Size { get; set; }
            public int Offset { get; set; }
        }
    }
}
