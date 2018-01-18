using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;
using NGDP.Commands;
using NGDP.Local;
using NGDP.Network;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Concurrent;
using NGDP.Xml;
#if !UNIX
using Colorful;
using Console = Colorful.Console;
#endif

namespace NGDP
{
    internal static class Scanner
    {
        /// <summary>
        /// XML configuration, deserialized.
        /// </summary>
        public static Configuration Configuration { get; private set; }
        
        /// <summary>
        /// List of irc servers we are connected to.
        /// </summary>
        private static Dictionary<string, IrcClient> _ircClients { get; } = new Dictionary<string, IrcClient>();

        /// <summary>
        /// Counds the amount of open connections.
        /// </summary>
        private static int _connectionCount = 0;
        
        /// <summary>
        /// The list of currently controlled channels.
        /// </summary>
        public static List<Channel> Channels { get; } = new List<Channel>();

        /// <summary>
        /// The HTTP proxy itself.
        /// </summary>
        public static HttpServer Proxy { get; private set; }

        private static CancellationTokenSource _token { get; } = new CancellationTokenSource();

        private static string[] _startupArguments;

        /// <summary>
        /// Users that want to get notified of an update.
        /// </summary>
        private static Dictionary<string, List<string>> _subscribers = new Dictionary<string, List<string>>();

        private static ConcurrentQueue<BuildInfo> _pendingUpdatesBuilds = new ConcurrentQueue<BuildInfo>();

#if !UNIX
        private static StyleSheet _styleSheet;
#endif

        public static void Main(string[] args)
        {
#if !UNIX && !DEBUG
            if (args.Length == 0)
            {
                Console.WriteLine("Arguments:");
                Console.WriteLine("--conf, -c           Path the xml configuration file.");
                return;
            }
#endif

#if !UNIX
            // Setup console
            _styleSheet = new StyleSheet(Color.White);
            _styleSheet.AddStyle(@"[0-9\/]+ [0-9:]+ (?:A|P)M", Color.SlateBlue);
            _styleSheet.AddStyle(@"\[[A-Z]+\]", Color.Gold);
            _styleSheet.AddStyle(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,4}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)", Color.Purple);
            _styleSheet.AddStyle(@"#[-az0-9_-]", Color.DarkGreen);
            _styleSheet.AddStyle(@" [a-f0-9]{32}", Color.Orange);
            _styleSheet.AddStyle(@"[a-z_-]+\-[0-9]{5}patch[0-9]\.[0-9]\.[0-9]_[A-Za-z]+", Color.Firebrick);
#endif

            _startupArguments = args;

            Console.CancelKeyPress += (s, ea) => {
                WriteLine("[ERROR] Aborting ...");
                foreach (var knownServer in _ircClients)
                    knownServer.Value.Disconnect();

                Proxy?.Stop();

                _token.Cancel();
            };

            #region Load XML configuration file
            var configurationFileName = GetStringParam("--conf", "-c", "conf.xml");
            var serializer = new XmlSerializer(typeof(Configuration));
            using (var reader = new StreamReader(configurationFileName))
                Configuration = (Configuration)serializer.Deserialize(reader);
            #endregion

            // Read channels from XML
            foreach (var channelInfo in Configuration.Branches)
            {
                var newChannel = new Channel() {ChannelName = channelInfo.Name, DisplayName = channelInfo.Description};
                newChannel.MessageEvent += OnMessageEvent;

                Channels.Add(newChannel);
            }

            #region HTTP Proxy setup

            if (Configuration.Proxy.Enabled)
            {
                WriteLine("[HTTP] Listening on http://{1}:{0}", Configuration.Proxy.PublicDomainName, Configuration.Proxy.BindPort);

                Proxy = new HttpServer(Configuration.Proxy.Endpoint, Configuration.Proxy.PublicDomainName);
                Proxy.Listen(Configuration.Proxy.BindPort, _token);
            }
            #endregion
            
