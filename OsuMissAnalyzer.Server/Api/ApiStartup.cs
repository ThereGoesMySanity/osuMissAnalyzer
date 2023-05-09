using System;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OsuMissAnalyzer.Server.Api
{
    public class ApiStartup
    {
        private readonly IHostEnvironment env;

        public ApiStartup(IHostEnvironment env)
        {
            this.env = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseMiddleware<IpWhitelist>();

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/api/scorerequest", ScoreRequest);
                endpoints.MapPost("/api/scoreresponse", ScoreResponse);
            });
        }
        public async Task<bool> ScoreRequest(ScoreRequest req, DiscordShardedClient discord, IServiceProvider serviceProvider)
        {
            if (!discord.GetShard(req.GuildId).Guilds.ContainsKey(req.GuildId)) return false;

            var replayLoader = ActivatorUtilities.CreateInstance<ServerReplayLoader>(serviceProvider);
            replayLoader.Source = Source.BOT;
            replayLoader.ScoreId = req.ScoreId;
            await replayLoader.Load();

            return replayLoader.Loaded && replayLoader.ReplayAnalyzer.misses.Count > 0;
        }
        public async Task<ulong?> ScoreResponse(ScoreResponse res, DiscordShardedClient discord, IServiceScopeFactory serviceScopeFactory)
        {
            if (!discord.GetShard(res.GuildId).Guilds.ContainsKey(res.GuildId)) return null;

            await using var scope = serviceScopeFactory.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<RequestContext>();
            context.LoadFrom(res);

            var replayLoader = scope.ServiceProvider.GetRequiredService<ServerReplayLoader>();
            replayLoader.Source = Source.BOT;
            replayLoader.ScoreId = res.ScoreId;

            var id = await scope.ServiceProvider.GetRequiredService<ResponseFactory>()
                    .CreateMessageResponse(await context.GetMessageAsync());

            return id;
        }
    }
}
