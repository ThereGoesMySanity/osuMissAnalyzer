using System;
using BMAPI.v1;
using Newtonsoft.Json.Linq;
using osuDodgyMomentsFinder;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.Server.Database;
using ReplayAPI;

namespace OsuMissAnalyzer.Server
{
    public class ServerReplayLoader : ReplayLoader
    {
        public Replay Replay { get; private set; }
        public Beatmap Beatmap { get; private set; }
        public ReplayAnalyzer ReplayAnalyzer { get; private set; }
        public ServerReplayLoader(Replay replay, Beatmap beatmap)
        {
            Replay = replay;
            Beatmap = beatmap;
            ReplayAnalyzer = new ReplayAnalyzer(Beatmap, Replay);
        }
        public ServerReplayLoader(JToken score, ServerReplayDb replayDb, ServerBeatmapDb beatmapDb)
         : this(score, beatmapDb.GetBeatmapFromId((string)score["beatmap"]["id"]), replayDb) {}
        
        public ServerReplayLoader(JToken score, Beatmap beatmap, ServerReplayDb replayDb)
         : this(replayDb.GetReplayFromOnlineId(score, beatmap), beatmap) {}
        public ServerReplayLoader(Replay replay, ServerBeatmapDb database) : this(replay, database.GetBeatmap(replay.MapHash)) {}
    }
}