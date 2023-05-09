using Microsoft.Extensions.Options;

namespace OsuMissAnalyzer.Server.Settings
{
    public class OsuApiOptions : IOptions<OsuApiOptions>
    {
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public required string ApiKey { get; set; }

        public OsuApiOptions Value => this;
    }
}