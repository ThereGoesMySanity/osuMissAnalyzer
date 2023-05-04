namespace OsuMissAnalyzer.Server.Models
{
    public class ScoreResponse
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public string ScoreId { get; set; }
    }
}