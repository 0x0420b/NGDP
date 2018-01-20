using System;
using System.Collections.Generic;
using System.IO;

namespace WMPQ.Protocol
{
    public sealed class MFIL
    {
        public int Version { get; }
        public string ServerPath { get; } = string.Empty;

        public class Record
        {
            public string File { get; set; }
            public long Size { get; set; }
            public int Version { get; set; }
            public int Flags { get; set; }
        }

        public List<Record> Records { get; } = new List<Record>();

        public MFIL(Stream dataStream)
        {
            using (var reader = new StreamReader(dataStream))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    var lineTokens = line.Split('=');
                    switch (lineTokens[0])
                    {
                        case "version":
                            Version = int.Parse(lineTokens[1]);
                            break;
                        case "serverpath":
                            var hasServerPath = lineTokens[1] != "base";
                            var nextLine = reader.ReadLine();
                            if (hasServerPath && nextLine != null)
                                ServerPath = nextLine.Split('=')[1];
                            break;
                        case "file":
                            LoadFileRecord(lineTokens[1], reader);
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }
        }

        private void LoadFileRecord(string fileName, TextReader reader)
        {
            var record = new Record {File = fileName};

            reader.ReadLine(); // Name duplicata
            var sizeLine = reader.ReadLine();
            var versionLine = reader.ReadLine();
            var flagsLine = reader.ReadLine();
            reader.ReadLine(); // Path (always base?)

            record.Size = long.Parse(sizeLine.Split('=')[1]);
            record.Version = int.Parse(versionLine.Split('=')[1]);
            record.Flags = int.Parse(flagsLine.Split('=')[1]);

            Records.Add(record);
        }
    }
}
