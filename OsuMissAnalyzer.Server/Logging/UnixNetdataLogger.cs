using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Unix;
using Mono.Unix.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server.Logging
{
    public enum Format
    {
        CSV, JSON
    }
    public class UnixNetdataLogger : IDataLogger
    {
        private const string ENDPOINT = "/run/missanalyzer-server";

        private StreamWriter file;
        private int[] counts;

        private Socket? socket;
        private UnixEndPoint? endpoint;
        private readonly ILogger<UnixNetdataLogger> logger;

        public event Action? UpdateLogs;

        public UnixNetdataLogger(IOptions<ServerOptions> options, ILogger<UnixNetdataLogger> logger)
        {
            file = new StreamWriter(Path.Combine(options.Value.ServerDir, "log.csv"), true);
            counts = new int[Enum.GetValues<DataPoint>().Length];
            this.logger = logger;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (File.Exists(ENDPOINT)) File.Delete(ENDPOINT);
            
            socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            endpoint = new UnixEndPoint(ENDPOINT);
            try
            {
                socket.Bind(endpoint);
                socket.Listen(1);
                socket.BeginAccept(new AsyncCallback(AcceptCallback), null);
                var fileInfo = new UnixFileInfo(ENDPOINT);
                Syscall.chown(ENDPOINT, Syscall.getuid(), Syscall.getgrnam("netdata").gr_gid);
                Syscall.chmod(ENDPOINT, FilePermissions.S_IWGRP | FilePermissions.S_IWUSR | FilePermissions.S_IRGRP | FilePermissions.S_IRUSR);
            } catch (Exception) 
            {
                socket.Close();
                socket = null;
            }
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            file.Close();
            socket?.Close();
            File.Delete(ENDPOINT);
            await Task.CompletedTask;
        }

        private void AcceptCallback(IAsyncResult result)
        {
            // if (!socket.Connected) return;
            try
            {
                Socket handler = socket!.EndAccept(result);

                // Create the state object.
                StateObject state = new()
                {
                    workSocket = handler
                };
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);

                socket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception on socket connection");
            }
        }
        private void ReadCallback(IAsyncResult result)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)result.AsyncState!;
            Socket handler = state.workSocket!;

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(result);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                content = Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead);

                if (content.StartsWith("GET "))
                {
                    UpdateLogs?.Invoke();

                    string[] opts = content[4..].Split(' ');
                    Func<DataPoint, bool>? filter = null;
                    if (opts.Length != 1 || !opts[0].Equals("all", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var points = opts.Select(TryParse).Where(e => e.HasValue).Select(e => e!.Value);
                        filter = d => points.Contains(d);
                    }
                    byte[] byteData = Encoding.ASCII.GetBytes(GetStats(Format.JSON, filter));
                    handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
                    file.WriteLine(GetStats(Format.CSV));
                }

                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
        }
        private void SendCallback(IAsyncResult result)
        {
            Socket handler = (Socket)result.AsyncState!;

            handler.EndSend(result);
        }
        private string GetStats(Format format, Func<DataPoint, bool>? filter = null)
        {
            if (!Enum.IsDefined(format))
                throw new ArgumentException($"Invalid format {format}", nameof(format));
            


            var dataPoints = Enum.GetValues<DataPoint>();
            if (filter is not null) dataPoints = [.. dataPoints.Where(filter)];

#pragma warning disable CS8524
            return format switch
            {
                Format.JSON => new JObject(dataPoints.Select(d => new JProperty(d.ToString(), counts[(int)d]))).ToString(Formatting.None),
                Format.CSV => $"{DateTime.UtcNow:O},{string.Join(",", counts)}"
            };
#pragma warning restore CS8524
        }
        public void Log(DataPoint type) => Log(type, 1);
        public void Log(DataPoint type, int count)
        {
            counts[(int)type] += count;
        }
        public void LogAbsolute(DataPoint type, int value)
        {
            counts[(int)type] = value;
        }

        private static DataPoint? TryParse(string s)
        {
            if (Enum.TryParse(s, true, out DataPoint var))
            {
                return var;
            }
            return null;
        }
    }
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Client socket.
        public Socket? workSocket = null;
    }
}