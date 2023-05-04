using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BMAPI.v1;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server.Database
{
    public class ServerBeatmapDb : IDisposable
    {
        private readonly OsuApi api;
        private readonly IDataLogger logger;
        string folder;
        Dictionary<string, string> hashes;
        public ServerBeatmapDb(OsuApi api, ServerOptions options, IConfiguration configuration, IDataLogger logger)
        {
            var reload = bool.Parse(configuration["ReloadDb"] ?? "false");
            this.api = api;
            this.logger = logger;
            folder = options.ServerDir;
            Directory.CreateDirectory(Path.Combine(options.ServerDir, "beatmaps"));
            string db = Path.Combine(options.ServerDir, "beatmaps.db");
            if (File.Exists(db) && !reload)
            {
                hashes = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(db));
            }
            else
            {
                hashes = new Dictionary<string, string>();
            }
            if (reload)
            {
                foreach (var file in Directory.EnumerateFiles(Path.Combine(options.ServerDir, "beatmaps")))
                {
                    hashes[Beatmap.MD5FromFile(file)] = Path.GetFileNameWithoutExtension(file);
                }
            }
            logger.LogAbsolute(DataPoint.BeatmapsDbSize, hashes.Count);
        }

        public void Dispose()
        {
            using (StreamWriter writer = File.CreateText(Path.Combine(folder, "beatmaps.db")))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, hashes);
            }
        }

        public async Task<Beatmap> GetBeatmap(string mapHash)
        {
            if (!string.IsNullOrEmpty(mapHash))
            {
                if (!hashes.ContainsKey(mapHash))
                {
                    await Logger.WriteLine("beatmap not found, downloading...");
                    var result = await api.DownloadBeatmapFromHashv1(mapHash, Path.Combine(folder, "beatmaps"));
                    if (result != null)
                    {
                        hashes[mapHash] = result;
                        logger.LogAbsolute(DataPoint.BeatmapsDbSize, hashes.Count);
                        logger.Log(DataPoint.BeatmapsCacheMiss);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    logger.Log(DataPoint.BeatmapsCacheHit);
                }
                return new Beatmap(Path.Combine(folder, "beatmaps", $"{hashes[mapHash]}.osu"));
            }
            return null;
        }
        public async Task<Beatmap> GetBeatmapFromId(string beatmap_id, bool forceRedl = false)
        {
            string file = Path.Combine(folder, "beatmaps", $"{beatmap_id}.osu");
            if (forceRedl) File.Delete(file);
            if (!File.Exists(file))
            {
                await Logger.WriteLine(forceRedl? "hash out of date, redownloading..." : "beatmap not found, downloading...");
                await api.DownloadBeatmapFromId(beatmap_id, Path.Combine(folder, "beatmaps"), forceRedl);
                string hash = Beatmap.MD5FromFile(file);
                hashes[hash] = beatmap_id;
                logger.LogAbsolute(DataPoint.BeatmapsDbSize, hashes.Count);
                logger.Log(DataPoint.BeatmapsCacheMiss);
            }
            else
            {
                logger.Log(DataPoint.BeatmapsCacheHit);
            }
            return new Beatmap(file);
        }
    }
}