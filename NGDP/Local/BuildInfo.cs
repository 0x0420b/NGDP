using System;
using System.IO;
using System.Linq;
using System.Threading;
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

        public string VersionName { get; set; }

        public event Action OnReady;
        public bool Ready { get; private set; }
        public bool Loading { get; private set; }

        public async void Prepare(bool downloadFiles = false)
        {
            if (Loading)
                return;

            Loading = true;

            await Task.Run(() =>
            {
                Program.WriteLine($"[{VersionName}] Downloading encoding {BuildConfiguration.Encoding[1].ToHexString()} ...");
                Encoding.FromNetworkResource(ServerInfo.Hosts[0],
                    $"/{ServerInfo.Path}/data/{BuildConfiguration.Encoding[1][0]:x2}/{BuildConfiguration.Encoding[1][1]:x2}/{BuildConfiguration.Encoding[1].ToHexString()}");
                Program.WriteLine($"[{VersionName}] Encoding downloaded ({Encoding.Count} entries).");

                if (!Encoding.TryGetValue(BuildConfiguration.Root, out Encoding.Entry rootEncodingEntry))
                    return;

                Program.WriteLine($"[{VersionName}] Downloading root {rootEncodingEntry.Key.ToHexString()} ...");
                Root.FromStream(ServerInfo.Hosts[0],
                    $"/{ServerInfo.Path}/data/{rootEncodingEntry.Key[0]:x2}/{rootEncodingEntry.Key[1]:x2}/{rootEncodingEntry.Key.ToHexString()}");
                Program.WriteLine($"[{VersionName}] Root downloaded.");

                Program.WriteLine($"[{VersionName}] Downloading {ContentConfiguration.Archives.Length} indices ...");
                Indices.FromStream(ServerInfo.Hosts[0], ContentConfiguration.Archives);
                Program.WriteLine($"[{VersionName}] Indices downloaded ({Indices.Records.Count} entries).");

                Action<string, string, byte[]> installLoader = (string host, string path, byte[] hash) =>
                {
                    Program.WriteLine($"[CASC] Trying to load Install {hash.ToHexString()} ...");

                    Install.FromNetworkResource(host,
                        $"/{path}/data/{hash[0]:x2}/{hash[1]:x2}/{hash.ToHexString()}");
                };

                installLoader(ServerInfo.Hosts[0], ServerInfo.Path, BuildConfiguration.Install[1]);
                if (!Install.Loaded)
                {
                    if (Encoding.TryGetValue(BuildConfiguration.Install[0], out Encoding.Entry encodingEntry))
                        installLoader(ServerInfo.Hosts[0], ServerInfo.Path, encodingEntry.Key);
                    else
                        Program.WriteLine($"[{VersionName}] Install file not found!");
                }

                if (Install.Loaded)
                    Program.WriteLine($"[{VersionName}] Install downloaded ({Install.Count} entries).");
            }).ConfigureAwait(false);

            Ready = true;
            Loading = false;
            OnReady?.Invoke();

            if (downloadFiles && Install.Loaded)
            {
#if UNIX
                await Task.Factory.StartNew(() =>
                {
                    foreach (var kv in Program.FilesToDownload)
                        DownloadFile(kv.Key, kv.Value);

                    // Immediately free
                    Unload();
                });
#else
                DownloadFile("Wow.exe", "Wow.exe");
#endif
            }
        }

        public void Unload()
        {
            if (!Ready)
                return;

            Ready = false;
            Encoding.Clear();
            Root.Clear();
            Indices.Clear();

            GC.Collect(3, GCCollectionMode.Forced);
        }

        public IndexStore.Record GetEntry(string fileName) => GetEntry(JenkinsHashing.Instance.ComputeHash(fileName));

        public IndexStore.Record GetEntry(ulong fileHash)
        {
            if (!Ready)
                return null;

            Encoding.Entry encodingRecord;

            if (Root.TryGetByHash(fileHash, out Root.Record rootRecord))
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

            if (!Indices.TryGetValue(encodingRecord.Key, out IndexStore.Record indexEntry))
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
            if (!string.Equals(localFileName, @"\"))
                localFileName = Path.GetFileName(remoteFileName);

            var completeFilePath = Path.Combine("/home/ubuntu/workspace/backups", VersionName, localFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(completeFilePath));
            
            if (File.Exists(completeFilePath))
                return;
            
            var fileEntry = GetEntry(remoteFileName);
            if (fileEntry == null)
                return;

            await Task.Factory.StartNew(() =>
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
