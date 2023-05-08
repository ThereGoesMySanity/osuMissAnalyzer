using Microsoft.Extensions.DependencyInjection;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using OsuMissAnalyzer.Server.Settings;
using System;
using System.Threading.Tasks;
using OsuMissAnalyzer.Core;
using Microsoft.Extensions.Logging;
using OsuMissAnalyzer.Server.Logging;
using System.Collections.Generic;

namespace OsuMissAnalyzer.Server
{
    public class ResponseFactory
    {
        private readonly IServiceProvider provider;
        private readonly ILogger<ResponseFactory> logger;
        private readonly IDataLogger dLog;
        private readonly ResponseCache cachedMisses;
        private readonly RequestContext requestContext;
        private readonly ServerReplayLoader replayLoader;

        public ResponseFactory(IServiceProvider provider, ILogger<ResponseFactory> logger, IDataLogger dLog,
                ResponseCache cachedMisses, RequestContext requestContext, ServerReplayLoader replayLoader)
        {
            this.provider = provider;
            this.logger = logger;
            this.dLog = dLog;
            this.cachedMisses = cachedMisses;
            this.requestContext = requestContext;
            this.replayLoader = replayLoader;
        }

        public async Task<ulong?> CreateMessageResponse(DiscordMessage message)
            => await CreateResponse(replayLoader.Source == Source.BOT && requestContext.GuildOptions.Compact ? 
                ActivatorUtilities.CreateInstance<CompactResponse>(provider, message):
                ActivatorUtilities.CreateInstance<MessageResponse>(provider, message));

        public async Task<ulong?> CreateInteractionResponse(InteractionContext ctx)
            => await CreateResponse(ActivatorUtilities.CreateInstance<InteractionResponse>(provider, ctx));

        private async Task<ulong?> CreateResponse(Response res)
        {
            try
            {
                replayLoader.ErrorMessage ??= await replayLoader.Load();
                if (replayLoader.Loaded)
                {
                    MissAnalyzer missAnalyzer = new MissAnalyzer(replayLoader);
                    res.Miss = ActivatorUtilities.CreateInstance<SavedMiss>(provider, missAnalyzer);
                    if (missAnalyzer.MissCount == 0)
                    {
                        replayLoader.ErrorMessage = "No misses found.";
                    }
                    else
                    {
                        var id = await res.CreateResponse();
                        if(id.HasValue)
                        {
                            await cachedMisses.GetOrCreateResponse(id.Value, res);
                            return id.Value;
                        }
                    }
                }
            }
            catch (ArgumentException ex)
            {
                replayLoader.ErrorMessage = ex.Message;
            }
            catch (Exception exc)
            {
                logger.LogInformation(exc, "Response Error");
            }

            if (replayLoader.ErrorMessage != null && (replayLoader.Source == Source.USER || replayLoader.Source == Source.ATTACHMENT))
            {
                dLog.Log(DataPoint.MessageCreated);
                dLog.Log(DataPoint.ErrorHandled);
                logger.LogInformation($"Error handled: {replayLoader.ErrorMessage}");
                await res.CreateErrorResponse(replayLoader.ErrorMessage);
            }
            return null;
        }
    }
}