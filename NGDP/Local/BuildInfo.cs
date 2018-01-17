using System;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using NGDP.NGDP;
using NGDP.Patch;
using NGDP.Utilities;

namespace NGDP.Local
{
    public class BuildInfo
    {
        public Encoding Encoding { get; } = new Encoding();
        public Root Root { get; } = new Root();
        public IndexStore Indices { get; } = new IndexStore();
        public Install Install { get; } = new Install();

        public BuildConfiguration BuildConfiguration { get; set; }
        public ContentConfiguration ContentConfiguration { get; set; }
        public CDNs.Record ServerInfo { get; set; }

        public string Channel { get; set; }
        public string VersionName { get; set; }

        public event Action OnReady;
        public bool Loading { get; private set; }

        public bool Ready => Encoding.Count != 0 && Root.Count != 0;

        public async Task Prepare(bool downloadFiles = false)
        {
            if (File.Exists(Path.Combine(Scanner.Configuration.Proxy.MirrorRoot, VersionName, ".skip")))
                return;

            if (Loading)
                return;

            Loading = true;

            await Task.Run(() =>
            {
                Scanner.WriteLine($"[{VersionName}] Downloading encoding {BuildConfiguration.Encoding[1].ToHexString()} ...");
                Encoding.FromNetworkResource(ServerInfo.Hosts[0],
                    $"/{ServerInfo.Path}/data/{BuildConfiguration.Encoding[1][0]:x2}/{BuildConfiguration.Encoding[1][1]:x2}/{BuildConfiguration.Encoding[1].ToHexString()}");
                Scanner.WriteLine($"[{VersionName}] Encoding downloaded ({Encoding.Count} entries).");

                if (!Encoding.TryGetValue(BuildConfiguration.Root, out var rootEncodingEntry))
                    return;

                Scanner.WriteLine($"[{VersionName}] Downloading root {rootEncodingEntry.Key.ToHexString()} ...");
                Root.FromStream(ServerInfo.Hosts[0],
                    $"/{ServerInfo.Path}/data/{rootEncodingEntry.Key[0]:x2}/{rootEncodingEntry.Key[1]:x2}/{rootEncodingEntry.Key.ToHexString()}");
                Scanner.WriteLine($"[{VersionName}] Root downloaded.");

                Scanner.WriteLine($"[{VersionName}] Downloading {ContentConfiguration.Archives.Length} indices ...");
                Indices.FromStream(ServerInfo.Hosts[0], ContentConfiguration.Archives);
                Scanner.WriteLine($"[{VersionName}] Indices downloaded ({Indices.Count} entries).");

                void InstallLoader(string host, string path, byte[] hash)
                {
                    Scanner.WriteLine($"[CASC] Trying to load Install {hash.ToHexString()} ...");

                    Install.FromNetworkResource(host,
                        $"/{path}/data/{hash[0]:x2}/{hash[1]:x2}/{hash.ToHexString()}");
                }

                InstallLoader(ServerInfo.Hosts[0], ServerInfo.Path, BuildConfiguration.Install[1]);
                if (!Install.Loaded)
                {
                    if (Encoding.TryGetValue(BuildConfiguration.Install[0], out var encodingEntry))
                        InstallLoader(ServerInfo.Hosts[0], ServerInfo.Path, encodingEntry.Key);
                    else
                        Scanner.WriteLine($"[{VersionName}] Install file not found!");
                }

                if (Install.Loaded)
                    Scanner.WriteLine($"[{VersionName}] Install downloaded ({Install.Count} entries).");
            }).ConfigureAwait(false);

            Loading = false;
            OnReady?.Invoke();

            if (downloadFiles && Install.Loaded)
            {
                await Task.Run(() =>
                {
                    foreach (var kv in Scanner.Configuration.GetBranchInfo(VersionName).AutoDownloads)
                        DownloadFile(kv.Value, kv.LocalName);

                    // Immediately free
                    Unload();
                });
            }
        }

        public void Unload()
        {
            // Don't empty if loading
            if (Loading)
                return;

            Encoding.Clear();
            Root.Clear();
            Indices.Clear();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced);
        }

        public IndexStore.Record GetEntry(string fileName) => GetEntry(JenkinsHashing.Instance.ComputeHash(fileName));

        public IndexStore.Record GetEntry(ulong fileHash)
        {
            Encoding.Entry encodingRecord;

            if (Root.TryGetByHash(fileHash, out var rootRecord))
            {
                if (!Encoding.TryGetValue(rootRecord.MD5, out encodingRecord))
                    return null;
            }
            else if (Install.HasFile(fileHash))
            {
                var installEntry = Install.GetEntriesByHash(fileHash).First();
                if (!Encoding.TryGetValue(installEntry.MD5, out encodingRecord))
                    return null;
            }
            else
                return null;

            if (!Indices.TryGetValue(encodingRecord.Key, out var indexEntry))
            {
                return new IndexStore.Record
                {
                    Offset = 0,
                    Size = -1,
                    ArchiveIndex = -1, // Use as a marker for whole-size archives
                    Hash = encodingRecord.Key
                };
            }

            return indexEntry;
        }

        public async void DownloadFile(string remoteFileName, string localFileName)
        {
            if (!string.Equals(localFileName, @"\") || string.IsNullOrEmpty(localFileName))
                localFileName = Path.GetFileName(remoteFileName);
            
            var completeFilePath = Path.Combine(Scanner.Configuration.Proxy.MirrorRoot, localFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(completeFilePath));

            if (File.Exists(completeFilePath))
                return;
            
            var fileEntry = GetEntry(remoteFileName);
            if (fileEntry == null)
                return;

            await Task.Run(() =>
            {
                using (var blte = new BLTE(ServerInfo.Hosts[0]))
                {
                    if (fileEntry.ArchiveIndex != -1)
                        blte.AddHeader("Range", $"bytes={fileEntry.Offset}-{fileEntry.Offset + fileEntry.Size - 1}");

                    var archiveName = fileEntry.Hash.ToHexString();
                    if (fileEntry.ArchiveIndex != -1)
                        archiveName = Indices.Archives[fileEntry.ArchiveIndex].ToHexString();

                    blte.Send($"/{ServerInfo.Path}/data/{archiveName.Substring(0, 2)}/{archiveName.Substring(2, 2)}/{archiveName}");

                    using (var fileStream = File.OpenWrite(completeFilePath))
                        blte.PipeTo(fileStream);
                }
            });
        }
    }
}
