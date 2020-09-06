using System;
using BMAPI.v1;
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
        public ServerReplayLoader(Tuple<string, string> ids, ServerReplayDb replayDb, ServerBeatmapDb beatmapDb)
         : this(replayDb.GetReplayFromOnlineId(ids.Item1), beatmapDb.GetBeatmapFromId(ids.Item2)) {}
        public ServerReplayLoader(Replay replay, ServerBeatmapDb database) : this(replay, database.GetBeatmap(replay.MapHash)) {}
    }
}