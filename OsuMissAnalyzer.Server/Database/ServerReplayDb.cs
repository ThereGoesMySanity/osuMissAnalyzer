using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BMAPI.v1;
using Newtonsoft.Json.Linq;
using ReplayAPI;

namespace OsuMissAnalyzer.Server.Database
{
    public class ServerReplayDb
    {
        private readonly OsuApi api;
        string serverFolder;
        public ServerReplayDb(OsuApi api, string serverFolder)
        {
            this.api = api;
            this.serverFolder = serverFolder;
        }
        private static Dictionary<string, int> modValues = new Dictionary<string, int>
        {
            ["NF"] = (int)Mods.NoFail,
            ["EZ"] = (int)Mods.Easy,
            ["TD"] = (int)Mods.TouchDevice,
            ["HD"] = (int)Mods.Hidden,
            ["HR"] = (int)Mods.HardRock,
            ["SD"] = (int)Mods.SuddenDeath,
            ["DT"] = (int)Mods.DoubleTime,
            ["HT"] = (int)Mods.HalfTime,
            ["NC"] = (int)Mods.NightCore,
            ["FL"] = (int)Mods.FlashLight,
            ["PF"] = (int)Mods.Perfect,
        };
        public async Task<Replay> GetReplayFromOnlineId(JToken score, Beatmap beatmap)
        {
            string file = Path.Combine(serverFolder, "replays", $"{(string)score["best_id"]}.osr");
            if (!File.Exists(file))
            {
                Console.WriteLine("replay not found, downloading...");
                Replay r = new Replay();
                r.GameMode = (GameModes)((int)score["mode_int"]);
                r.MapHash = beatmap.BeatmapHash;
                r.PlayerName = (string)score["user"]["username"];
                // r.ReplayHash = 
                r.Count300 = (ushort)score["statistics"]["count_300"];
                r.Count100 = (ushort)score["statistics"]["count_100"];
                r.Count50 = (ushort)score["statistics"]["count_50"];
                r.CountGeki = (ushort)score["statistics"]["count_geki"];
                r.CountKatu = (ushort)score["statistics"]["count_katu"];
                r.CountMiss = (ushort)score["statistics"]["count_miss"];
                r.TotalScore = (uint)score["score"];
                r.MaxCombo = (ushort)score["max_combo"];
                r.IsPerfect = (bool)score["perfect"];
                r.Mods = (Mods)score["mods"].Select(m => modValues[(string)m]).Sum();
                r.headerLoaded = true;
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    byte[] data = await api.DownloadReplayFromId((string)score["best_id"]);
                    bw.WriteNullableString(string.Empty);
                    bw.Write(DateTime.Parse((string)score["created_at"]).ToUniversalTime().Ticks);
                    bw.Write(data.Length);
                    bw.Write(data);
                    bw.Write((ulong)score["best_id"]);
                    ms.Seek(0, SeekOrigin.Begin);
                    using (BinaryReader reader = new BinaryReader(ms))
                    {
                        r.replayReader = reader;
                        r.Load();
                    }
                }
                r.Save(file);
                Logger.Log(Logging.ReplaysCacheMiss);
            }
            else
            {
                Logger.Log(Logging.ReplaysCacheHit);
            }
            return new Replay(file);
        }
    }
}