using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WMPQ.Protocol;
using WMPQ.Protocol.Client;
using WMPQ.Protocol.Server;

namespace WMPQ
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientBuilds = new[]
            {
                25864, 25864, 25860, 25860, 25848, 25848, 25826, 25807, 25804, 25753, 25744, 25727, 25716, 25717, 25692,
                25632, 25619, 25607, 25549, 25549, 25497, 25497, 25480, 25480, 25455, 25455, 25442, 25442, 25383, 25383,
                25326, 25255, 25208, 25021, 25163, 25135, 25079, 25021, 25021, 24970, 24956, 24931, 24931, 24920, 24920,
                24904, 24896, 24887, 24878, 24864, 24852, 24845, 24834, 24793, 24781, 24759, 24744, 24742, 24738, 24730,
                24715, 24700, 24692, 24681, 24651, 24633, 24614, 24608, 24563, 24539, 24500, 24492, 24461, 24484, 24430,
                24415, 24393, 24367, 24367, 24330, 24330, 24287, 24236, 24218, 24163, 24140, 24116, 24076, 24026, 24015,
                23993, 23959, 23958, 23937, 23910, 23911, 23911, 23905, 23877, 23877, 23857, 23857, 23852, 23852, 23846,
                23846, 23841, 23836, 23835, 23835, 23826, 23826, 23810, 23801, 23789, 23780, 23758, 23753, 23728, 23706,
                23699, 23657, 23623, 23578, 23530, 23514, 23478, 23476, 23470, 23445, 23420, 23436, 23420, 23360, 23360,
                23353, 23244, 23222, 23194, 23178, 23171, 23138, 23109, 23038, 22996, 22996, 22995, 22995, 22989, 22989,
                22950, 22950, 22908, 22908, 22900, 22900, 22864, 22852, 22844, 22810, 22810, 22797, 22747, 22731, 22722,
                22685, 22636, 22624, 22578, 22594, 22566, 22522, 22345, 22201, 22133, 22053, 22018, 21996, 21996, 21952,
                21874, 21846, 21796, 21737, 21691, 21691, 21531, 21491, 21414, 21249, 21215, 21134, 21108, 21063, 20914,
                20810, 20796, 20773, 20756, 20740, 22522, 22522, 22498, 22498, 22484, 22472, 22451, 22445, 22423, 22423,
                22410, 22410, 22396, 22396, 22345, 22371, 22345, 22324, 22306, 22293, 22290, 22289, 22280, 22271, 22267,
                22267, 22248, 22248, 22231, 22217, 22210, 22197, 22172, 22158, 22150, 22124, 22101, 22018, 21996, 22000,
                21973, 21963, 21953, 21742, 21742, 21676, 21676, 21463, 21463, 21355, 21355, 21348, 21345, 21343, 21336,
                21336, 21315, 21315, 21274, 21253, 21210, 21160, 21105, 21073, 21061, 20886, 20886, 20779, 20779, 20726,
                20726, 20716, 20691, 20655, 20601, 20574, 20574, 20490, 20490, 20444, 20444, 20438, 20426, 20395, 20363,
                20328, 20271, 20328, 20338, 20271, 20253, 20253, 20216, 20216, 20201, 20201, 20182, 20182, 20173, 20173,
                20157, 20141, 20130, 20104, 20076, 20057, 20033, 20008, 19988, 19973, 19953, 19906, 19890, 19865, 19865,
                19831, 19831, 19802, 19802, 19793, 19769, 19728, 19711, 19702, 19702, 19701, 19678, 19658, 19622, 19611,
                19605, 19579, 19551, 19533, 19508, 19445, 19342, 19234, 19234, 19116, 19116, 19085, 19057, 19041, 19027,
                19005, 19000, 18988, 18982, 18973, 18965, 18935, 18934, 18927, 18922, 18918, 18916, 18888, 18888, 18865,
                18850, 18848, 18837, 18833, 18816, 18764, 18761, 18738, 18716, 18702, 18689, 18663, 18645, 18612, 18594,
                18566, 18556, 18546, 18537, 18522, 18505, 18482, 18471, 18443, 18379, 18332, 18297, 18179, 18156, 17537,
                19116, 19116, 19103, 19102, 19085, 19041, 19034, 19027, 19027, 19005, 19000, 18990, 18988, 18986, 18982,
                18983, 18966, 18935, 18934, 18922, 18918, 18916, 18898, 18888, 18849, 18838, 18414, 18291, 18291, 18273,
                18224, 18019, 17956, 17930, 17898, 17898, 17889, 17859, 17841, 17807, 17688, 17658, 17658, 17645, 17644,
                17636, 17614, 17595, 17585, 17538, 17538, 17513, 17481, 17399, 17371, 17359, 17345, 17337, 17331, 17321,
                17314, 17299, 17271, 17260, 17247, 17227, 17205, 17191, 17169, 17161, 17153, 17128, 17124, 17116, 17093,
                17056, 17055, 16992, 16983, 16981, 16977, 16965, 16958, 16954, 16946, 16924, 16921, 16911, 16908, 16896,
                16888, 16876, 16853, 16837, 16825, 16826, 16790, 16781, 16767, 16758, 16769, 16760, 16733, 16716, 16709,
                16701, 16685, 16683, 16669, 16650, 16656, 16650, 16634, 16631, 16618, 16597, 16591, 16577, 16562, 16547,
                16539, 16534, 16503, 16486, 16467, 16446, 16408, 16357, 16309, 16155, 16139, 16135, 16057, 16048, 16048,
                16030, 16016, 16016, 16016, 16010, 16004, 15983, 15972, 15961, 15952, 15929, 15929, 15913, 15882, 15882,
                15851, 15851, 15799, 15781, 15762, 15752, 15739, 15726, 15699, 15689, 15677, 15668, 15662, 15657, 15650,
                15640, 15589, 15544, 15508, 15464, 15595, 15595, 15531, 15499, 15354, 15354, 15338, 15314, 15211, 15211,
                15201, 15176, 15171, 15148, 15050, 15005, 15005, 14995, 14980, 14976, 14966, 14946, 14942, 14911, 14899,
                14890, 14849, 14809, 14791, 14732, 14545, 14545, 14534, 14522, 14505, 14492, 14480, 14333, 14316, 14313,
                14299, 14288, 14265, 14241, 14199, 14179, 14133, 14107, 14040, 14007, 14002, 13914, 13914, 13875, 13850,
                13812, 13707, 13698, 13682, 13623, 13596, 13596, 13329, 13287, 13205, 13202, 13195, 13189, 13164, 13164,
                13156, 13131, 13117, 13082, 13066, 13033, 12984, 12942, 12941, 12911, 12857, 12824, 12803, 12759, 12694,
                12635, 12644, 12604, 12539, 12479, 12319, 12266, 12232, 12164, 12122, 12065, 12025, 11927, 10287, 12340,
                12213, 12196, 12166, 12148, 12124, 12045, 11993, 11723, 11685, 11599, 11403, 11159, 10958, 10952, 10894,
                10835, 10805, 10772, 10747, 10712, 10676, 10623, 10596, 10571, 10554, 10505, 10482, 10392, 10371, 10357,
                10314, 10192, 10083, 10072, 10048, 10026, 9947, 9901, 9889, 9868, 9855, 9835, 9806, 9767, 9757, 9742,
                9733, 9722, 9704, 9684, 9658, 9637, 9626, 9614, 9551, 9506, 9464, 9328, 9183, 9095, 9061, 9056, 9038,
                8970, 8885, 8820, 8788, 8770, 8714, 8681, 8634, 8622, 8471, 8391, 8334, 8303, 8606, 8478, 8278, 8209,
                8125, 8089, 8063, 8049, 8031, 8016, 7994, 7979, 7962, 7958, 7948, 7923, 7897, 7799, 7741, 7720, 7705,
                7677, 7655, 7627, 7561, 7359, 7344, 7318, 7304, 7286, 7272, 7261, 7250, 7229, 7214, 7195, 7189, 7187,
                7175, 7153, 7125, 7091, 7051, 6983, 6932, 6898, 6803, 6739, 6729, 6692, 6678, 6655, 6641, 6624, 6607,
                6592, 6577, 6546, 6448, 6403, 6383, 6373, 6337, 6320, 6314, 6299, 6282, 6244, 6213, 6180, 6178, 6175,
                6157, 6144, 6114, 6108, 6082, 6080, 6052, 6046, 6022, 5991, 5965, 5921, 5894, 5849, 5666, 5665, 5610,
                6005, 5875, 5875, 5803, 5734, 5595, 5595, 5590, 5579, 5561, 5537, 5521, 5496, 5464, 5464, 5462, 5428,
                5413, 5383, 5366, 5344, 5302, 5230, 5195, 5140, 5086, 5059, 4996, 4983, 4937, 4869, 4851, 4878, 4807,
                4784, 4769, 4735, 4714, 4695, 4671, 4579, 4544, 4500, 4470, 4499, 4442, 4375, 4364, 4341, 4297, 4284,
                4281, 4262, 4222, 4211, 4196, 4150, 4147, 4125, 4062, 4044, 3989, 3980, 3988, 3925, 3892, 3810, 3807,
                3734, 3712, 3702, 3694, 3494, 3368
            };

            if (args.Length == 0)
            {
                foreach (var build in clientBuilds.Distinct())
                    DownloadClientBuild("WoW", build);
            }
            else
                switch (args[0])
                {
                    case "--mfil":
                        DownloadMFIL(args[1]);
                        break;
                    case "--mfils":
                        using (var reader = new StreamReader(args[1]))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                                DownloadMFIL(line);
                        }
                        break;
                    case "--bruteforce":
                        TryBruteforceArchives(int.Parse(args[1]), int.Parse(args[2]), int.Parse(args[3]));
                        break;
                    case "--maldivia":
                        ParseMaldiviaDump(args[1]);
                        break;
                }
        }

        private static void ParseMaldiviaDump(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var tokens = line.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray();
                    if (tokens.Length == 0 || tokens.Length != 6)
                        continue;

                    if (tokens[0] == "program")
                        continue;

                    var programName = tokens[0].ToLower();
                    var configUrl = tokens[5];
                    var mfil = tokens[4];
                    var tfil = tokens[3];

                    Console.WriteLine($"[{programName}] MFIL: {mfil}");

                    // 1. Copy config file
                    using (var netStream = SendGet(configUrl))
                    {
                        if (netStream != null)
                        {
                            var configUri = "./" + new Uri(configUrl).AbsolutePath;
                            Directory.CreateDirectory(Path.GetDirectoryName(configUri));
                            if (!File.Exists(configUri))
                            {
                                using (var fs = File.OpenWrite(configUri))
                                    netStream.CopyTo(fs);
                            }
                        }
                    }

                    var configInfo = SendGet(configUrl)?.Deserialize<Config>();
                    if (configInfo != null)
                    {
                        var productInfo = configInfo.Version.Products.FirstOrDefault(p => p.Product.ToLower() == programName);
                        if (productInfo == null)
                            continue;

                        var serverBase = productInfo.Servers.First();
                        var mfilName = $"{programName.ToLowerInvariant()}-{tokens[2]}-{mfil}.mfil";
                        var tfilName = $"{programName.ToLowerInvariant()}-{tokens[2]}-{tfil}.tfil";
                        using (var netStream = SendGet($"{serverBase.Url}{mfilName}"))
                        {
                            if (netStream != null)
                            {
                                var uri = "./" + new Uri($"{serverBase.Url}{mfilName}").AbsolutePath;
                                Directory.CreateDirectory(Path.GetDirectoryName(uri));
                                if (!File.Exists(uri))
                                    using (var fs = File.OpenWrite(uri))
                                        netStream.CopyTo(fs);
                            }
                        }

                        // using (var netStream = SendGet($"{serverBase.Url}{tfilName}"))
                        // {
                        //     var uri = "./" + new Uri($"{serverBase.Url}{tfilName}").AbsolutePath;
                        //     Directory.CreateDirectory(Path.GetDirectoryName(uri));
                        //     if (!File.Exists(uri))
                        //         using (var fs = File.OpenWrite(uri))
                        //             netStream.CopyTo(fs);
                        // }
                    }
                    else
                        Console.WriteLine($"[{programName}] Unable to read {configUrl}...");
                }
            }
        }

        private static void TryBruteforceArchives(int baseDirect, int minBuild, int maxBuild)
        {
            var dataFile = "Data/wow-update-base-{0}.MPQ";
            var winUpdateFile = "Updates/wow-0-{0}-{1}-final.MPQ";

            var host = $"http://ak.worldofwarcraft.com.edgesuite.net/wow-pod-retail/NA/{baseDirect}.direct/";
            dataFile = Path.Combine(host, dataFile);
            winUpdateFile = Path.Combine(host, winUpdateFile);

            Parallel.For(minBuild, maxBuild + 1, new ParallelOptions() {MaxDegreeOfParallelism = 2}, i =>
            {
                Console.WriteLine($"[] Testing build {i}");

                var dataExists = FileExistsRemote(string.Format(dataFile, i));
                var winExists = FileExistsRemote(string.Format(winUpdateFile, i, "Win"));
                var osxExists = FileExistsRemote(string.Format(winUpdateFile, i, "OSX"));

                if (dataExists && winExists && osxExists)
                    Console.WriteLine($"[*] Found build {i}");
            });
        }

        private static bool FileExistsRemote(string uri)
        {
            try
            {
                var localFilePath = new List<string>() { "." };
                localFilePath.AddRange(new Uri(uri).Segments.Skip(1));

                var localFile = string.Join("/", localFilePath);

                Directory.CreateDirectory(Path.GetDirectoryName(localFile));

                if (File.Exists(localFile))
                    return true;

                using (var netStream = SendGet(uri))
                {
                    if (netStream != null)
                    {
                        Console.WriteLine("Downloading");
                        Console.WriteLine("     From: {0}", uri);
                        Console.WriteLine("     To:   {0}", localFile);

                        using (var fStream = File.OpenWrite(localFile))
                            netStream.CopyTo(fStream);

                    }
                }

                return new FileInfo(localFile).Length != 0;
            } catch { return false; }
        }

        private static void DownloadClientBuild(string programName, int build)
        {
            var responseInfo = SendPatchInfoRequest(programName, build).GetRecord(programName);
            if (responseInfo == null)
                return;

            Console.WriteLine($"[{build}] Found {responseInfo.BuildId} as target build.");

            Console.WriteLine($"[{build}] Loading XML {responseInfo.Config}.");
            var serverConfig = SendJsonGet<Config>(responseInfo.Config);

            var baseAddress = serverConfig.Version.Products[0].Servers[0].Url;
            var mfilName = $"wow-{responseInfo.BuildId}-{responseInfo.Manifest}.mfil";

            DownloadMFIL(Path.Combine(baseAddress, mfilName));
        }

        private static void DownloadMFIL(string address)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(address);
            webRequest.Method = "GET";

            Console.WriteLine("Downloading MFIL from {0}", webRequest.Address);

            var webResponse = (HttpWebResponse)webRequest.GetResponse();
            var mfil = new MFIL(webResponse.GetResponseStream());

            foreach (var knownFile in mfil.Records)
            {
                var fileUri = new Uri(address.Replace(Path.GetFileName(address), knownFile.File));

                var localSegments = new List<string>() {"."};
                localSegments.AddRange(fileUri.Segments.Skip(1));
                var localFilePath = Path.Combine(localSegments.Select(s => s.Replace("/", "")).ToArray());

                Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));

                var localFileInfo = new FileInfo(localFilePath);
                if (localFileInfo.Exists)
                {
                    continue;

                    // Console.WriteLine("{2}: Expected size {0}, found {1}", knownFile.Size, localFileInfo.Length, knownFile.File);
                    // localFilePath.Replace(".MPQ", $"-{build}.MPQ");
                    //
                    // localFileInfo.Delete();
                    // localFileInfo = new FileInfo(localFilePath);
                }

                using (var localFileStream = localFileInfo.OpenWrite())
                {
                    Console.WriteLine("[{0}] Downloading ({1:n0} bytes, version {2})", knownFile.Version, knownFile.Size,
                        knownFile.Version);
                    Console.WriteLine("     From: {0}", fileUri);
                    Console.WriteLine("     To:   {0}", localFilePath);

                    SendGet(fileUri.ToString())?.CopyTo(localFileStream);
                }
            }

            var localMfilPath = string.Join("/", new Uri(address).Segments.Skip(1));
            if (File.Exists(localMfilPath))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(localMfilPath));

            webRequest = (HttpWebRequest)WebRequest.Create(address);
            webRequest.Method = "GET";
            webResponse = (HttpWebResponse)webRequest.GetResponse();
            using (var localFile = File.OpenWrite(localMfilPath))
            using (var netStream = webResponse.GetResponseStream())
                netStream.CopyTo(localFile);
        }

        private static PatchInfoResponse SendPatchInfoRequest(string programName, int clientBuild)
        {
            var request = new PatchInfoRequest
            {
                Program = programName
            };

            request.Records.Add(new PatchInfoRequest.Record()
            {
                Program = "Bnet",
                Component = "Win",
                Version = 1
            });

            request.Records.Add(new PatchInfoRequest.Record()
            {
                Program = programName,
                Component = "enUS",
                Version = 1,
                Build = clientBuild
            });

            return SendJsonPost<PatchInfoRequest, PatchInfoResponse>(request);
        }

        private static TResponse SendJsonPost<TRequest, TResponse>(TRequest request)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create("http://enUS.patch.battle.net:1119/patch");
            webRequest.ContentType = "text/xml;encoding='utf-8'";
            webRequest.Method = "POST";

            var serializedBody = request.Serialize();
            webRequest.ContentLength = serializedBody.Length;

            using (var requestWriter = new StreamWriter(webRequest.GetRequestStream()))
                requestWriter.Write(serializedBody);

            var responseMsg = (HttpWebResponse)webRequest.GetResponse();
            if (responseMsg.StatusCode == HttpStatusCode.OK)
                return responseMsg.GetResponseStream().Deserialize<TResponse>();

            return default(TResponse);
        }

        private static TResponse SendJsonGet<TResponse>(string address)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(address);
            webRequest.Method = "GET";

            var responseMsg = (HttpWebResponse)webRequest.GetResponse();
            if (responseMsg.StatusCode == HttpStatusCode.OK)
                return responseMsg.GetResponseStream().Deserialize<TResponse>();

            return default(TResponse);
        }

        private static Stream SendGet(string address)
        {
            try
            {
                var webRequest = (HttpWebRequest) WebRequest.Create(address);
                webRequest.Method = "GET";

                var responseMsg = (HttpWebResponse) webRequest.GetResponse();
                if (responseMsg.StatusCode == HttpStatusCode.OK)
                    return responseMsg.GetResponseStream();
            }
            catch
            {
                try
                {
                    var uri = new Uri(address);
                    var lst = new List<string>(uri.Segments);

                    var sub = lst[lst.Count - 1].IndexOf('-');
                    var prgm = lst[lst.Count - 1].Substring(0, sub);
                    lst[lst.Count - 1] = lst[lst.Count - 1].Replace(prgm, prgm.ToLower());

                    var b = new UriBuilder(uri);
                    b.Path = string.Join("", lst);
                    var webRequest = (HttpWebRequest) WebRequest.Create(b.ToString());
                    webRequest.Method = "GET";

                    var responseMsg = (HttpWebResponse) webRequest.GetResponse();
                    if (responseMsg.StatusCode == HttpStatusCode.OK)
                        return responseMsg.GetResponseStream();
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }
}
