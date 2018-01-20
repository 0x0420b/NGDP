using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NGDP.Local;
using NGDP.NGDP;
using NGDP.Patch;

namespace NGDP
{
    public class Channel
    {
        public string ChannelName { get; set; }
        public string DisplayName { get; set; }

        public event Action<string, string, string> BuildDeployed;

        public Versions Version { get; private set; }
        public CDNs CDN { get; private set; }

        public void Update(bool silent)
        {
            Version = new Versions(ChannelName);
            CDN = new CDNs(ChannelName);

            // Walk through versions
            foreach (var versionInfo in Version.Records)
            {
                var versionName = versionInfo.Value.GetName(DisplayName);

                // Get CDN data.
                if (!CDN.Records.TryGetValue(versionInfo.Value.Region, out var serverInfo))
                    continue;

                var currentBuildInfo = RemoteBuildManager.GetBuild(versionName);
                var isNewBuild = currentBuildInfo == null;
                if (!isNewBuild)
                {
                    currentBuildInfo.Regions.Add(versionInfo.Value.Region);
                }
                else
                {
                    currentBuildInfo = new BuildInfo
                    {
                        Version = versionInfo.Value,
                        CDN = serverInfo,

                        VersionName = versionName
                    };

                    RemoteBuildManager.AddBuild(currentBuildInfo);
                }

                // Get build info
                currentBuildInfo.BuildConfiguration = new BuildConfiguration(serverInfo, versionInfo.Value.BuildConfig);
                currentBuildInfo.ContentConfiguration = new ContentConfiguration(serverInfo, versionInfo.Value.CDNConfig);

                if (currentBuildInfo.BuildConfiguration.Encoding == null || currentBuildInfo.ContentConfiguration.Archives == null)
                    Scanner.WriteLine($"[{versionName}] Error retrieving either CDN or build configuration file.");
                else if (isNewBuild)
                    Scanner.QueueInitialUpdate(currentBuildInfo);
            }

            foreach (var currentBuild in RemoteBuildManager.Builds.Values.Where(b => b.JustDeployed))
            {
                currentBuild.JustDeployed = true;
                var coalescedRegions = string.Join(", ", currentBuild.Regions).ToUpperInvariant();

                if (!silent)
                    BuildDeployed?.Invoke(ChannelName, currentBuild.VersionName, coalescedRegions);

                Scanner.WriteLine($"[{currentBuild.VersionName}] Deployed to regions {coalescedRegions}.");
            }

            // Sleep half a second to make sure every message goes through.
            Thread.Sleep(500);
        }
    }
}
