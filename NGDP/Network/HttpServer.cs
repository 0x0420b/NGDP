using System;using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NGDP.Local;
using NGDP.NGDP;
using NGDP.Utilities;

namespace NGDP.Network
{
    public class HttpServer
    {
        private CancellationTokenSource _token;
        private HttpListener _listener;

        public int Port { get; private set; }
        public string PublicDomain { get; }
        public string LocalDomain { get; }

        public HttpServer(string localAddress, string publicDomain)
        {
            LocalDomain = localAddress;
            PublicDomain = publicDomain;
        }

        /// <summary>
        /// Construct server with suitable port.
        /// </summary>
        public void Listen(CancellationTokenSource token)
        {
            // Get an empty port
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();

            Listen(port, token);
        }

        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            _token.Cancel();
            _listener.Stop();
        }

        public void Listen(int port, CancellationTokenSource token)
        {
            Port = port;
            _token = token;

            Task.Run(() =>
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://" + LocalDomain + ":" + Port + "/");
                _listener.Start();

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var context = _listener.GetContext();
                        Process(context);
                    }
                    catch (Exception ex)
                    {
                        Scanner.WriteLine(ex.ToString());
                    }
                }
            }, token.Token);
        }

        private static void WriteError(HttpListenerContext context, string messageBody, HttpStatusCode statusCode)
        {
            context.Response.ContentType = "text/plain";
            context.Response.ContentLength64 = messageBody.Length;
            context.Response.AddHeader("Connection", "close");

            context.Response.StatusCode = (int)statusCode;
            using (var writer = new StreamWriter(context.Response.OutputStream))
                writer.Write(messageBody);

            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
        }

        private static void Process(HttpListenerContext context)
        {
            var tokens = context.Request.Url.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                WriteError(context, "Badly formatted request.", HttpStatusCode.BadRequest);
                return;
            }
            
            var buildName = tokens[0];
            var fileHash = ulong.Parse(tokens[1]);
            var fileName = tokens[2];

            var buildInfo = RemoteBuildManager.GetBuild(buildName);
            if (buildInfo == null)
            {
                WriteError(context, "Unknown build.", HttpStatusCode.BadRequest);
                return;
            }

            var fileEntry = buildInfo.GetEntry(fileHash);
            if (fileEntry == null)
            {
                WriteError(context, "This build is currently loading. Try again later.", HttpStatusCode.BadRequest);
                return;
            }

            try
            {
                using (var blte = new BLTE(buildInfo.ServerInfo.Hosts[0]))
                {
                    if (fileEntry.ArchiveIndex != -1)
                        blte.AddHeader("Range", $"bytes={fileEntry.Offset}-{fileEntry.Offset + fileEntry.Size - 1}");

                    var archiveName = fileEntry.Hash.ToHexString();
                    if (fileEntry.ArchiveIndex != -1)
                        archiveName = buildInfo.Indices.Archives[fileEntry.ArchiveIndex].ToHexString();

                    blte.Send($"/{buildInfo.ServerInfo.Path}/data/{archiveName.Substring(0, 2)}/{archiveName.Substring(2, 2)}/{archiveName}");

                    Scanner.WriteLine($"[PROXY] Serving {fileName} through {blte.URL}.");

                    if (!blte.Failed)
                    {
                        context.Response.ContentType = "application/octet-stream";
                        context.Response.ContentLength64 = blte.DecompressedLength;
                        context.Response.AddHeader("Date", blte.ResponseHeaders.Get("Date"));
                        context.Response.AddHeader("ETag", blte.ResponseHeaders.Get("ETag"));
                        context.Response.AddHeader("Last-Modified", DateTime.Now.ToString("r"));
                        context.Response.AddHeader("Connection", "Keep-Alive");

                        context.Response.StatusCode = (int)HttpStatusCode.OK;

                        Scanner.WriteLine($"[PROXY] Serving {fileName} to {context.Request.RemoteEndPoint} - {archiveName}");

                        blte.PipeTo(context.Response.OutputStream);
                    }
                    else
                    {
                        var sBuilder = new StringBuilder();
                        sBuilder.AppendLine($"File {fileName} cannot be downloaded - it might be an encrypted archive...");
                        sBuilder.AppendLine();
                        sBuilder.AppendLine($"Requested archive: {blte.URL}");
                        if (fileEntry.ArchiveIndex != -1)
                            sBuilder.AppendLine($"Range: {fileEntry.Offset}-{fileEntry.Offset + fileEntry.Size - 1}");
                        WriteError(context, sBuilder.ToString(), HttpStatusCode.InternalServerError);
                    }
                }
            }
            catch (IOException ioe)
            {
                Scanner.WriteLine("[PROXY] Remote client closed the connection.");
            }
            catch (Exception e)
            {
                Scanner.WriteLine(e.ToString());

                WriteError(context, e.ToString(), HttpStatusCode.InternalServerError);
            }
        }
    }
}
