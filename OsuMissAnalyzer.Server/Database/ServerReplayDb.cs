using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            ["HD"] = (int)Mods.Hidden,
            ["HR"] = (int)Mods.HardRock,
            ["SD"] = (int)Mods.SuddenDeath,
            ["DT"] = (int)Mods.DoubleTime,
            ["HT"] = (int)Mods.HalfTime,
            ["NC"] = (int)Mods.NightCore,
            ["FL"] = (int)Mods.FlashLight,
            ["PF"] = (int)Mods.Perfect,
        };
        public Replay GetReplayFromOnlineId(JToken replay, Beatmap beatmap)
        {
            string file = Path.Combine(serverFolder, "replays", $"{(string)replay["best_id"]}.osr");
            if (!File.Exists(file))
            {
                Console.WriteLine("replay not found, downloading...");
                Replay r = new Replay();
                r.GameMode = (GameModes)((int)replay["mode_int"]);
                r.MapHash = beatmap.BeatmapHash;
                r.PlayerName = (string)replay["user"]["username"];
                // r.ReplayHash = 
                r.Count300 = (ushort)replay["statistics"]["count_300"];
                r.Count100 = (ushort)replay["statistics"]["count_100"];
                r.Count50 = (ushort)replay["statistics"]["count_50"];
                r.CountGeki = (ushort)replay["statistics"]["count_geki"];
                r.CountKatu = (ushort)replay["statistics"]["count_katu"];
                r.CountMiss = (ushort)replay["statistics"]["count_miss"];
                r.TotalScore = (uint)replay["score"];
                r.MaxCombo = (ushort)replay["max_combo"];
                r.IsPerfect = (bool)replay["perfect"];
                r.Mods = (Mods)replay["mods"].Select(m => modValues[(string)m]).Sum();
                r.headerLoaded = true;
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    byte[] data = api.DownloadReplayFromId((string)replay["best_id"]);
                    bw.WriteNullableString(string.Empty);
                    bw.Write(DateTime.Parse((string)replay["created_at"]).ToUniversalTime().Ticks);
                    bw.Write(data.Length);
                    bw.Write(data);
                    bw.Write((ulong)replay["best_id"]);
                    ms.Seek(0, SeekOrigin.Begin);
                    using (BinaryReader reader = new BinaryReader(ms))
                    {
                        r.replayReader = reader;
                        r.Load();
                    }
                }
                r.Save(file);
            }
            return new Replay(file);
        }
    }
}