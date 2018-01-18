using System.Threading.Tasks;
using Meebey.SmartIrc4net;
using NGDP.Local;
using NGDP.Utilities;
using System;
using System.IO;
using System.Linq;

namespace NGDP.Commands
{
    public static class Handlers
    {
        [CommandHandler(".listen", "<channel name>", 1)]
        public static void JoinChannel(IrcClient client, IrcMessageData messageData)
        {
            var channelName = messageData.MessageArray[1];
            if (Scanner.Channels.Any(c => c.ChannelName == channelName))
                return;

            var channel = new Channel
            {
                ChannelName = channelName,
                DisplayName = channelName,
            };

            channel.BuildDeployed += Scanner.OnBuildDeployed;

            Scanner.Channels.Add(channel);
        }

        [CommandHandler(".forceupdate", "<branch_name>", 1)]
        public static void ForceChannelUpdate(IrcClient client, IrcMessageData messageData)
        {
            var channelName = messageData.MessageArray[1];
            var channel = Scanner.Channels.FirstOrDefault(p => p.ChannelName == channelName);
            if (channel == null)
                client.SendReply(messageData, "Unknown branch (try adding it first?)");
            else
                channel.Update(false);
        }

        [CommandHandler(".notify", "<branch_name>", 1)]
        public static void Subscrbe(IrcClient client, IrcMessageData messageData)
        {
            var serverInfo = Scanner.Configuration.GetServerInfo(client.Address);
            var channelInfo = serverInfo?.GetChannel(messageData.Channel);
            if (channelInfo == null)
                return;

            var branchName = messageData.MessageArray[1];
            channelInfo.RegisterListener(messageData.Nick, branchName);

            Scanner.WriteLine($"[COMMANDS] User {messageData.Nick} registered to updates on branch {branchName} on {client.Address} {messageData.Channel}");
        }

        [CommandHandler(".unnotify", "<branch_name>", 1)]
        public static void Unsubscribe(IrcClient client, IrcMessageData messageData)
        {
            var serverInfo = Scanner.Configuration.GetServerInfo(client.Address);
            var channelInfo = serverInfo?.GetChannel(messageData.Channel);
            if (channelInfo == null)
                return;

            var branchName = messageData.MessageArray[1];
            channelInfo.UnregisterListener(messageData.Nick, branchName);

            Scanner.WriteLine($"[COMMANDS] User {messageData.Nick} unregistered to updates on branch {branchName} on {client.Address} {messageData.Channel}");
        }

        [CommandHandler(".unload", 0)]
        public static void Unload(IrcClient client, IrcMessageData messageData)
        {
            var buildInfo = RemoteBuildManager.GetBuild(messageData.MessageArray[1]);
            if (buildInfo != null)
                buildInfo.Unload();
        }

        [CommandHandler(".downloadfile", "<build name string> <filePath>", 2)]
        public static void HandleDownloadFile(IrcClient client, IrcMessageData messageData)
        {
            if (!Scanner.Configuration.Proxy.Enabled)
            {
                client.SendReply(messageData, "Command disabled.");
                return;
            }

            if (!RemoteBuildManager.IsBuildKnown(messageData.MessageArray[1]))
            {
                client.SendReply(messageData, "Unknown build.");
                return;
            }

            CheckFileExists(messageData.MessageArray[1],
                buildInfo =>
                {
                    var remoteFileName = string.Join(" ", messageData.MessageArray.Skip(2));

                    var indexEntry = buildInfo.GetEntry(remoteFileName);
                    if (indexEntry == null)
                    {
                        client.SendReply(messageData, $"{messageData.Nick}: File does not exist.");
                        return;
                    }

                    // Give out link
                    // Disable resharper for readability
                    // ReSharper disable once UseStringInterpolation
                    var response = string.Format("http://{0}/{1}/{2}/{3}",
                        Scanner.Proxy.PublicDomain,
                        buildInfo.VersionName,
                        JenkinsHashing.Instance.ComputeHash(messageData.MessageArray[2]),
                        Path.GetFileName(remoteFileName.Replace('\\', '/')));

                    client.SendReply(messageData,
                        $"{messageData.Nick}: {response}");
                },
                buildInfo => {
                    client.SendReply(messageData,
                        $"{messageData.Nick}: Loading {buildInfo.VersionName} ...");
                },
                () => {
                    var buildInfo = RemoteBuildManager.GetBuild(messageData.MessageArray[1]);
                    if (buildInfo.Loading)
                    {
                        client.SendReply(messageData, $"{messageData.Nick}: Build {buildInfo.VersionName} is currently loading.");
                        return;
                    }

                    client.SendReply(messageData, $"{messageData.Nick}: Build {buildInfo.VersionName} has been loaded.");

                    if (!buildInfo.Install.Loaded)
                        client.SendReply(messageData, $"{messageData.Nick}: Install file could not be downloaded, expect erroneous results.");

                    Task.Delay(10 * 60 * 1000).ContinueWith(t => {
                        buildInfo.Expired = true;
                    });
                });
        }

        [CommandHandler(".listbuilds", 0)]
        public static void HandleListBuilds(IrcClient client, IrcMessageData messageData)
        {
            foreach (var kv in RemoteBuildManager.Builds)
                client.SendReply(messageData, $"* {kv.Value.VersionName}");
        }

        private static void CheckFileExists(string buildName,
            Action<BuildInfo> successHandler, Action<BuildInfo> failHandler, Action onReady)
        {
            var buildInfo = RemoteBuildManager.GetBuild(buildName);

            if (!buildInfo.Ready)
            {
                // Save memory by unloading old data.
                RemoteBuildManager.ClearExpiredBuilds();

                failHandler(buildInfo);
                buildInfo.OnReady += onReady;
                if (!buildInfo.Loading)
                    buildInfo.Prepare();
            }
            else
                successHandler(buildInfo);
        }
    }
}
