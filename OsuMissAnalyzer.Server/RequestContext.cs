using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using OsuMissAnalyzer.Server.Models;
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

        public void LoadFrom(ScoreRequest req)
        {
            GuildId = req.GuildId;
            ChannelId = req.ChannelId;
        }
        public void LoadFrom(ScoreResponse res)
        {
            GuildId = res.GuildId;
            ChannelId = res.ChannelId;
            MessageId = res.MessageId;
        }
    }
}