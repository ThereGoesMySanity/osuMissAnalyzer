using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BMAPI.v1;
using Newtonsoft.Json;

namespace OsuMissAnalyzer.Tests
{
    public class ServerBeatmapDb
    {
        private readonly OsuApi api;
        string folder;
        public Dictionary<string, string> hashes;
        public ServerBeatmapDb(OsuApi api, string serverDir, bool reload = false)
        {
            this.api = api;
            folder = serverDir;
            string db = Path.Combine(serverDir, "beatmaps.db");
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
                foreach (var file in Directory.EnumerateFiles(Path.Combine(serverDir, "beatmaps")))
                {
                    hashes[Beatmap.MD5FromFile(file)] = Path.GetFileNameWithoutExtension(file);
                }
            }
        }
        public void Close()
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
                    var result = await api.DownloadBeatmapFromHashv1(mapHash, Path.Combine(folder, "beatmaps"));
                    if (result != null)
                    {
                        hashes[mapHash] = result;
                    }
                    else
                    {
                        return null;
                    }
                }
                return new Beatmap(Path.Combine(folder, "beatmaps", $"{hashes[mapHash]}.osu"));
            }
            return null;
        }
        public async Task<Beatmap> GetBeatmapFromId(string beatmap_id, bool forceRedl = false)
        {
            string file = Path.Combine(folder, "beatmaps", $"{beatmap_id}.osu");
            if (!File.Exists(file) || forceRedl)
            {
                await api.DownloadBeatmapFromId(beatmap_id, Path.Combine(folder, "beatmaps"));
                string hash = Beatmap.MD5FromFile(file);
                hashes[hash] = beatmap_id;
            }
            return new Beatmap(file);
        }
    }
}