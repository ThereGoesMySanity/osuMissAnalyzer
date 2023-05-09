namespace OsuMissAnalyzer.Server.Api
{
    public class ScoreResponse
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public ulong ScoreId { get; set; }
    }
}