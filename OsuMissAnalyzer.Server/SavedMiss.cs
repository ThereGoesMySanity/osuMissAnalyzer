using OsuMissAnalyzer.Core;

namespace OsuMissAnalyzer.Server
{
    public struct SavedMiss
    {
        public MissAnalyzer MissAnalyzer;
        public bool[] MissesDisplayed;
        public SavedMiss(MissAnalyzer analyzer) : this(analyzer, new bool[analyzer.MissCount]) {}

        public SavedMiss(MissAnalyzer analyzer, bool[] missesDisplayed)
        {
            MissAnalyzer = analyzer;
            MissesDisplayed = missesDisplayed;
        } 
    }
}