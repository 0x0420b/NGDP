using System.Collections.Generic;
using System.IO;
using System.Linq;
using NGDP.Network;

namespace NGDP.Patch
{
    public class CDNs : AsyncClient
    {
        public Dictionary<string, Record> Records { get; } = new Dictionary<string, Record>();

        public CDNs(string channel) : base("us.patch.battle.net", 1119)
        {
            LogRequest = false;
            Send($"/{channel}/cdns");

            using (var reader = new StreamReader(Stream))
            {
                // Skip header
                // ReSharper disable once RedundantAssignment
                var line = reader.ReadLine();
                while ((line = reader.ReadLine()) != null)
                {
                    var lineTokens = line.Split('|');

                    Records[lineTokens[0]] = new Record()
                    {
                        Name = lineTokens[0],
                        Path = lineTokens[1],
                        Hosts = lineTokens[2].Split(' ').Where(h => !h.Contains("edgecast")).ToArray()
                    };
                }
            }
        }

        public struct Record
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string[] Hosts { get; set; }
            // public string ConfigPath { get; set; }
        }
    }
}
