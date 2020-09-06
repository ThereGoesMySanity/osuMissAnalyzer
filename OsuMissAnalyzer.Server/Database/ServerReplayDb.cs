using System;
using System.IO;
using ReplayAPI;

namespace OsuMissAnalyzer.Server.Database
{
    public class ServerReplayDb
    {
        private readonly OsuApi api;
        string serverFolder;
        public ServerReplayDb(OsuApi api, string serverFolder)
        {
            this.api = api;
            this.serverFolder = serverFolder;
        }
        public Replay GetReplayFromOnlineId(string onlineId)
        {
            string file = Path.Combine(serverFolder, "replays", $"{onlineId}.osr");
            if (!File.Exists(file))
            {
                Console.WriteLine("test1");
                api.DownloadReplayFromId(onlineId, Path.Combine(serverFolder, "replays"));
            }
            Console.WriteLine("test");
            return new Replay(file);
        }
    }
}