using Microsoft.Extensions.Options;

namespace OsuMissAnalyzer.Server.Settings
{
    public class DiscordOptions : IOptions<DiscordOptions>
    {
        public required string DiscordToken { get; set; }
        public string DiscordId { get; set; }
        public string DiscordPermissions { get; set; }

        public string BotLink => $"https://discordapp.com/oauth2/authorize?client_id={DiscordId}&scope=bot&permissions={DiscordPermissions}";

        public DiscordOptions Value => this;
    }
}