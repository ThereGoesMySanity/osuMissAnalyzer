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
        public ServerBeatmapDb(OsuApi api, string beatmapFolder)
        {
            this.api = api;
            folder = beatmapFolder;
            hashes = JsonConvert.DeserializeObject<Dictionary<string, string>>(Path.Combine(beatmapFolder, "beatmaps.db"));
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
            return new Beatmap(Path.Combine(folder, $"{hashes[mapHash]}.osu"));
        }
        public Beatmap GetBeatmapFromId(string beatmap_id)
        {
            string file = Path.Combine(folder, $"{beatmap_id}.osu");
            if (!File.Exists(file))
            {
                api.DownloadBeatmapFromId(beatmap_id, folder);
                hashes[Beatmap.MD5FromFile(file)] = beatmap_id;
            }
            return new Beatmap(file);
        }
    }
}