using System;
using System.ComponentModel;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OsuMissAnalyzer.Server.Logging
{
    [ProviderAlias("Discord")]
    public class DiscordLoggerProvider : ILoggerProvider
    {
        private DiscordLogger logger;
        public DiscordLoggerConfiguration Config { get; set; }

        private IDisposable onChange;

        public DiscordLoggerProvider(HttpClient httpClient, IOptionsMonitor<DiscordLoggerConfiguration> config)
        {
            Config = config.CurrentValue;
            onChange = config.OnChange(upd => Config = upd);
            logger = new DiscordLogger(httpClient, () => Config);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return logger;
        }

        public void Dispose()
        {
            onChange.Dispose();
        }
    }
}