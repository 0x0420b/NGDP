using System.IO;
using System.Linq;
using NGDP.Network;
using NGDP.Patch;
using NGDP.Utilities;

namespace NGDP.NGDP
{
    public class BuildConfiguration
    {
        public byte[] Root { get; }
        public byte[][] Install { get; }
        // public int InstallSize { get; }
        // public byte[] Download { get; }
        // public int DownloadSize { get; }
        // public byte[] PartialPriority { get; }
        // public int PartialPrioritySize { get; }
        public byte[][] Encoding { get; }
        // public int[] EncodingSize { get; }
        // public byte[] Patch { get; set; }
        // public int PatchSize { get; set; }
        // public byte[] PatchConfig { get; set; }

        public BuildConfiguration(CDNs.Record hostInfo, byte[] buildHash)
        {
            using (var asyncClient = new AsyncClient(hostInfo.Hosts[0]))
            {
                var queryString =
                    $"/{hostInfo.Path}/config/{buildHash[0]:x2}/{buildHash[1]:x2}/{buildHash.ToHexString()}";

                asyncClient.Send(queryString);

                using (var textReader = new StreamReader(asyncClient.Stream))
                {
                    var line = textReader.ReadLine();
                    if (line != "# Build Configuration")
                        return;

                    while ((line = textReader.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(line))
                            continue;

                        var lineTokens = line.Split('=').Select(l => l.Trim()).ToArray();
                        if (lineTokens.Length != 2)
                            continue;

                        // ReSharper disable once SwitchStatementMissingSomeCases
                        switch (lineTokens[0])
                        {
                            case "root":
                                Root = lineTokens[1].ToByteArray();
                                break;
                            case "install":
                            {
                                Install = new byte[2][];
                                var installTokens = lineTokens[1].Split(' ');
                                Install[0] = installTokens[0].ToByteArray();
                                Install[1] = installTokens[1].ToByteArray();
                                break;
                            }
                            // case "install-size":
                            //     InstallSize = int.Parse(lineTokens[1]);
                            //     break;
                            // case "download":
                            //     Download = lineTokens[1].ToByteArray();
                            //     break;
                            // case "download-size":
                            //     DownloadSize = int.Parse(lineTokens[1]);
                            //     break;
                            // case "partial-priority":
                            //     PartialPriority = lineTokens[1].ToByteArray();
                            //     break;
                            // case "partial-priority-size":
                            //     PartialPrioritySize = int.Parse(lineTokens[1]);
                            //     break;
                            case "encoding":
                            {
                                Encoding = new byte[2][];
                                var encodingTokens = lineTokens[1].Split(' ');
                                Encoding[0] = encodingTokens[0].ToByteArray();
                                Encoding[1] = encodingTokens[1].ToByteArray();
                                break;
                            }
                            // case "encoding-size":
                            // {
                            //     EncodingSize = new int[2];
                            //     var encodingTokens = lineTokens[1].Split(' ');
                            //     EncodingSize[0] = int.Parse(encodingTokens[0]);
                            //     EncodingSize[1] = int.Parse(encodingTokens[1]);
                            //     break;
                            // }
                            // case "patch":
                            //     Patch = lineTokens[1].ToByteArray();
                            //     break;
                            // case "patch-size":
                            //     PatchSize = int.Parse(lineTokens[1]);
                            //     break;
                            // case "patch-config":
                            //     PatchConfig = lineTokens[1].ToByteArray();
                            //     break;
                        }
                    }
                }
            }
        }
    }
}
