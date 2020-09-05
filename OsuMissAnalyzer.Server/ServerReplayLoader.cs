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
        public ServerReplayLoader(string replayFile, string beatmapId, ServerBeatmapDb database)
        {
            Replay = new Replay(replayFile, true, false);
            Beatmap = database.GetBeatmapFromId(beatmapId);
            ReplayAnalyzer = new ReplayAnalyzer(Beatmap, Replay);
        }
        public ServerReplayLoader(string replayFile, ServerBeatmapDb database)
        {
            Replay = new Replay(replayFile, true, false);
            Beatmap = database.GetBeatmap(Replay.MapHash);
            ReplayAnalyzer = new ReplayAnalyzer(Beatmap, Replay);
        }
    }
}