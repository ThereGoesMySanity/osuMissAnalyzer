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
        private readonly ILogger logger;
        private readonly IDataLogger dLog;
        private readonly ServerContext serverContext;
        private readonly RequestContext requestContext;
        private readonly ServerReplayLoader replayLoader;
        private readonly GuildOptions options;

        public ResponseFactory(IServiceProvider provider, ILogger logger, IDataLogger dLog,
                ServerContext serverContext, RequestContext requestContext, ServerReplayLoader replayLoader, GuildOptions options)
        {
            this.provider = provider;
            this.logger = logger;
            this.dLog = dLog;
            this.serverContext = serverContext;
            this.requestContext = requestContext;
            this.replayLoader = replayLoader;
            this.options = options;
        }

        public async Task<KeyValuePair<ulong, MessageResponse>?> CreateMessageResponse(DiscordMessage message)
            => await CreateResponse(replayLoader.Source == Source.BOT && options.Compact ? 
                ActivatorUtilities.CreateInstance<CompactResponse>(provider, message):
                ActivatorUtilities.CreateInstance<MessageResponse>(provider, message));

        public async Task<KeyValuePair<ulong, InteractionResponse>?> CreateInteractionResponse(InteractionContext ctx)
            => await CreateResponse(ActivatorUtilities.CreateInstance<InteractionResponse>(provider, ctx));

        private async Task<KeyValuePair<ulong, T>?> CreateResponse<T>(T res) where T : Response
        {
            KeyValuePair<ulong, T>? response = null;
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
                        if(id.HasValue) response = new(id.Value, res);
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

            if (response.HasValue) serverContext.UpdateCache(response.Value.Key, response.Value.Value);
            return response;
        }
    }
}