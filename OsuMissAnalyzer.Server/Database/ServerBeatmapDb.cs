using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BMAPI.v1;
using Newtonsoft.Json;
namespace OsuMissAnalyzer.Server.Database
{
    public class ServerBeatmapDb
    {
        private readonly OsuApi api;
        string folder;
        Dictionary<string, string> hashes;
        public ServerBeatmapDb(OsuApi api, string serverDir)
        {
            this.api = api;
            folder = serverDir;
            string db = Path.Combine(serverDir, "beatmaps.db");
            if (File.Exists(db))
            {
                hashes = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(db));
                Logger.LogAbsolute(Logging.BeatmapsDbSize, hashes.Count);
            }
            else
            {
                hashes = new Dictionary<string, string>();
            }
        }
        public void Close()
        {
            using (FileStream file = File.OpenWrite(Path.Combine(folder, "beatmaps.db")))
            using (StreamWriter writer = new StreamWriter(file))
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
                        Logger.LogAbsolute(Logging.BeatmapsDbSize, hashes.Count);
                        Logger.Log(Logging.BeatmapsCacheMiss);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    Logger.Log(Logging.BeatmapsCacheHit);
                }
                return new Beatmap(Path.Combine(folder, "beatmaps", $"{hashes[mapHash]}.osu"));
            }
            return null;
        }
        public async Task<Beatmap> GetBeatmapFromId(string beatmap_id)
        {
            string file = Path.Combine(folder, "beatmaps", $"{beatmap_id}.osu");
            if (!File.Exists(file))
            {
                await Logger.WriteLine("beatmap not found, downloading...");
                await api.DownloadBeatmapFromId(beatmap_id, Path.Combine(folder, "beatmaps"));
                hashes[Beatmap.MD5FromFile(file)] = beatmap_id;
                Logger.LogAbsolute(Logging.BeatmapsDbSize, hashes.Count);
                Logger.Log(Logging.BeatmapsCacheMiss);
            }
            else
            {
                Logger.Log(Logging.BeatmapsCacheHit);
            }
            return new Beatmap(file);
        }
    }
}