using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OsuMissAnalyzer.Server.Logging
{
    public class DiscordLogger : ILogger, IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly Func<DiscordLoggerConfiguration> getConfig;
        private Task logTask;

        public DiscordLogger(HttpClient httpClient, Func<DiscordLoggerConfiguration> getConfig)
        {
            this.httpClient = httpClient;
            this.getConfig = getConfig;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => default!;

        public void Dispose()
        {
            logTask.Dispose();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if(!IsEnabled(logLevel)) return;

            logTask.Wait();
            logTask.Dispose();

            var config = getConfig();
            logTask = LogToDiscord(config, formatter(state, exception));
        }

        private async Task LogToDiscord(DiscordLoggerConfiguration config, string message)
        {
            int maxLength = 1000;
            if (message.Length > maxLength)
            {
                List<string> parts = new List<string>();
                var breaks = message.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                int i = 0;
                while (i < breaks.Length)
                {
                    StringBuilder sb = new StringBuilder();
                    if (breaks[i].Length > maxLength)
                    {
                        breaks[i] = breaks[i].Substring(0, maxLength - 3) + "...";
                    }
                    while (i < breaks.Length && sb.Length + breaks[i].Length <= maxLength)
                    {
                        if (sb.Length != 0) sb.Append('\n');
                        sb.Append(breaks[i]);
                        i++;
                    }
                    if (sb.Length != 0)
                    {
                        var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("content", sb.ToString()) });
                        await httpClient.PostAsync(config.WebHook, content);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            else
            {
                var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("content", message) });
                await httpClient.PostAsync(config.WebHook, content);
            }
        }
    }
}