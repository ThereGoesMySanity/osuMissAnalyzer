using DSharpPlus;
using System;
using System.Threading.Tasks;

namespace OsuMissAnalyzer.Server
{
    public class Program
    {
        static DiscordClient discord;
        [STAThread]
        public static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        static async Task MainAsync(string[] args)
        {
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = "",
                TokenType = TokenType.Bot
            });
            discord.MessageCreated += async e =>
            {
                foreach (var attachment in e.Message.Attachments)
                {
                    if (attachment.FileName.EndsWith(".osr"))
                    {
                        
                    }
                }
            };

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}