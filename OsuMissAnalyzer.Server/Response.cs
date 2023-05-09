using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server
{
    public abstract class Response
    {
        public Response(GuildOptions guildSettings)
        {
            GuildSettings = guildSettings;
        }

        public SavedMiss Miss { get; set; }
        public GuildOptions GuildSettings { get; }

        public int MissCount => Miss.MissAnalyzer.MissCount;


        public abstract Task CreateErrorResponse(string errorMessage);
        public abstract Task<ulong?> CreateResponse();
        public abstract Task<ulong?> UpdateResponse(object e, int index);
        public abstract Task OnExpired();

        protected async Task<T> BuildMessage<T>(bool disabled = false) where T : BaseDiscordMessageBuilder<T>
        {
            var builder = Activator.CreateInstance<T>().WithContent(await GetContent());
            if (MissCount > 1)
            {
                foreach(var row in GetMissComponents())
                {
                    builder.AddComponents(row);
                }
                if (disabled) foreach (var r in builder.Components) foreach (var b in r.Components) (b as DiscordButtonComponent).Disable();
            }
            return builder;
        }

        public async Task<string> GetContent()
        {
            if (MissCount == 1) Miss.CurrentMiss = 0;
            if (Miss.CurrentMiss.HasValue) return await Miss.GetOrCreateMissMessage();
            else return $"Found **{MissCount}** misses";
        }

        protected IEnumerable<IEnumerable<DiscordComponent>> GetMissComponents() => 
                            GetMissRows(Math.Min(Math.Min(GuildSettings.MaxButtons, MissCount), 25));

        private IEnumerable<IEnumerable<DiscordComponent>> GetMissRows(int number) =>
                            Enumerable.Range(0, (int)Math.Ceiling(number / 5f)).Select(i => GetMissRow(i, number));
        private IEnumerable<DiscordComponent> GetMissRow(int rowIndex, int totalCount) =>
                    Enumerable.Range(5 * rowIndex + 1, Math.Min(5, totalCount - 5 * rowIndex))
                            .Select(i => new DiscordButtonComponent(ButtonStyle.Primary, i.ToString(), i.ToString(), Miss.CurrentMiss == i - 1));
    }
    public class InteractionResponse : Response
    {
        private readonly InteractionContext ctx;

        //ctx must have deferred response already sent
        public InteractionResponse(RequestContext request, InteractionContext ctx) : base(request.GuildOptions)
        {
            this.ctx = ctx;
        }

        public override async Task CreateErrorResponse(string errorMessage)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(errorMessage));
        }

        public override async Task<ulong?> CreateResponse()
        {
            var builder = await BuildMessage<DiscordWebhookBuilder>();
            await ctx.EditResponseAsync(builder);
            return ctx.InteractionId;
        }

        public override async Task OnExpired()
        {
            await ctx.EditResponseAsync(await BuildMessage<DiscordWebhookBuilder>(disabled: true));
        }

        public override async Task<ulong?> UpdateResponse(object e, int index)
        {
            Miss.CurrentMiss = index;
            ComponentInteractionCreateEventArgs args = (ComponentInteractionCreateEventArgs)e;
            await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    await BuildMessage<DiscordInteractionResponseBuilder>());
            return null;
        }
    }
    public class MessageResponse : Response
    {
        protected DiscordMessage source;
        protected DiscordMessage response;
        public MessageResponse(RequestContext request, DiscordMessage message) : base(request.GuildOptions)
        {
            source = message;
            response = null;
        }

        public override async Task CreateErrorResponse(string errorMessage)
        {
            await source.RespondAsync(errorMessage);
        }

        public override async Task<ulong?> CreateResponse()
        {
            response = await source.RespondAsync(await BuildMessage<DiscordMessageBuilder>());
            return response?.Id;
        }

        public override async Task OnExpired()
        {
            await response.ModifyAsync(await BuildMessage<DiscordMessageBuilder>(disabled: true));
        }

        public override async Task<ulong?> UpdateResponse(object e, int index)
        {
            Miss.CurrentMiss = index;
            ComponentInteractionCreateEventArgs args = (ComponentInteractionCreateEventArgs)e;
            await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    await BuildMessage<DiscordInteractionResponseBuilder>());
            return null;
        }
    }
    public class CompactResponse : MessageResponse
    {
        public CompactResponse(RequestContext request, DiscordMessage message) : base(request, message) {}
        public override async Task<ulong?> CreateResponse()
        {
            await SendReactions(source, MissCount);
            return source.Id;
        }
        public override async Task<ulong?> UpdateResponse(object e, int index)
        {
            Miss.CurrentMiss = index;
            if (response == null)
            {
                return await base.CreateResponse();
            }
            else
            {
                return await base.UpdateResponse(e, index);
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

        public override async Task OnExpired()
        {
            for (int i = 1; i < Math.Min(MissCount + 1, ServerContext.numberEmojis.Length); i++)
            {
                await source.DeleteOwnReactionAsync(ServerContext.numberEmojis[i]);
            }
            if (response != null) await base.OnExpired();
        }
    }
}