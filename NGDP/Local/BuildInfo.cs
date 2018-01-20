using System;
using System.Collections.Generic;
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

        public Versions.Record Version { get; set; }
        public CDNs.Record CDN { get; set; }

        public string Channel { get; set; }
        public string VersionName { get; set; }

        public event Action OnReady;
        public bool Loading { get; private set; }

        public bool Ready => Encoding.Count != 0 && Root.Count != 0;

        public bool Expired { get; set; } = false;

        public bool JustDeployed { get; set; } = true;

        public HashSet<string> Regions { get; } = new HashSet<string>();

        public async Task Prepare(bool downloadFiles = false)
        {
            var skipFilePath = Path.Combine(Scanner.Configuration.Proxy.MirrorRoot, VersionName, ".skip");
            if (downloadFiles && File.Exists(skipFilePath))
                return;

            if (Loading)
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(skipFilePath));
            File.Create(skipFilePath);

            Loading = true;

            await Task.Run(() =>
            {
                Scanner.WriteLine($"[{VersionName}] Downloading encoding {BuildConfiguration.Encoding[1].ToHexString()} ...");
                Encoding.FromNetworkResource(CDN.Hosts[0],
                    $"/{CDN.Path}/data/{BuildConfiguration.Encoding[1][0]:x2}/{BuildConfiguration.Encoding[1][1]:x2}/{BuildConfiguration.Encoding[1].ToHexString()}");
                Scanner.WriteLine($"[{VersionName}] Encoding downloaded ({Encoding.Count} entries).");

                if (!Encoding.TryGetValue(BuildConfiguration.Root, out var rootEncodingEntry))
                {
                    Scanner.WriteLine($"[{VersionName}] Unable to find root file {BuildConfiguration.Root.ToHexString()} ...");
                    return;
                }

                Scanner.WriteLine($"[{VersionName}] Downloading root {rootEncodingEntry.Key.ToHexString()} ...");
                Root.FromStream(CDN.Hosts[0], $"/{CDN.Path}/data/{rootEncodingEntry.Key[0]:x2}/{rootEncodingEntry.Key[1]:x2}/{rootEncodingEntry.Key.ToHexString()}");
                Scanner.WriteLine($"[{VersionName}] Root downloaded ({Root.Count} entries).");

                Scanner.WriteLine($"[{VersionName}] Downloading {ContentConfiguration.Archives.Length} indices ...");
                Indices.FromStream(CDN.Hosts[0], ContentConfiguration.Archives);
                Scanner.WriteLine($"[{VersionName}] Indices downloaded ({Indices.Count} entries).");

                void InstallLoader(string host, string path, byte[] hash)
                {
                    Install.FromNetworkResource(host,
                        $"/{path}/data/{hash[0]:x2}/{hash[1]:x2}/{hash.ToHexString()}");

                    if (Install.Loaded)
                        Scanner.WriteLine($"[{VersionName}] Install file loaded ({hash.ToHexString()}, {Install.Count} entries).");
                }

                InstallLoader(CDN.Hosts[0], CDN.Path, BuildConfiguration.Install[1]);
                if (!Install.Loaded)
                {
                    if (Encoding.TryGetValue(BuildConfiguration.Install[0], out var encodingEntry))
                        InstallLoader(CDN.Hosts[0], CDN.Path, encodingEntry.Key);
                    else
                        Scanner.WriteLine($"[{VersionName}] Install file not found!");
                }
            }).ConfigureAwait(false);

            OnReady?.Invoke();

            if (downloadFiles && Install.Loaded)
            {
                await Task.Run(() =>
                {
                    foreach (var kv in Scanner.Configuration.GetBranchInfo(Channel).AutoDownloads)
                    {
                        DownloadFile(kv.Value, kv.LocalName);

                        // If we see an exe, be horribly gullible and hope for a PDB
                        if (kv.Value.Contains(".exe"))
                        {
                            var remotePdbName = kv.Value.Replace(".exe", ".pdb");
                            var localPdbName = kv.LocalName?.Replace(".exe", ".pdb");

                            DownloadFile(remotePdbName, localPdbName);
                        }
                    }

                    // Mark for collection
                    Expired = true;
                });
            }

            Loading = false;
        }

        public void Unload()
        {
            // Don't empty if loading
            if (Loading || !Expired)
                return;

            Encoding.Clear();
            Root.Clear();
            Indices.Clear();

            Expired = false;
            Loading = false;

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced);

            Scanner.WriteLine($"[{VersionName}] Unloaded.");
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

        public void DownloadFile(string remoteFileName, string localFileName)
        {
            if (string.IsNullOrEmpty(localFileName))
                localFileName = Path.GetFileName(remoteFileName.Replace("\\", "/"));

            if (string.IsNullOrEmpty(localFileName))
                return;

            var completeFilePath = Path.Combine(Scanner.Configuration.Proxy.MirrorRoot, VersionName, localFileName);
            var fileDirectory = Path.GetDirectoryName(completeFilePath);
            if (string.IsNullOrEmpty(fileDirectory))
                return;

            if (!Directory.CreateDirectory(fileDirectory).Exists)
                return;

            if (File.Exists(completeFilePath))
                return;

            var fileEntry = GetEntry(remoteFileName);
            if (fileEntry == null)
                return;

            using (var blte = new BLTE(CDN.Hosts[0]))
            {
                if (fileEntry.ArchiveIndex != -1)
                    blte.AddHeader("Range", $"bytes={fileEntry.Offset}-{fileEntry.Offset + fileEntry.Size - 1}");

                var archiveName = fileEntry.Hash.ToHexString();
                if (fileEntry.ArchiveIndex != -1)
                    archiveName = Indices.Archives[fileEntry.ArchiveIndex].ToHexString();

                blte.Send($"/{CDN.Path}/data/{archiveName.Substring(0, 2)}/{archiveName.Substring(2, 2)}/{archiveName}");

                Scanner.WriteLine($"[{VersionName}] Downloading {localFileName} from {blte.URL} ...");

                using (var fileStream = File.OpenWrite(completeFilePath))
                    blte.PipeTo(fileStream);
            }
        }
    }
}
