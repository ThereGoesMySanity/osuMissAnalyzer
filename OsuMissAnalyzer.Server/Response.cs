using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server
{
    public abstract class Response
    {
        public Response(ServerContext context)
        {
            Context = context;
        }

        public SavedMiss Miss { get; set; }
        public ServerContext Context { get; }

        public abstract Task CreateErrorResponse(string errorMessage);
        public abstract Task<ulong?> CreateResponse(GuildSettings settings, string content, int misses);
        public abstract Task UpdateResponse(GuildSettings settings, string content, int index);

        protected IEnumerable<DiscordComponent> GetMissComponents(int number) => Enumerable.Range(1, Math.Max(number, 25))
                            .Select(i => new DiscordButtonComponent(ButtonStyle.Primary, i.ToString(), i.ToString()));
    }
    public class InteractionResponse : Response
    {
        private readonly InteractionContext ctx;

        //ctx must have deferred response already sent
        public InteractionResponse(ServerContext context, InteractionContext ctx) : base(context)
        {
            this.ctx = ctx;
        }

        public override async Task CreateErrorResponse(string errorMessage)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(errorMessage));
        }

        public override async Task<ulong?> CreateResponse(GuildSettings settings, string content, int misses)
        {
            var builder = new DiscordWebhookBuilder().WithContent(content);
            if (misses > 1)
            {
                builder.AddComponents(GetMissComponents(Math.Max(settings.MaxButtons, misses)));
            }
            await ctx.EditResponseAsync(builder);
            return ctx.InteractionId;
        }

        public override async Task UpdateResponse(GuildSettings settings, string content, int index)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(content));
        }
    }
    public class MessageResponse : Response
    {
        protected DiscordMessage source;
        protected DiscordMessage response;
        public MessageResponse(ServerContext context, MessageCreateEventArgs e) : base(context)
        {
            source = e.Message;
            response = null;
        }

        public override async Task CreateErrorResponse(string errorMessage)
        {
            await source.RespondAsync(errorMessage);
        }

        public override async Task<ulong?> CreateResponse(GuildSettings guildSettings, string content, int misses)
        {
            var builder = new DiscordMessageBuilder().WithContent(content);
            if (misses > 1)
            {
                builder.AddComponents(GetMissComponents(Math.Max(guildSettings.MaxButtons, misses)));
            }
            await source.RespondAsync(builder);
            return response?.Id;
        }
        public override async Task UpdateResponse(GuildSettings settings, string content, int index)
        {
            await response.ModifyAsync(content);
        }
    }
    public class CompactResponse : MessageResponse
    {
        public CompactResponse(ServerContext context, MessageCreateEventArgs e) : base(context, e) {}
        public override async Task<ulong?> CreateResponse(GuildSettings guildSettings, string content, int misses)
        {
            await SendReactions(source, misses);
            return source.Id;
        }
        public override async Task UpdateResponse(GuildSettings guildSettings, string content, int index)
        {
            if (response == null)
            {
                await base.CreateResponse(guildSettings, await Context.GetOrCreateMissMessage(Miss, index), Miss.MissAnalyzer.MissCount);
            }
            else
            {
                await base.UpdateResponse(guildSettings, content, index);
            }
        }
        public override async Task CreateErrorResponse(string errorMessage)
        {
            //no-op
            await Task.CompletedTask;
        }
        private async Task SendReactions(DiscordMessage message, int missCount)
        {
            for (int i = 1; i < Math.Min(missCount + 1, ServerContext.numberEmojis.Length); i++)
            {
                await message.CreateReactionAsync(ServerContext.numberEmojis[i]);
            }
        }
    }
}