namespace OsuMissAnalyzer.Server.Models
{
    public class ScoreRequest
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string ScoreId { get; set; }
    }
}