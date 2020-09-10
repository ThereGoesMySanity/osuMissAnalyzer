using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Mono.Unix;
using Mono.Unix.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OsuMissAnalyzer.Server
{
    public enum Logging
    {
        AttachmentCalls,
        BotCalls,
        UserCalls,
        ReactionCalls,
        BeatmapsCacheHit,
        BeatmapsCacheMiss,
        ReplaysCacheHit,
        ReplaysCacheMiss,
        TokenExpiry,
        ApiGetUserv1,
        ApiGetBeatmapsv1,
        ApiGetReplayv1,
        ApiGetScorev2,
        ApiGetUserScoresv2,
        ApiGetBeatmapScoresv2,
        ApiDownloadBeatmap,
        EventsHandled,
        ServersJoined,
        CachedMessages,
        BeatmapsDbSize,
    }
    public enum Format
    {
        CSV, JSON
    }
    public class Logger
    {
        private const string ENDPOINT = "/run/missanalyzer-server";
        public static Logger Instance { get; set; }
        private StreamWriter file;
        private int[] counts;


        private Socket socket;
        private UnixEndPoint endpoint;
        public event Action UpdateLogs;

        public Logger(string logFile)
        {
            file = new StreamWriter(logFile, true);
            counts = new int[Enum.GetValues(typeof(Logging)).Length];
            if (File.Exists(ENDPOINT)) File.Delete(ENDPOINT);
            socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            endpoint = new UnixEndPoint(ENDPOINT);
            socket.Bind(endpoint);
            socket.Listen(1);
            socket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            var fileInfo = new Mono.Unix.UnixFileInfo(ENDPOINT);
            Syscall.chown(ENDPOINT, Syscall.getuid(), Syscall.getgrnam("netdata").gr_gid);
            Syscall.chmod(ENDPOINT, FilePermissions.S_IWGRP | FilePermissions.S_IWUSR | FilePermissions.S_IRGRP | FilePermissions.S_IRUSR);
        }
        public void Close()
        {
            file.Close();
            socket.Close();
            File.Delete(ENDPOINT);
        }
        public void AcceptCallback(IAsyncResult result)
        {
            // if (!socket.Connected) return;
            try
            {
                Socket handler = socket.EndAccept(result);

                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);

                socket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            catch (Exception e) { if (!(e is Exception)) Console.WriteLine(e); }
        }
        public void ReadCallback(IAsyncResult result)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)result.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(result);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                content = Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead);

                if (content.StartsWith("GET "))
                {
                    UpdateLogs();

                    string[] opts = content.Substring(4).Split(' ');
                    if (opts.Length == 1 && opts[0].ToLower() == "all")
                    {
                        byte[] byteData = Encoding.ASCII.GetBytes(GetStats(Format.JSON));
                        handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
                    }
                    else
                    {
                        opts.Select(TryParse).Where(e => e.HasValue).Select(e => e.Value);
                    }
                    file.WriteLine(GetStats(Format.CSV));
                }

                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
        }
        public void SendCallback(IAsyncResult result)
        {
            Socket handler = (Socket)result.AsyncState;

            handler.EndSend(result);
        }
        public string GetStats(Format format)
        {
            switch (format)
            {
                case Format.JSON:
                    return new JObject(Enum.GetNames(typeof(Logging)).Zip(counts, (a, b) => new JProperty(a, b))).ToString(Formatting.None);
                case Format.CSV:
                    return $"{DateTime.UtcNow:O},{string.Join(",", counts)}";
            }
            return null;
        }
        public static void Log(Logging type, int count = 1)
        {
            if (Instance == null) return;
            Instance.counts[(int)type] += count;
        }
        public static void LogAbsolute(Logging type, int value)
        {
            if (Instance == null) return;
            Instance.counts[(int)type] = value;
        }
        private static Logging? TryParse(String s)
        {
            Logging logging;
            Logging? var = null;
            if (Enum.TryParse<Logging>(s, true, out logging))
            {
                var = logging;
            }
            return var;
        }
    }
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Client socket.
        public Socket workSocket = null;
    }

}