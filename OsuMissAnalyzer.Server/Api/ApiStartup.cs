using System;
using System.Runtime.Caching.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server.Models
{
    public class ApiStartup : IStartup
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public ServerContext ServerContext { get; set; }
        public MemoryCache<string, ServerReplayLoader> CachedRequests { get; private set; }
        public ApiStartup(ServerContext context, IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory)
        {
            ServerContext = context;
            this.serviceProvider = serviceProvider;
            this.serviceScopeFactory = serviceScopeFactory;
            CachedRequests = new MemoryCache<string, ServerReplayLoader>(128);
        }
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildServiceProvider();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/api/scorerequest", ScoreRequest);
                endpoints.MapPost("/api/scoreresponse", ScoreResponse);
            });
        }
        public async Task<bool> ScoreRequest(ScoreRequest req)
        {
            var replayLoader = ActivatorUtilities.CreateInstance<ServerReplayLoader>(serviceProvider);
            replayLoader.Source = Source.BOT;
            replayLoader.ScoreId = req.ScoreId;
            await replayLoader.Load();

            if (replayLoader.Loaded) CachedRequests.Add(req.ScoreId, replayLoader);

            return replayLoader.Loaded;
        }
        public async Task<ulong?> ScoreResponse(ScoreResponse res)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<RequestContext>();
            context.LoadFrom(res);

            var replayLoader = scope.ServiceProvider.GetRequiredService<ServerReplayLoader>();
            replayLoader.Source = Source.BOT;
            replayLoader.ScoreId = res.ScoreId;

            var response = await scope.ServiceProvider.GetRequiredService<ResponseFactory>()
                    .CreateMessageResponse(await context.GetMessageAsync());

            return response?.Key;
        }

    }
}
