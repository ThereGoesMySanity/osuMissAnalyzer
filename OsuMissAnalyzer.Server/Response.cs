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
        public Response(ServerContext context, GuildSettings guildSettings)
        {
            Context = context;
            GuildSettings = guildSettings;
        }

        public SavedMiss Miss { get; set; }
        public ServerContext Context { get; }
        public GuildSettings GuildSettings { get; }

        public abstract Task CreateErrorResponse(string errorMessage);
        public abstract Task<ulong?> CreateResponse(string content, int misses);
        public abstract Task UpdateResponse(string content, int index);

        protected IEnumerable<DiscordComponent> GetMissComponents(int number) => 
                            GetMissRows(Math.Min(Math.Min(GuildSettings.MaxButtons, number), 25));

        private IEnumerable<DiscordComponent> GetMissRows(int number) =>
                            Enumerable.Range(1, number / 5).Select(i => GetMissRow(i, number));
        protected DiscordComponent GetMissRow(int rowIndex, int totalCount) => 
                    new DiscordActionRowComponent(Enumerable.Range(5 * rowIndex + 1, totalCount - 5 * rowIndex)
                            .Select(i => new DiscordButtonComponent(ButtonStyle.Primary, i.ToString(), i.ToString())));
    }
    public class InteractionResponse : Response
    {
        private readonly InteractionContext ctx;

        //ctx must have deferred response already sent
        public InteractionResponse(ServerContext context, GuildSettings settings, InteractionContext ctx) : base(context, settings)
        {
            this.ctx = ctx;
        }

        public override async Task CreateErrorResponse(string errorMessage)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(errorMessage));
        }

        public override async Task<ulong?> CreateResponse(string content, int misses)
        {
            var builder = new DiscordWebhookBuilder().WithContent(content);
            if (misses > 1)
            {
                builder.AddComponents(GetMissComponents(misses));
            }
            await ctx.EditResponseAsync(builder);
            return ctx.InteractionId;
        }

        public override async Task UpdateResponse(string content, int index)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(content));
        }
    }
    public class MessageResponse : Response
    {
        protected DiscordMessage source;
        protected DiscordMessage response;
        public MessageResponse(ServerContext context, GuildSettings settings, MessageCreateEventArgs e) : base(context, settings)
        {
            source = e.Message;
            response = null;
        }

        public override async Task CreateErrorResponse(string errorMessage)
        {
            await source.RespondAsync(errorMessage);
        }

        public override async Task<ulong?> CreateResponse(string content, int misses)
        {
            var builder = new DiscordMessageBuilder().WithContent(content);
            if (misses > 1)
            {
                builder.AddComponents(GetMissComponents(misses));
            }
            await source.RespondAsync(builder);
            return response?.Id;
        }
        public override async Task UpdateResponse(string content, int index)
        {
            await response.ModifyAsync(content);
        }
    }
    public class CompactResponse : MessageResponse
    {
        public CompactResponse(ServerContext context, GuildSettings settings, MessageCreateEventArgs e) : base(context, settings, e) {}
        public override async Task<ulong?> CreateResponse(string content, int misses)
        {
            await SendReactions(source, misses);
            return source.Id;
        }
        public override async Task UpdateResponse(string content, int index)
        {
            if (response == null)
            {
                await base.CreateResponse(await Context.GetOrCreateMissMessage(Miss, index), Miss.MissAnalyzer.MissCount);
            }
            else
            {
                await base.UpdateResponse(content, index);
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