using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using OsuMissAnalyzer.Server.Api;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server
{
    public class RequestContext
    {
        private readonly GuildManager guildManager;
        private readonly DiscordShardedClient discord;

        public GuildOptions GuildOptions { get; set; }
        public ulong GuildId { get => GuildOptions.Id; set => GuildOptions = guildManager.GetGuild(value); }

        public DiscordClient DiscordClient => discord.GetShard(GuildId);

        public ulong ChannelId { get; set; }
        public async Task<DiscordChannel> GetChannelAsync() => await DiscordClient.GetChannelAsync(ChannelId);

        public ulong MessageId { get; set; }
        public async Task<DiscordMessage> GetMessageAsync() => await (await GetChannelAsync()).GetMessageAsync(MessageId);

        public RequestContext(GuildManager guildManager, DiscordShardedClient discord)
        {
            this.guildManager = guildManager;
            this.discord = discord;
        }

        public void LoadFrom(ScoreResponse res)
        {
            GuildId = res.GuildId;
            ChannelId = res.ChannelId;
            MessageId = res.MessageId;
        }
        public void LoadFrom(MessageCreateEventArgs e)
        {
            GuildId = e.Guild.Id;
            ChannelId = e.Channel.Id;
            MessageId = e.Message.Id;
        }
        public void LoadFrom(InteractionContext ctx)
        {
            GuildId = ctx.Guild.Id;
            ChannelId = ctx.Channel.Id;
            MessageId = ctx.InteractionId;
        }
    }
}