﻿using System.Threading.Tasks;
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
        [CommandHandler(".register", ".listen <channel name>", 1)]
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

            channel.MessageEvent += Scanner.OnMessageEvent;

            Scanner.Channels.Add(channel);
        }

        [CommandHandler(".leave", 0)]
        public static void LeaveChannel(IrcClient client, IrcMessageData messageData)
        {
            var channelName = messageData.Channel;
            if (Scanner.Channels.Any(c => c.ChannelName == channelName))
                return;

            var channel = new Channel
            {
                ChannelName = channelName,
                DisplayName = channelName,
            };

            channel.MessageEvent += Scanner.OnMessageEvent;

            Scanner.Channels.Add(channel);
        }

        [CommandHandler(".forceupdate", ".forceupdate <branch_name>", 1)]
        public static void ForceChannelUpdate(IrcClient client, IrcMessageData messageData)
        {
            var channelName = messageData.MessageArray[1];
            var channel = Scanner.Channels.FirstOrDefault(p => p.ChannelName == channelName);
            if (channel == null)
                client.SendReply(messageData, "Unknown branch (try adding it first?)");
            else
                channel.Update(false);
        }

        [CommandHandler(".subscribe", 0)]
        public static void Subscrbe(IrcClient client, IrcMessageData messageData)
        {
            Scanner.Subscribe(messageData.From, messageData.Channel);
        }

        [CommandHandler(".unsubscribe", 0)]
        public static void Unsubscribe(IrcClient client, IrcMessageData messageData)
        {
            Scanner.Unsubscribe(messageData.From);
        }

        [CommandHandler(".unload", 0)]
        public static void Unload(IrcClient client, IrcMessageData messageData)
        {
            var buildInfo = RemoteBuildManager.GetBuild(messageData.MessageArray[1]);
            buildInfo.Unload();
        }

        [CommandHandler(".downloadfile", ".downloadfile <build name string> <filePath>", -1)]
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
