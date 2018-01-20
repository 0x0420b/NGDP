using System;
using System.Collections.Generic;
using System.IO;
using NGDP.Network;
using NGDP.Utilities;

namespace NGDP.Patch
{
    public class Versions
    {
        public Dictionary<string, Record> Records { get; } = new Dictionary<string,Record>();

        public Versions(string channel)
        {
            using (var asyncClient = new AsyncClient("us.patch.battle.net", 1119))
            {
                asyncClient.Send($"/{channel}/versions");

                using (var reader = new StreamReader(asyncClient.Stream))
                {
                    // Skip header
                    // ReSharper disable once RedundantAssignment
                    var line = reader.ReadLine();
                    while ((line = reader.ReadLine()) != null)
                    {
                        var lineTokens = line.Split('|');

                        Records[lineTokens[0]] = new Record
                        {
                            Region = lineTokens[0],
                            BuildConfig = BuildHash(lineTokens[1]),
                            CDNConfig = BuildHash(lineTokens[2]),
                            KeyRing = BuildHash(lineTokens[3]),
                            BuildID = int.Parse(lineTokens[4]),
                            VersionsName = lineTokens[5],
                            ProductConfig = BuildHash(lineTokens[6]),

                            Channel = channel
                        };
                    }
                }
            }
        }

        public class Record
        {
            public string Region { get; set; }
            public byte[] BuildConfig { get; set; }
            public byte[] CDNConfig { get; set; }
            public byte[] KeyRing { get; set; }
            public int BuildID { get; set; }
            public string VersionsName { get; set; }
            public byte[] ProductConfig { get; set; }

            public string Channel { get; set; }

            public string GetName(string channel) => $"{Channel}-{BuildID}patch{VersionsName.Substring(0, 5)}_{channel}";

            public bool Equals(Record otherRegionVersion)
            {
                if (otherRegionVersion.BuildID != BuildID)
                    return true;

                if (!ByteArrayComparer.Instance.Equals(otherRegionVersion.BuildConfig, BuildConfig))
                    return false;

                if (!ByteArrayComparer.Instance.Equals(otherRegionVersion.CDNConfig, CDNConfig))
                    return false;

                if (!ByteArrayComparer.Instance.Equals(otherRegionVersion.ProductConfig, ProductConfig))
                    return false;

                if (!string.Equals(VersionsName, otherRegionVersion.VersionsName))
                    return false;

                return true;
            }
        }

        private static int GetHexVal(char hex)
        {
            // For uppercase A-F letters:
            // return val - (val < 58 ? 48 : 55);
            // For lowercase a-f letters:
            return hex - (hex < 58 ? 48 : 87);
            // Or the two combined, but a bit slower:
            // return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        private static byte[] BuildHash(string hex)
        {
            var arr = new byte[hex.Length >> 1];
            for (var i = 0; i < hex.Length >> 1; ++i)
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));

            return arr;
        }
    }
}
