using System.Threading.Tasks;
using BMAPI.v1;
using Newtonsoft.Json.Linq;
using osuDodgyMomentsFinder;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.Server.Database;
using ReplayAPI;

namespace OsuMissAnalyzer.Server
{
    public class ServerReplayLoader : IReplayLoader
    {
        public string UserId;
        public string Username;
        public string UserScores;
        public string BeatmapId;
        public string ScoreId;
        public string Mods;
        public string ReplayFile;
        public bool FailedScores = false;

        public int? PlayIndex;

        public Replay Replay => _replay;
        private Replay _replay;

        public Beatmap Beatmap => _beatmap;
        private Beatmap _beatmap;

        public ReplayAnalyzer ReplayAnalyzer => _analyzer;
        private ReplayAnalyzer _analyzer;

        public async Task<bool> Load(OsuApi api, ServerReplayDb replays, ServerBeatmapDb beatmaps)
        {
            JToken score = null;
            if (Username != null && UserId == null)
                UserId = await api.GetUserIdv1(Username);

            if (BeatmapId != null)
                _beatmap = await beatmaps.GetBeatmapFromId(BeatmapId);

            if (ReplayFile != null)
                _replay = new Replay(ReplayFile);
            else if (ScoreId != null && Mods != null)
                _replay = await replays.GetReplayFromOnlineId(ScoreId, Mods, _beatmap);

            if(_replay == null && PlayIndex.HasValue)
            {
                if (UserId != null && UserScores != null)
                    score = await api.GetUserScoresv2(UserId, UserScores, PlayIndex.Value, FailedScores);
                else if (BeatmapId != null)
                    score = await api.GetBeatmapScoresv2(BeatmapId, PlayIndex.Value);

                if (score != null && _beatmap == null)
                    _beatmap = await beatmaps.GetBeatmapFromId((string)score["beatmap"]["id"]);
            }

            if (score != null && _beatmap != null)
                _replay = await replays.GetReplayFromScore(score, _beatmap);

            if (_beatmap == null && _replay != null)
                _beatmap = await beatmaps.GetBeatmap(_replay.MapHash);

            if (_replay != null && _beatmap != null)
            {
                _analyzer = new ReplayAnalyzer(_beatmap, _replay);
                return true;
            }
            return false;
        }
    }
}