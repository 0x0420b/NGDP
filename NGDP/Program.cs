using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;
using NGDP.Commands;
using NGDP.Local;
using NGDP.Network;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
#if !UNIX
using Colorful;
using Console = Colorful.Console;
#endif

namespace NGDP
{
    [Serializable, XmlRoot("configuration")]
    public class Configuration
    {
        [Serializable]
        public class ServerInfo
        {
            [XmlElement("address")]
            public string Address     { get; set; }

            [XmlElement("port")]
            public int Port           { get; set; }
 
            [XmlElement("channel")]
            public List<Channel> Channels { get; set; }
            
            [XmlElement("user")]
            public string Username    { get; set; }
        }

        [Serializable]
        public class Channel
        {
            [XmlElement("name")]
            public string Name { get; set; }

            [XmlElement("key")]
            public string Key { get; set; }
        }
        
        [XmlElement("server")]
        public List<ServerInfo> Servers { get; set; }
    }
    
    internal static class Program
    {
        private static Configuration Configuration { get; set; }
        
        private static Dictionary<string, IrcClient> _clients { get; } = new Dictionary<string, IrcClient>();
        private static int _connectionCount = 0;
        public static List<Channel> Channels { get; } = new List<Channel>();
        public static List<BuildInfo> Builds { get; } = new List<BuildInfo>();

        public static bool BinariesDownloadEnabled => false;

        public static bool HasProxy => _httpServer != null;
        private static HttpServer _httpServer;

        private static CancellationTokenSource _token = new CancellationTokenSource();
        private static string[] _startupArguments;

        private static Dictionary<string, List<string>> _subscribers = new Dictionary<string, List<string>>();

        #if !UNIX
        private static StyleSheet StyleSheet;
#endif

        public static Dictionary<string, string> FilesToDownload { get; } = new Dictionary<string, string>();
        public static string PUBLIC_DOMAIN;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Arguments:");
                Console.WriteLine("--conf, -c           Path the xml configuration file.");
                Console.WriteLine("--autodownload, -a   List of files to autodownload.");
                Console.WriteLine("--httpDomain, -d     Domain name where the bot can be reached.");
                Console.WriteLine("--bindAddr, -b       Address to bind the HTTP server to (typically 0.0.0.0)");
                Console.WriteLine("                     Implities --hasHttp");
                Console.WriteLine("--bindPort, -p       Public port to bind the HTTP server on.");
                Console.WriteLine("                     Implities --hasHttp");
                Console.WriteLine("--hasHttp, -h        Control wether or not http as active. Overrides any other implicit setting.");
                return;
            }

