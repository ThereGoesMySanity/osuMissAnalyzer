using System.IO;
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
        public Source? Source = null;
        public string ErrorMessage = null;

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

        public bool Loaded { get; internal set; }

        private ReplayAnalyzer _analyzer;

        public async Task<string> Load(OsuApi api, ServerReplayDb replays, ServerBeatmapDb beatmaps)
        {
            JToken score = null;
            if (Username != null && UserId == null)
                UserId = await api.GetUserIdv1(Username);

            if (BeatmapId != null)
                _beatmap = await beatmaps.GetBeatmapFromId(BeatmapId);

            if (ReplayFile != null)
                _replay = new Replay(ReplayFile);
            else if (ScoreId != null)
            {
                if (Mods == null || Beatmap == null)
                    score = await api.GetScorev2(ScoreId);
                else
                    _replay = await replays.GetReplayFromOnlineId(ScoreId, Mods, Beatmap);
            }

            if(_replay == null && PlayIndex.HasValue)
            {
                if (PlayIndex.Value < 0) return "Index value must be greater than 0";

                if (UserId != null && UserScores != null)
                    score = await api.GetUserScoresv2(UserId, UserScores, PlayIndex.Value, FailedScores);
                else if (BeatmapId != null)
                    score = await api.GetBeatmapScoresv2(BeatmapId, PlayIndex.Value);
            }

            if (score != null)
            {
                if (!(bool)score["replay"]) return "Replay not saved online";
                if ((bool)score["perfect"]) return "No misses";

                if (_beatmap == null) _beatmap = await beatmaps.GetBeatmapFromId((string)score["beatmap"]["id"]);
                _replay = await replays.GetReplayFromScore(score, _beatmap);

            }

            if (_beatmap == null && _replay != null)
                _beatmap = await beatmaps.GetBeatmap(_replay.MapHash);

            if (_beatmap != null && _replay != null && _beatmap.BeatmapHash != _replay.MapHash)
                _beatmap = await beatmaps.GetBeatmapFromId(_beatmap.BeatmapID.Value.ToString(), forceRedl: true);

            if (_replay != null && !_replay.fullLoaded)
                return "Replay does not contain any cursor data - can't analyze";


            if (_replay != null && _beatmap != null)
            {
                if (_beatmap.Mode != GameMode.osu) return null;

                _analyzer = new ReplayAnalyzer(_beatmap, _replay);
                Loaded = true;
                return null;
            }
            return $"Couldn't find {(_replay == null? "replay" : "beatmap")}";
        }
        public override string ToString()
        {
            return (Path.GetFileNameWithoutExtension(Replay.Filename) ?? ScoreId ?? Replay.OnlineId.ToString());
        }
    }
}