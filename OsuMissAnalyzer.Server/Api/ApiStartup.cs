using System;
using System.Runtime.Caching.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace OsuMissAnalyzer.Server.Models
{
    public class ApiStartup
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
        public void ConfigureServices(IServiceCollection services)
        {
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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
