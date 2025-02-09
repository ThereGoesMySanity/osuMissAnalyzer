using System.IO;
using System.Threading.Tasks;
using BMAPI.v1;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using osuDodgyMomentsFinder;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.Server.Database;
using OsuMissAnalyzer.Server.OsuApi;
using ReplayAPI;

namespace OsuMissAnalyzer.Server
{
    public class ServerReplayLoader : IReplayLoader
    {
        public Source? Source = null;
        public string? ErrorMessage = null;

        public ulong? UserId;
        public string? Username;
        public string? UserScores;
        public ulong? BeatmapId;
        public ulong? LegacyId;
        public ulong? ScoreId;
        public string? Mods;
        public string? ReplayFile;
        public bool FailedScores = false;

        public int? PlayIndex;

        public Replay? Replay => _replay;
        private Replay? _replay;

        public Beatmap? Beatmap => _beatmap;
        private Beatmap? _beatmap;

        public ReplayAnalyzer? ReplayAnalyzer => _analyzer;

        public bool Loaded { get; internal set; }

        private ReplayAnalyzer? _analyzer;

        private readonly OsuApiv2 api;
        private readonly ServerReplayDb replays;
        private readonly ServerBeatmapDb beatmaps;

        public ColorScheme ColorScheme { get; set; } = ColorScheme.Default;

        public ServerReplayLoader(RequestContext context, OsuApiv2 api, ServerReplayDb replays, ServerBeatmapDb beatmaps)
            : this(api, replays, beatmaps)
        {
            this.ColorScheme = ColorScheme.Parse(context.GuildOptions.ColorScheme) ?? ColorScheme.Default;
        }

        [ActivatorUtilitiesConstructor]
        public ServerReplayLoader(OsuApiv2 api, ServerReplayDb replays, ServerBeatmapDb beatmaps)
        {
            this.api = api;
            this.replays = replays;
            this.beatmaps = beatmaps;
        }
        public async Task<string?> Load()
        {
            if (Loaded) return null;

            OsuApi.Score? score = null;
            if (Username != null && UserId == null)
                UserId = (await api.GetUser(Username))?.Id;

            if (BeatmapId.HasValue)
                _beatmap = await beatmaps.GetBeatmapFromId(BeatmapId.Value);

            if (ReplayFile != null)
                _replay = new Replay(ReplayFile);
            else if (ScoreId.HasValue || LegacyId.HasValue)
            {
                _replay = await replays.GetReplay(ScoreId, LegacyId);
            }

            if(_replay == null && PlayIndex.HasValue)
            {
                if (PlayIndex.Value < 0) return "Index value must be greater than 0";

                if (UserId.HasValue && UserScores != null)
                    score = await api.GetUserScoresv2(UserId.Value, UserScores, PlayIndex.Value, FailedScores);
                else if (BeatmapId.HasValue)
                    score = await api.GetBeatmapScoresv2(BeatmapId.Value, PlayIndex.Value);
            }

            if (score is not null)
            {
                if (!score.HasReplay) return "Replay not saved online";
                if (score.IsPerfectCombo) return "No misses";

                _beatmap ??= await beatmaps.GetBeatmapFromId(score.BeatmapId);
                _replay = await replays.GetReplay(score.Id, score.LegacyScoreId);

            }

            if (_beatmap == null && _replay != null)
                _beatmap = await beatmaps.GetBeatmap(_replay.MapHash);

            if (_beatmap != null && _replay != null 
                    && _beatmap.BeatmapHash != _replay.MapHash
                    && _beatmap.BeatmapID.HasValue)
                _beatmap = await beatmaps.GetBeatmapFromId((ulong)_beatmap.BeatmapID.Value, forceRedl: true);

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
            return Path.GetFileNameWithoutExtension(Replay?.Filename) ?? ScoreId?.ToString() ?? Replay?.OnlineId.ToString() ?? "unknown score";
        }
    }
}