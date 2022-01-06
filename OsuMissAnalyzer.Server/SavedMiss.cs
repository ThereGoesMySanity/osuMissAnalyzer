using DSharpPlus.Entities;
using OsuMissAnalyzer.Core;

namespace OsuMissAnalyzer.Server
{
    public class SavedMiss
    {
        public MissAnalyzer MissAnalyzer;
        public string[] MissUrls;
        public SavedMiss(MissAnalyzer analyzer)
        {
            MissAnalyzer = analyzer;
            MissUrls = new string[analyzer.MissCount];
        } 
    }
}