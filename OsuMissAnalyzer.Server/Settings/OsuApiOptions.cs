namespace OsuMissAnalyzer.Server.Settings
{
    public class OsuApiOptions
    {
        public string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public required string ApiKey { get; set; }
    }
}