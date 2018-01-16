using System.Collections.Generic;
using System.IO;
using System.Linq;
using NGDP.Utilities;

namespace NGDP.NGDP
{
    public class Install
    {
        public bool Loaded { get; private set; }

        public class Record
        {
            public ulong NameHash { get; set; }
            public int MD5 { get; set; } // byte[] normally, but we hashcode it
            public int Size { get; set; }
        }

        private List<Record> Records { get; } = new List<Record>();

        public int Count => Records.Count;
        public void Clear() => Records.Clear();

        public void FromNetworkResource(string host, string queryString)
        {
            using (var blte = new BLTE(host))
            {
                blte.Send(queryString);

                if (!blte.Failed)
                    FromStream(blte);
                else
                    Loaded = false;
            }
        }

        public void FromStream(Stream stream)
        {
            using (var reader = new EndianBinaryReader(EndianBitConverter.Big, stream))
            {
                // skip signature and two unk bytes
                reader.Seek(2 + 2, SeekOrigin.Begin);

                var tagCount = reader.ReadInt16();
                var entryCount = reader.ReadInt32();

                var numMaskBytes = entryCount / 8 + (entryCount % 8 > 0 ? 1 : 0);

                for (var i = 0; i < tagCount; ++i)
                {
                    reader.ReadCString(); // Tag name
                    reader.Seek(numMaskBytes + 2, SeekOrigin.Current);
                }

                for (var i = 0; i < entryCount; ++i)
                {
                    // ReSharper disable once UseObjectOrCollectionInitializer
                    var record = new Record();

                    record.NameHash = JenkinsHashing.Instance.ComputeHash(reader.ReadCString());
                    record.MD5 = ByteArrayComparer.Instance.GetHashCode(reader.ReadBytes(16));
                    record.Size = reader.ReadInt32();

                    Records.Add(record);
                }
            }

            Loaded = Records.Count != 0;
        }

        public IEnumerable<Record> GetEntriesByName(string fileName) =>
            Records.Where(entry => entry.NameHash == JenkinsHashing.Instance.ComputeHash(fileName));

        public IEnumerable<Record> GetEntriesByHash(ulong fileHash) =>
            Records.Where(entry => entry.NameHash == fileHash);

        public bool HasFile(string fileName) =>
            Records.Any(r => r.NameHash == JenkinsHashing.Instance.ComputeHash(fileName));
            
        public bool HasFile(ulong fileHash) =>
            Records.Any(r => r.NameHash == fileHash);
    }
}
