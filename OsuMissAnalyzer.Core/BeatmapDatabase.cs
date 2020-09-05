namespace OsuMissAnalyzer.Core
{
    public interface BeatmapDatabase
    {
        Beatmap GetBeatmap(string mapHash);
    }
}