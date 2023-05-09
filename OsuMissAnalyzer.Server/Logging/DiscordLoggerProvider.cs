using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OsuMissAnalyzer.Server.Logging
{
    [ProviderAlias("DiscordLog")]
    public class DiscordLoggerProvider : ILoggerProvider
    {
        private DiscordLogger logger;
        private HttpClient httpClient;
        public DiscordLoggerConfiguration Config { get; set; }

        public DiscordLoggerProvider(IOptions<DiscordLoggerConfiguration> config)
        {
            Config = config.Value;
            httpClient = new HttpClient();
            logger = new DiscordLogger(httpClient, config.Value);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return logger;
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}