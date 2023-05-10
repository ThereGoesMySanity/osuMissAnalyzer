using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OsuMissAnalyzer.Server.Logging;

namespace OsuMissAnalyzer.Server.Api
{
    public class ApiStartup
    {
        const ulong BATHBOT = 297073686916366336;

        private Dictionary<ulong, string> directBotIds = new Dictionary<ulong, string>
        {
            [BATHBOT] = "bathbot"
        };
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
        public async Task<bool> ScoreRequest(ScoreRequest req, DiscordShardedClient discord, IServiceProvider serviceProvider,
                IDataLogger dLog)
        {
            if (!discord.GetShard(req.GuildId).Guilds.ContainsKey(req.GuildId)) return false;

            var replayLoader = ActivatorUtilities.CreateInstance<ServerReplayLoader>(serviceProvider);
            replayLoader.Source = Source.BOT;
            replayLoader.ScoreId = req.ScoreId;
            await replayLoader.Load();

            var ret = replayLoader.Loaded && replayLoader.ReplayAnalyzer.misses.Count > 0;
            if (ret) dLog.Log(DataPoint.BotDirectReqTrue);
            else dLog.Log(DataPoint.BotDirectReqFalse);
            return ret;
        }
        public async Task<ulong?> ScoreResponse(ScoreResponse res, DiscordShardedClient discord, IServiceScopeFactory serviceScopeFactory,
                ILogger<ApiStartup> logger, IDataLogger dLog)
        {
            if (!discord.GetShard(res.GuildId).Guilds.ContainsKey(res.GuildId)) return null;

            await using var scope = serviceScopeFactory.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<RequestContext>();
            context.LoadFrom(res);

            var replayLoader = scope.ServiceProvider.GetRequiredService<ServerReplayLoader>();
            replayLoader.Source = Source.BOT;
            replayLoader.ScoreId = res.ScoreId;

            var message = await context.GetMessageAsync();
            logger.LogInformation("{bot} direct response", directBotIds[message.Author.Id]);

            dLog.Log(DataPoint.BotDirectResponse);
            var id = await scope.ServiceProvider.GetRequiredService<ResponseFactory>()
                    .CreateMessageResponse(message);
            return id;
        }
    }
}
