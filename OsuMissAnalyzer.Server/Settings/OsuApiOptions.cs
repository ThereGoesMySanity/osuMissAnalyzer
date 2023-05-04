namespace OsuMissAnalyzer.Server.Settings
{
    public class OsuApiOptions
    {
        public string OsuId{ get; set; }  = "2558";
        public required string OsuSecret{ get; set; }
        public required string OsuApiKey { get; set; }
    }
}