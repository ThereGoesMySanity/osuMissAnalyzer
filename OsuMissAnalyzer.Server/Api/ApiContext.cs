using System.Runtime.Caching.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.AspNetCore.Builder;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server.Models
{
    public class ApiContext
    {
        private readonly DiscordShardedClient discord;
        private readonly GuildManager guildManager;

        public ServerContext ServerContext { get; set; }
        public MemoryCache<ulong, ServerReplayLoader> CachedRequests { get; private set; }
        public ApiContext(ServerContext context, DiscordShardedClient discord, GuildManager guildManager)
        {
            ServerContext = context;
            this.discord = discord;
            this.guildManager = guildManager;
            CachedRequests = new MemoryCache<ulong, ServerReplayLoader>(128);

            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();
            app.MapPost("/api/scorerequest", ScoreRequest);
            app.MapPost("/api/scoreresponse", ScoreResponse);

        }
        public bool ScoreRequest(ScoreRequest req)
        {
            ServerReplayLoader replayLoader = new ServerReplayLoader
            {
                Source = Source.BOT,
                ScoreId = req.ScoreId
            };
            replayLoader.Load(GuildSettings.Default, context);

            return false;
        }
        public async Task<ulong?> ScoreResponse(ScoreResponse res)
        {
            ServerReplayLoader replayLoader = new ServerReplayLoader
            {
                Source = Source.BOT,
                ScoreId = res.ScoreId
            };

            DiscordClient client = discord.GetShard(res.GuildId);
            var guildSettings = guildManager.GetGuild(res.GuildId);
            var channel = await client.GetChannelAsync(res.ChannelId);
            var message = await channel.GetMessageAsync(res.MessageId);
            Response response = MessageResponse.CreateMessageResponse(replayLoader.Source.Value, ServerContext, guildSettings, message);

            return await ServerContext.CreateResponse(client, response, replayLoader);
        }
    }
}