            // Setup IRC clients
            foreach (var serverInfo in Configuration.Servers)
            {
                WriteLine("[IRC] Connecting to {0}:{1}", serverInfo.Address, serverInfo.Port);

                var client = new IrcClient() {
                    SupportNonRfc = true,
                    ActiveChannelSyncing = true
                };

                client.OnConnected += (sender, eventArgs) => StartThreads();
                
                client.OnChannelMessage += (sender, eventArgs) => {
                    Dispatcher.Dispatch(eventArgs.Data, client);
                };
                
                client.Connect(serverInfo.Address, serverInfo.Port);
                client.Login(serverInfo.Username, serverInfo.Username, 0, serverInfo.Username);
                foreach (var channelInfo in serverInfo.Channels)
                {
                    if (string.IsNullOrEmpty(channelInfo.Key))
                        client.RfcJoin("#" + channelInfo.Name);
                    else
                        client.RfcJoin("#" + channelInfo.Name, channelInfo.Key);
                }

                Task.Run(() => { client.Listen(); });
                
                _ircClients[serverInfo.Address] = client;
            }

            while (!_token.IsCancellationRequested)
            {
                if (_token.Token.WaitHandle.WaitOne(60000))
                    break;

                RemoteBuildManager.ClearExpiredBuilds();
            }

            RemoteBuildManager.ClearExpiredBuilds();
        }

        public static void OnMessageEvent(SendType type, string message)
        {
            if (type != SendType.Message)
                return;
            
            foreach (var knownServer in _ircClients)
            {
                foreach (var channelName in knownServer.Value.JoinedChannels)
                {
                    knownServer.Value.SendMessage(type, channelName, message);

                    var usersToPoke = new HashSet<string>();

                    foreach (var sub in _subscribers)
                        if (sub.Value.Contains(channelName))
                            usersToPoke.Add(sub.Key);

                    if (usersToPoke.Count != 0)
                        knownServer.Value.SendMessage(type, channelName, $"{string.Join(", ", usersToPoke)}: Ping!");
                }
            }
        }

        public static void QueueInitialUpdate(BuildInfo build)
        {
            _pendingUpdatesBuilds.Enqueue(build);
        }

        private static void StartThreads()
        {
            ++_connectionCount;
            if (_connectionCount != Configuration.Servers.Count)
                return;
            
            WriteLine("[IRC] Connected.");

            Task.Run(async () =>
            {
                while (!_token.IsCancellationRequested)
                {
                    if (_pendingUpdatesBuilds.TryDequeue(out var currentBuildInfo))
                        await currentBuildInfo.Prepare(true);

                    _token.Token.WaitHandle.WaitOne(5000);
                }

            }, _token.Token).ConfigureAwait(false);

            Task.Run(() =>
            {
                foreach (var channel in Channels)
                    channel.Update(true); // Initial update is silent to avoid spamming IRC on connect.

                while (!_token.IsCancellationRequested)
                {
                    foreach (var channel in Channels)
                        channel.Update(false);

                    _token.Token.WaitHandle.WaitOne(30000);
                }
            }, _token.Token).ConfigureAwait(false);
        }

        private static string GetStringParam(string argumentName, string shortName, string defaultValue)
        {
            var idx = Array.IndexOf(_startupArguments, argumentName);
            if (idx == _startupArguments.Length - 1)
                return defaultValue;

            if (idx == -1)
                idx = Array.IndexOf(_startupArguments, shortName);

            // If not found or last argument
            if (idx == -1 || idx == _startupArguments.Length - 1)
                return defaultValue;

            return _startupArguments[idx + 1];
        }
        
        public static void WriteLine(string fmt, params object[] args)
        {
            var subfmt = $"[{DateTime.Now}] {fmt}";

#if UNIX
            Console.WriteLine(subfmt, args);
#else
            Console.WriteLineStyled(_styleSheet, subfmt, args);
#endif
        }

        public static void Subscribe(string userName, string channel)
        {
            if (!_subscribers.TryGetValue(userName, out var s))
                _subscribers[userName] = s = new List<string>();

            s.Add(channel);
        }

        public static void Unsubscribe(string userName)
        {
            _subscribers.Remove(userName);
        }
    }
}
