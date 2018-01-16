using System.Collections.Generic;
using System.IO;
using NGDP.Utilities;

namespace NGDP.NGDP
{
    public class Root
    {
        private Dictionary<ulong, Record> Records { get; } = new Dictionary<ulong, Record>();

        public int Count => Records.Count;
        public void Clear() => Records.Clear();

        public bool TryGetByHash(ulong hash, out Record record) => Records.TryGetValue(hash, out record);

        public bool Loaded { get; private set; } = false;

        public void FromStream(string host, string queryString)
        {
            using (var blte = new BLTE(host))
            {
                blte.Send(queryString);
                if (blte.Failed)
                    return;

                using (var fileReader = new EndianBinaryReader(EndianBitConverter.Little, blte.ReadToEnd()))
                {
                    while (fileReader.BaseStream.Position < fileReader.BaseStream.Length)
                    {
                        var recordCount = fileReader.ReadInt32();
                        
                        fileReader.BaseStream.Seek(4 + 4, SeekOrigin.Current); // Skip flags
                        // var contentFlags = fileReader.ReadInt32();
                        // var localeFlags = fileReader.ReadInt32();

                        var records = new Record[recordCount];
                        // var fileDataIndex = 0;

                        for (var i = 0; i < recordCount; ++i)
                        {
                            // ReSharper disable once UseObjectOrCollectionInitializer
                            records[i] = new Record();
                            // records[i].LocaleFlags = localeFlags;
                            // records[i].ContentFlags = contentFlags;

                            fileReader.BaseStream.Seek(4, SeekOrigin.Current); // Skip FileDataID
                            // records[i].FileDataID = fileDataIndex + fileReader.ReadInt32();
                            // fileDataIndex = records[i].FileDataID + 1;
                        }

                        for (var i = 0; i < recordCount; ++i)
                        {
                            records[i].MD5 = ByteArrayComparer.Instance.GetHashCode(fileReader.ReadBytes(16));
                            var recordHash = fileReader.ReadUInt64();

                            Records[recordHash] = records[i];
                        }
                    }
                }
            }

            Loaded = true;
        }

        public class Record
        {
            public int MD5 { get; set; }
            /// public ulong Hash { get; set; }
            // public int FileDataID { get; set; }
            // public int ContentFlags { get; set; }
            // public int LocaleFlags { get; set; }
        }
    }
}
