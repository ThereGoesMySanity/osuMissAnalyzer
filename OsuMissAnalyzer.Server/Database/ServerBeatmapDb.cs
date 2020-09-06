using System.Collections.Generic;
using System.IO;
using System.Net;
using BMAPI.v1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OsuMissAnalyzer.Core;
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
        public Beatmap GetBeatmap(string mapHash)
        {
            if (!hashes.ContainsKey(mapHash))
            {
                hashes[mapHash] = api.DownloadBeatmapFromHashv1(mapHash, folder);
            }
            return new Beatmap(Path.Combine(folder, "beatmaps", $"{hashes[mapHash]}.osu"));
        }
        public Beatmap GetBeatmapFromId(string beatmap_id)
        {
            string file = Path.Combine(folder, "beatmaps", $"{beatmap_id}.osu");
            if (!File.Exists(file))
            {
                api.DownloadBeatmapFromId(beatmap_id, folder);
                hashes[Beatmap.MD5FromFile(file)] = beatmap_id;
            }
            return new Beatmap(file);
        }
    }
}