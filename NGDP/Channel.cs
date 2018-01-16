using System;
using System.Threading;
using Meebey.SmartIrc4net;
using NGDP.Local;
using NGDP.NGDP;
using NGDP.Patch;

namespace NGDP
{
    public class Channel
    {
        public string ChannelName { get; set; }
        public string DisplayName { get; set; }

        public event Action<SendType, string> MessageEvent;

        public void Update(bool silent)
        {
            var versions = new Versions(ChannelName);
            var cdns = new CDNs(ChannelName);

            // Walk through versions
            foreach (var versionInfo in versions.Records)
            {
                // Get CDN data.
                if (!cdns.Records.TryGetValue(versionInfo.Value.Region, out CDNs.Record serverInfo))
                    serverInfo = cdns.Records["eu"];

                var versionName = versionInfo.Value.GetName(DisplayName);

                if (RemoteBuildManager.IsBuildKnown(versionName))
                    continue;

                var buildInfo = new BuildInfo {
                    VersionName = versionName,
                    ServerInfo = serverInfo
                };

                RemoteBuildManager.AddBuild(buildInfo);

                if (!silent)
                    MessageEvent?.Invoke(SendType.Message, $"Build {versionName} deployed.");

                // Get build info
                buildInfo.BuildConfiguration = new BuildConfiguration(serverInfo, versionInfo.Value.BuildConfig);
                buildInfo.ContentConfiguration = new ContentConfiguration(serverInfo, versionInfo.Value.CDNConfig);

                if (buildInfo.BuildConfiguration.Encoding == null || buildInfo.ContentConfiguration.Archives == null)
                {
                    if (!silent)
                        MessageEvent?.Invoke(SendType.Message, $"Error retrieving either CDN or build configuration file for {versionName}.");
                }
                // else
                //     buildInfo.Prepare(true);

#if !UNIX
                break;
#endif
            }

            // Sleep one second to make sure every message goes through.
            Thread.Sleep(1000);
        }
    }
}
