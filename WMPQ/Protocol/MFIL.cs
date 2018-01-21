using System;
using System.Collections.Generic;
using System.IO;

namespace WMPQ.Protocol
{
    public sealed class MFIL
    {
        public int Version { get; }
        public Dictionary<string, string> ServerPath { get; } = new Dictionary<string, string>();

        public class Record
        {
            public string File { get; set; }
            public long Size { get; set; }
            public int Version { get; set; }
            public int Flags { get; set; }
            public string Path { get; set; }
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
                            var nextLine = reader.ReadLine();
                            if (nextLine != null)
                                ServerPath[lineTokens[1]] = nextLine.Split('=')[1];
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

            var blockLines = new Dictionary<string, string>();

            while (reader.Peek() != 'f') // file=....
            {
                var line = reader.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;

                var lineTokens = line.Split('=');
                blockLines[lineTokens[0].Trim()] = lineTokens[1].Trim();
            }

            if (!fileName.EndsWith(".MPQ") && Version == 2)
                return;

            if (blockLines.ContainsKey("size"))
                record.Size = long.Parse(blockLines["size"]);

            if (blockLines.ContainsKey("version"))
                record.Version = int.Parse(blockLines["version"]);

            if (blockLines.ContainsKey("flags"))
                record.Flags = int.Parse(blockLines["flags"]);

            Records.Add(record);
        }
    }
}
