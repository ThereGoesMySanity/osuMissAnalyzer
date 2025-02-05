using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BMAPI.v1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OsuMissAnalyzer.Server.Logging;
using OsuMissAnalyzer.Server.Settings;
using ReplayAPI;

namespace OsuMissAnalyzer.Server.Database
{
    public class ServerReplayDb
    {
        private readonly OsuApi api;
        private readonly IDataLogger dLog;
        private readonly ILogger<ServerReplayDb> logger;
        string serverFolder;
        public ServerReplayDb(OsuApi api, IOptions<ServerOptions> options, IDataLogger dLog, ILogger<ServerReplayDb> logger)
        {
            this.api = api;
            this.dLog = dLog;
            this.logger = logger;
            this.serverFolder = options.Value.ServerDir;
            Directory.CreateDirectory(Path.Combine(options.Value.ServerDir, "replays"));
        }

        public async Task<Replay?> GetReplayFromOnlineId(ulong onlineId)
        {
            string file = Path.Combine(serverFolder, "replays", $"{onlineId}.osr");
            Replay? replay = null;
            if (!File.Exists(file))
            {
                dLog.Log(DataPoint.ReplaysCacheMiss);
                logger.LogInformation("replay not found, downloading...");

                replay = await api.DownloadReplayFromId(onlineId);
                replay.Save(file);
            }
            else
            {
                replay = new Replay(file);
                dLog.Log(DataPoint.ReplaysCacheHit);
            }
            return replay.fullLoaded? replay : null;
        }
        public Task<Replay?> GetReplayFromScore(JToken score)
        {
            return GetReplayFromOnlineId((ulong)score["best_id"]!);
        }
    }
}