            #if !UNIX
            // Setup console
            StyleSheet = new StyleSheet(Color.White);
            StyleSheet.AddStyle(@"\[[0-9\/]+] [0-9:]+\ ?[AP]?M?]", Color.LightSkyBlue);
            StyleSheet.AddStyle(@"\[[A-Z]+\]", Color.Red);
            StyleSheet.AddStyle(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,4}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)", Color.Purple);
            StyleSheet.AddStyle(@" [0-9]+ ", Color.LightGreen);
            StyleSheet.AddStyle(@"#[-az0-9_-]", Color.DarkGreen);
            StyleSheet.AddStyle(@" [a-f0-9]{32}", Color.Orange);
            #endif

            _startupArguments = args;

            #region Create channel monitors
            Channels.Clear();
            Channels.Add(new Channel {
                ChannelName = "wow",
                DisplayName = "Retail",
            });

            Channels.Add(new Channel {
                ChannelName = "wowt",
                DisplayName = "PTR",
            });

            Channels.Add(new Channel {
                ChannelName = "wow_beta",
                DisplayName = "Beta",
            });

            Channels.Add(new Channel
            {
                ChannelName = "wow_internal",
                DisplayName = "Internal",
            });

            Channels.Add(new Channel
            {
                ChannelName = "wowdev",
                DisplayName = "Development",
            });
            #endregion

            foreach (var channel in Channels)
                channel.MessageEvent += OnMessageEvent;

            Console.CancelKeyPress += (s, ea) => {
                Program.WriteLine("[ERROR] Aborting ...");
                foreach (var knownServer in _clients)
                    knownServer.Value.Disconnect();
                _httpServer?.Stop();

                _token.Cancel();
            };
            
            var configurationFileName = GetStringParam("--conf", "-c", "conf.xml");
            var serializer = new XmlSerializer(typeof(Configuration));
            using (var reader = new StreamReader(configurationFileName))
                Configuration = (Configuration)serializer.Deserialize(reader);

            var autodownloadList = GetStringParam("--autoDownload", "-auto", null);
            
            PUBLIC_DOMAIN = GetStringParam("--httpDomain", "-d", "ngdp-warpten.c9users.io");
            var bindAddr = GetStringParam("--bindAddr", "-a", "0.0.0.0");
            var httpPort = GetIntParam("--bindPort", "-h", 8080);
            
            var hasHttp = Array.IndexOf(args, "--hasHttp") != -1;
            if (!hasHttp)
            {
                hasHttp = Array.IndexOf(args, "--bindAddr") != -1;
                if (!hasHttp)
                    hasHttp = Array.IndexOf(args, "--bindPort") != -1;
            }

            if (!string.IsNullOrEmpty(autodownloadList))
            {
                using (var reader = new StreamReader(autodownloadList))
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length == 0 || line[0] == '#')
                            continue;

                        var tokens = line.Split(new[] { " => " }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
                        if (tokens.Length == 0)
                            continue;
                        
                        FilesToDownload[tokens[0]] = tokens[1];
                        // Auto-add PDBs on the off chance.
                        if (tokens[0].Substring(tokens[0].Length - 4, 4) == ".exe")
                            FilesToDownload[tokens[0].Replace(".exe", ".pdb")] = tokens[1];
                    }
                }
            }

            if (hasHttp)
            {
                WriteLine("[HTTP] Listening on http://{1}:{0}", httpPort, PUBLIC_DOMAIN);

                Task.Factory.StartNew(() => { _httpServer = new HttpServer(httpPort); });
            }

            foreach (var serverInfo in Configuration.Servers)
            {
                WriteLine("[IRC] Connecting to {0}:{1}", serverInfo.Address, serverInfo.Port);

                var client = new IrcClient() {
                    SupportNonRfc = true,
                    ActiveChannelSyncing = true
                };

                client.OnConnected += (sender, eventArgs) => StartRequestingUpdates();
                
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
                
                _clients[serverInfo.Address] = client;
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
            
            foreach (var knownServer in _clients)
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

        private static void StartRequestingUpdates()
        {
            WriteLine("[IRC] Connected.");
            ++_connectionCount;
            if (_connectionCount != Configuration.Servers.Count)
                return;

            Task.Factory.StartNew(() =>
            {
                var silent = true;
                
                while (!_token.IsCancellationRequested)
                {
                    foreach (var channel in Channels)
                    {
                        channel.Update(silent);
#if !UNIX
                        break;
#endif
                    }
                    silent = false;

#if !UNIX
                    break;
#endif
                    Thread.Sleep(30000);
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

        private static int GetIntParam(string argumentName, string shortName, int defaultValue)
        {
            var idx = Array.IndexOf(_startupArguments, argumentName);
            if (idx == _startupArguments.Length - 1)
                return defaultValue;

            if (idx == -1)
                idx = Array.IndexOf(_startupArguments, shortName);

            // If not found or last argument
            if (idx == -1 || idx == _startupArguments.Length - 1)
                return defaultValue;

            return int.Parse(_startupArguments[idx + 1]);
        }

        public static void WriteLine(string fmt, params object[] args)
        {
            var subfmt = $"[{DateTime.Now}] {fmt}";

            #if UNIX
            Console.WriteLine(subfmt, args);
            #else
            Console.WriteLineStyled(StyleSheet, subfmt, args);
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
