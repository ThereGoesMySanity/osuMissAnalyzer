using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace OsuMissAnalyzer.Server.Models
{
    public class ApiStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/api/scorerequest", ScoreRequest);
                endpoints.MapPost("/api/scoreresponse", ScoreResponse);
            });
        }
        public async Task<bool> ScoreRequest(ScoreRequest req, IServiceProvider serviceProvider)
        {
            var replayLoader = ActivatorUtilities.CreateInstance<ServerReplayLoader>(serviceProvider);
            replayLoader.Source = Source.BOT;
            replayLoader.ScoreId = req.ScoreId;
            await replayLoader.Load();

            return replayLoader.Loaded;
        }
        public async Task<ulong?> ScoreResponse(ScoreResponse res, IServiceScopeFactory serviceScopeFactory)
        {
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
