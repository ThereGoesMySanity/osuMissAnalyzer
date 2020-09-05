using System.Collections.Generic;
using System.IO;
using System.Net;
using BMAPI.v1;
using Newtonsoft.Json;
using OsuMissAnalyzer.Core;
namespace OsuMissAnalyzer.Server.Database
{
    public class ServerBeatmapDb
    {
        string folder;
        Dictionary<string, string> hashes;
        public ServerBeatmapDb(string beatmapFolder)
        {
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
                new FileWebRequest(Path.Combine(folder, "temp.osu"))
            }
        }
        public void AddBeatmap(File beatmap)
        {

        }
    }
}