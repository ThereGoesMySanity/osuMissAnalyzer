using System.IO;
using ReplayAPI;

namespace OsuMissAnalyzer.Server.Database
{
    public class ServerReplayDb
    {
        private readonly OsuApi api;
        string folder;
        public ServerReplayDb(OsuApi api, string folder)
        {
            this.api = api;
            this.folder = folder;
        }
        public Replay GetReplayFromOnlineId(string onlineId)
        {
            string file = Path.Combine(folder, $"{onlineId}.osr");
            if (!File.Exists(file))
            {
                api.DownloadReplayFromId(onlineId, folder);
            }
            return new Replay(file);
        }
    }
}