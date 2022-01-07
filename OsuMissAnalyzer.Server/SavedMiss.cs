using System.Threading.Tasks;
using OsuMissAnalyzer.Core;

namespace OsuMissAnalyzer.Server
{
    public class SavedMiss
    {
        public MissAnalyzer MissAnalyzer;
        public string[] MissUrls;
        public int? CurrentMiss;
        public SavedMiss(MissAnalyzer analyzer)
        {
            MissAnalyzer = analyzer;
            MissUrls = new string[analyzer.MissCount];
            CurrentMiss = null;
        }
        public async Task<string> GetOrCreateMissMessage(ServerContext context)
        {
            if (!CurrentMiss.HasValue) return null;
            MissUrls[CurrentMiss.Value] ??= await context.SendMissMessage(MissAnalyzer, CurrentMiss.Value);
            return MissUrls[CurrentMiss.Value];
        }
    }
}