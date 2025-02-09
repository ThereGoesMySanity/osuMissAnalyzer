using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using BMAPI.v1;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OsuMissAnalyzer.Server.Logging;
using OsuMissAnalyzer.Server.OsuApi;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server.Database
{
    public class ServerBeatmapDb : IDisposable
    {
        private readonly OsuApiv2 api;
        private readonly IDataLogger dLog;
        private readonly ILogger<ServerBeatmapDb> logger;
        string serverDir;
        readonly Dictionary<string, string> hashes = [];
        public ServerBeatmapDb(OsuApiv2 api, IOptions<ServerOptions> options, IConfiguration configuration, IDataLogger dLog, ILogger<ServerBeatmapDb> logger)
        {
            var reload = bool.Parse(configuration["ReloadDb"] ?? "false");
            this.api = api;
            this.dLog = dLog;
            this.logger = logger;
            serverDir = options.Value.ServerDir;
            Directory.CreateDirectory(Path.Combine(serverDir, "beatmaps"));
            string db = Path.Combine(serverDir, "beatmaps.db");
            if (File.Exists(db) && !reload)
            {
                hashes = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(db)) ?? [];
            }
            if (reload)
            {
                foreach (var file in Directory.EnumerateFiles(Path.Combine(serverDir, "beatmaps")))
                {
                    hashes[Beatmap.MD5FromFile(file)] = Path.GetFileNameWithoutExtension(file);
                }
            }
            dLog.LogAbsolute(DataPoint.BeatmapsDbSize, hashes.Count);
        }

        public void Dispose()
        {
            using StreamWriter writer = File.CreateText(Path.Combine(serverDir, "beatmaps.db"));
            JsonSerializer serializer = new();
            serializer.Serialize(writer, hashes);
        }

        public async Task<Beatmap?> GetBeatmap(string mapHash)
        {
            if (!string.IsNullOrEmpty(mapHash))
            {
                if (hashes.TryGetValue(mapHash, out string? beatmapId) 
                    && File.Exists(DbPath(beatmapId)))
                {
                    dLog.Log(DataPoint.BeatmapsCacheHit);
                    return new Beatmap(DbPath(beatmapId));
                }
                else
                {
                    logger.LogInformation("beatmap not found, downloading...");
                    var result = await api.DownloadBeatmapFromHash(mapHash, Path.Combine(serverDir, "beatmaps"));
                    if (result != null)
                    {
                        hashes[mapHash] = result.Id.ToString();
                        dLog.LogAbsolute(DataPoint.BeatmapsDbSize, hashes.Count);
                        dLog.Log(DataPoint.BeatmapsCacheMiss);
                        return new Beatmap(DbPath(result.Id));
                    }
                }
            }
            return null;
        }
        public async Task<Beatmap> GetBeatmapFromId(ulong beatmapId, bool forceRedl = false)
        {
            var file = DbPath(beatmapId);
            if (forceRedl) File.Delete(file);
            if (!File.Exists(file))
            {
                logger.LogInformation(forceRedl? "hash out of date, redownloading..." : "beatmap not found, downloading...");
                await api.DownloadBeatmapFromId(beatmapId, Path.Combine(serverDir, "beatmaps"), forceRedl);
                string hash = Beatmap.MD5FromFile(file);
                hashes[hash] = beatmapId.ToString();
                dLog.LogAbsolute(DataPoint.BeatmapsDbSize, hashes.Count);
                dLog.Log(DataPoint.BeatmapsCacheMiss);
            }
            else
            {
                dLog.Log(DataPoint.BeatmapsCacheHit);
            }
            return new Beatmap(file);
        }

        private string DbPath(ulong beatmapId) => DbPath(beatmapId.ToString());
        private string DbPath(string beatmapId) => Path.Combine(serverDir, "beatmaps", $"{beatmapId}.osu");

    }
}