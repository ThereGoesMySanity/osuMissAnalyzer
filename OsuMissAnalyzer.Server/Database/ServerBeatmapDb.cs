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
        string folder;
        Dictionary<string, string> hashes;
        WebClient webClient;
        string apiKey;
        public ServerBeatmapDb(string beatmapFolder)
        {
            folder = beatmapFolder;
            hashes = JsonConvert.DeserializeObject<Dictionary<string, string>>(Path.Combine(beatmapFolder, "beatmaps.db"));
            webClient = new WebClient();
            apiKey = File.ReadAllText("key.dat");
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
                var j = JArray.Parse(webClient.DownloadString($"https://osu.ppy.sh/api/get_beatmaps?k={apiKey}&h={mapHash}"));
                string beatmap_id = (string)j[0]["beatmap_id"];
                webClient.DownloadFile($"https://osu.ppy.sh/osu/{beatmap_id}", Path.Combine(folder, $"{beatmap_id}.osu"));
                hashes[mapHash] = beatmap_id;
            }
            return new Beatmap(Path.Combine(folder, $"{hashes[mapHash]}.osu"));
        }
        public Beatmap GetBeatmapFromId(string beatmap_id)
        {
            string file = Path.Combine(folder, $"{beatmap_id}.osu");
            if (!File.Exists(file))
            {
                webClient.DownloadFile($"https://osu.ppy.sh/osu/{beatmap_id}", file);
                hashes[Beatmap.MD5FromFile(file)] = beatmap_id;
            }
            return new Beatmap(file);
        }
    }
}