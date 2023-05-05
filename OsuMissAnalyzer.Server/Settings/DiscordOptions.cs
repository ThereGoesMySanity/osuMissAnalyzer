namespace OsuMissAnalyzer.Server.Settings
{
    public class DiscordOptions
    {
        public required string DiscordToken { get; set; }
        public string DiscordId { get; set; } = "752035690237394944";
        public string DiscordPermissions { get; set; } = "117824";
        public string BotLink => $"https://discordapp.com/oauth2/authorize?client_id={DiscordId}&scope=bot&permissions={DiscordPermissions}";
    }
}