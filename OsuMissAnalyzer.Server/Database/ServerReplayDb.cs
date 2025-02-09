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
using OsuMissAnalyzer.Server.OsuApi;
using ReplayAPI;

namespace OsuMissAnalyzer.Server.Database
{
    public class ServerReplayDb
    {
        private readonly OsuApiv2 api;
        private readonly IDataLogger dLog;
        private readonly ILogger<ServerReplayDb> logger;
        readonly string serverFolder;
        public ServerReplayDb(OsuApiv2 api, IOptions<ServerOptions> options, IDataLogger dLog, ILogger<ServerReplayDb> logger)
        {
            this.api = api;
            this.dLog = dLog;
            this.logger = logger;
            serverFolder = options.Value.ServerDir;
            Directory.CreateDirectory(Path.Combine(options.Value.ServerDir, "replays"));
        }

        public async Task<Replay?> GetReplay(ulong? id, ulong? legacyId)
        {
            if (!id.HasValue && !legacyId.HasValue) throw new ArgumentException("One ID is required");
            string folder = Path.Combine(serverFolder, "replays");
            if (!legacyId.HasValue)
            {
                legacyId = (await api.GetScorev2(id!.Value))!.LegacyScoreId;
            }

            string file = Path.Combine(folder, $"{legacyId}.osr");
            Replay? replay;
            if (File.Exists(file))
            {
                replay = new Replay(file);
                dLog.Log(DataPoint.ReplaysCacheHit);
            }
            else
            {
                dLog.Log(DataPoint.ReplaysCacheMiss);
                logger.LogInformation("replay not found, downloading...");

                replay = await api.DownloadReplayFromId(id ?? legacyId!.Value, !id.HasValue);
                replay?.Save(Path.Combine(folder, $"{replay.OnlineId}.osr"));
            }
            return (replay?.fullLoaded ?? false) ? replay : null;
        }
    }
}