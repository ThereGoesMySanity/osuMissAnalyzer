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
        public Response(ServerContext context, GuildSettings guildSettings)
        {
            Context = context;
            GuildSettings = guildSettings;
        }

        public SavedMiss Miss { get; set; }
        public ServerContext Context { get; }
        public GuildSettings GuildSettings { get; }

        public int MissCount => Miss.MissAnalyzer.MissCount;

        public async Task<string> GetContent()
        {
            if (MissCount == 1) Miss.CurrentMiss = 0;
            if (Miss.CurrentMiss.HasValue) return await Miss.GetOrCreateMissMessage(Context);
            else return $"Found **{MissCount}** misses";
        }

        public abstract Task CreateErrorResponse(string errorMessage);
        public abstract Task<ulong?> CreateResponse();
        public abstract Task UpdateResponse(object e, int index);

        protected IEnumerable<IEnumerable<DiscordComponent>> GetMissComponents() => 
                            GetMissRows(Math.Min(Math.Min(GuildSettings.MaxButtons, MissCount), 25));

        private IEnumerable<IEnumerable<DiscordComponent>> GetMissRows(int number) =>
                            Enumerable.Range(0, number / 5 + 1).Select(i => GetMissRow(i, number));
        private IEnumerable<DiscordComponent> GetMissRow(int rowIndex, int totalCount) =>
                    Enumerable.Range(5 * rowIndex + 1, Math.Min(5, totalCount - 5 * rowIndex))
                            .Select(i => new DiscordButtonComponent(ButtonStyle.Primary, i.ToString(), i.ToString()));
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

        public override async Task<ulong?> CreateResponse()
        {
            var builder = new DiscordWebhookBuilder().WithContent(await GetContent());
            if (MissCount > 1)
            {
                foreach(var row in GetMissComponents()) builder.AddComponents(row);
            }
            await ctx.EditResponseAsync(builder);
            return ctx.InteractionId;
        }

        public override async Task UpdateResponse(object e, int index)
        {
            Miss.CurrentMiss = index;
            await CreateResponse();
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
        private DiscordMessageBuilder BuildMessage(string content)
        {
            var builder = new DiscordMessageBuilder().WithContent(content);
            if (MissCount > 1)
            {
                foreach(var row in GetMissComponents()) builder.AddComponents(row);
            }
            return builder;
        }

        public override async Task<ulong?> CreateResponse()
        {
            response = await source.RespondAsync(BuildMessage(await GetContent()));
            return response?.Id;
        }
        public override async Task UpdateResponse(object e, int index)
        {
            Miss.CurrentMiss = index;
            ComponentInteractionCreateEventArgs args = (ComponentInteractionCreateEventArgs)e;
            await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder(BuildMessage(await GetContent())));
        }
    }
    public class CompactResponse : MessageResponse
    {
        public CompactResponse(ServerContext context, GuildSettings settings, MessageCreateEventArgs e) : base(context, settings, e) {}
        public override async Task<ulong?> CreateResponse()
        {
            await SendReactions(source, MissCount);
            return source.Id;
        }
        public override async Task UpdateResponse(object e, int index)
        {
            Miss.CurrentMiss = index;
            if (response == null)
            {
                await base.CreateResponse();
            }
            else
            {
                await base.UpdateResponse(e, index);
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