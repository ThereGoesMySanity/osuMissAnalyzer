using System.Threading.Tasks;
using DSharpPlus;
using OsuMissAnalyzer.Core;

namespace OsuMissAnalyzer.Server
{
    public class SavedMiss
    {
        private readonly DiscordClient discord;
        public MissAnalyzer MissAnalyzer;
        public string[] MissUrls;
        public int? CurrentMiss;
        public SavedMiss(DiscordClient discord, MissAnalyzer analyzer)
        {
            this.discord = discord;
            MissAnalyzer = analyzer;
            MissUrls = new string[analyzer.MissCount];
            CurrentMiss = null;
        }
        public async Task<string> GetOrCreateMissMessage(ServerContext context)
        {
            if (!CurrentMiss.HasValue) return null;
            MissUrls[CurrentMiss.Value] ??= await context.SendMissMessage(discord, MissAnalyzer, CurrentMiss.Value);
            return MissUrls[CurrentMiss.Value];
        }
    }
}