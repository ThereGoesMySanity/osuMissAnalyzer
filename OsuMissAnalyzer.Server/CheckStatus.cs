using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server
{
    public class CheckStatus : IHostedService
    {
        private Timer status;
        private static TimeSpan statusRefreshRate = new TimeSpan(0, 5, 0);
        private readonly DiscordShardedClient discord;
        private readonly IHostEnvironment env;

        public CheckStatus(DiscordShardedClient discord, IHostEnvironment env)
        {
            this.discord = discord;
            this.env = env;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            status = new Timer(this.Check, null, TimeSpan.Zero, statusRefreshRate);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await status.DisposeAsync();
        }
        public async void Check(Object e)
        {
            string stat = env.IsDevelopment() ? "Down for maintenance - be back soon!"
                                        : "/help for help!";
            await discord.UpdateStatusAsync(new DiscordActivity(stat));
        }
    }
}