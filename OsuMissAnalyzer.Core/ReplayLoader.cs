using BMAPI.v1;
using ReplayAPI;
using osuDodgyMomentsFinder;
namespace OsuMissAnalyzer.Core
{
    public interface IReplayLoader
    {
        Replay Replay { get; }
        Beatmap Beatmap { get; }
        ReplayAnalyzer ReplayAnalyzer { get; }
    }
}