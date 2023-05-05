namespace OsuMissAnalyzer.Server.Settings
{
    public class OsuApiOptions
    {
        public string ClientId { get; set; }  = "2558";
        public required string ClientSecret { get; set; }
        public required string ApiKey { get; set; }
    }
}