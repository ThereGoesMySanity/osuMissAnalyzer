using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.Server.Settings;
using SixLabors.ImageSharp;

namespace OsuMissAnalyzer.Server
{
    public class SavedMiss
    {
        private readonly DiscordClient discord;
        public MissAnalyzer MissAnalyzer;
        private readonly ServerOptions serverOptions;
        public string[] MissUrls;
        public int? CurrentMiss;
        public SavedMiss(DiscordClient discord, MissAnalyzer analyzer, ServerOptions serverOptions)
        {
            this.discord = discord;
            MissAnalyzer = analyzer;
            this.serverOptions = serverOptions;
            MissUrls = new string[analyzer.MissCount];
            CurrentMiss = null;
        }
        public async Task<string> GetOrCreateMissMessage()
        {
            if (!CurrentMiss.HasValue) return null;
            MissUrls[CurrentMiss.Value] ??= await SendMissMessage(CurrentMiss.Value);
            return MissUrls[CurrentMiss.Value];
        }
        public async Task<string> SendMissMessage(int index)
        {
            MissAnalyzer.CurrentObject = index;
            var img = await GetStream(MissAnalyzer.DrawSelectedHitObject(serverOptions.Area));
            DiscordMessageBuilder message = new DiscordMessageBuilder().AddFile("miss.png", img);
            return (await (await discord.GetChannelAsync(serverOptions.DumpChannel)).SendMessageAsync(message)).Attachments[0].Url;
        }

        private static async Task<MemoryStream> GetStream(Image bitmap)
        {
            MemoryStream s = new MemoryStream();
            await bitmap.SaveAsPngAsync(s);
            s.Seek(0, SeekOrigin.Begin);
            return s;
        }

        
    }
}