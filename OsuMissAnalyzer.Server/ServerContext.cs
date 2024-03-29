using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using OsuMissAnalyzer.Server.Settings;
using System.Net.Http;
using System.Net.WebSockets;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Reflection;
using System.Text;
using OsuMissAnalyzer.Server.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace OsuMissAnalyzer.Server
{
    public enum Source { USER, BOT, ATTACHMENT }
    public class ServerContext : IHostedService
    {
        private readonly ServerOptions serverOptions;
        private readonly DiscordShardedClient discord;
        private readonly OsuApi api;
        private readonly HttpClient webClient;
        private readonly ResponseCache cachedMisses;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly IHostEnvironment env;
        private readonly ILogger<ServerContext> logger;
        private readonly IDataLogger dLog;

        private static string[] pfpPrefixes = { "https://a.ppy.sh/", "http://s.ppy.sh/a/" };
        private static Regex partialBeatmapRegex = new Regex("^\\d+#osu/(\\d+)");
        private static Regex modRegex = new Regex("](?: \\+([A-Z]+))?\\n");
        public static DiscordEmoji[] numberEmojis;

        public ServerContext(DiscordShardedClient discord, OsuApi api, HttpClient webClient,
                ResponseCache cachedMisses, IOptions<ServerOptions> serverOptions, IServiceScopeFactory scopeFactory,
                IHostEnvironment env, ILogger<ServerContext> logger, IDataLogger dLog)
        {
            this.serverOptions = serverOptions.Value;
            this.discord = discord;
            this.api = api;
            this.webClient = webClient;
            this.cachedMisses = cachedMisses;
            this.scopeFactory = scopeFactory;
            this.env = env;
            this.logger = logger;
            this.dLog = dLog;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (env.IsDevelopment()) logger.LogInformation("Started in test mode");
            string gitCommit;
            using (var stream = Assembly.GetEntryAssembly().GetManifestResourceStream("OsuMissAnalyzer.Server.Resources.GitCommit.txt"))
            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            {
                gitCommit = streamReader.ReadToEnd();
            }
            logger.LogInformation(gitCommit);

            var apiToken = api.RefreshToken();

            numberEmojis = new DiscordEmoji[10];

            for (int i = 0; i < 10; i++)
            {
                numberEmojis[i] = DiscordEmoji.FromUnicode(i + "\ufe0f\u20e3");
            }

            discord.MessageCreated += async (d, e) =>
            {
                await HandleMessage(d, e);
            };

            discord.MessageReactionAdded += HandleReaction;

            discord.ComponentInteractionCreated += HandleInteraction;

            discord.ClientErrored += async (d, e) =>
            {
                logger.LogError(e.Exception, e.EventName);
                await Task.CompletedTask;
            };

            discord.SocketErrored += async (d, e) =>
            {
                if (e.Exception.GetType() == typeof(WebSocketException)
                    && e.Exception.HResult == unchecked((int)0x80004005)) return;

                logger.LogError(e.Exception, "Socket Error");
                await Task.CompletedTask;
            };

            dLog.UpdateLogs += () => dLog.LogAbsolute(DataPoint.ServersJoined, discord.ShardClients.Select(s => s.Value.Guilds?.Count ?? 0).Sum());

            await apiToken;
            await discord.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await discord.StopAsync();
        }

        public async Task HandleMessage(DiscordClient discord, MessageCreateEventArgs e)
        {
            dLog.Log(DataPoint.EventsHandled);
            if (env.IsDevelopment() && e.Guild?.Id != serverOptions.TestGuild) return;
            _ = Task.Run(async () =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var requestContext = scope.ServiceProvider.GetRequiredService<RequestContext>();
                requestContext.LoadFrom(e);
                ServerReplayLoader replayLoader = scope.ServiceProvider.GetRequiredService<ServerReplayLoader>();

                //attachment
                foreach (var attachment in e.Message.Attachments)
                {
                    if (attachment.FileName.EndsWith(".osr"))
                    {
                        logger.LogInformation("processing attachment");
                        dLog.Log(DataPoint.AttachmentCalls);
                        string dest = Path.Combine(serverOptions.ServerDir, "replays", attachment.FileName);

                        using (var stream = await webClient.GetStreamAsync(attachment.Url))
                        using (var file = File.OpenWrite(dest))
                        {
                            await stream.CopyToAsync(file);
                        }

                        replayLoader.ReplayFile = dest;
                        replayLoader.Source = Source.ATTACHMENT;
                    }
                }

                //bot
                if (requestContext.GuildOptions.AutoResponses && botIds.ContainsKey(e.Author.Id) && rsFunc[e.Author.Id](replayLoader, e))
                {
                    logger.LogInformation($"processing {botIds[e.Author.Id]} message");
                    dLog.Log(DataPoint.BotCalls);
                    replayLoader.UserScores = "recent";
                    replayLoader.FailedScores = true;
                    replayLoader.PlayIndex = 0;
                    replayLoader.Source = Source.BOT;
                }

                if (replayLoader.Source.HasValue)
                {
                    var responseFactory = scope.ServiceProvider.GetRequiredService<ResponseFactory>();
                    await responseFactory.CreateMessageResponse(e.Message);
                }
            });
            await Task.CompletedTask;
        }

        public async Task HandleInteraction(DiscordClient discord, ComponentInteractionCreateEventArgs e)
        {
            dLog.Log(DataPoint.EventsHandled);
            if (env.IsDevelopment() && e.Message.Channel.GuildId != serverOptions.TestGuild) return;

            Response response = null;
            if ((cachedMisses.TryGetResponse(e.Message.Id, out response)
                || (e.Message.Interaction != null && cachedMisses.TryGetResponse(e.Message.Interaction.Id, out response)))
                && !e.User.IsCurrent && !e.User.IsBot)
            {
                int index = int.Parse(e.Id) - 1;
                dLog.Log(DataPoint.ReactionCalls);
                _ = Task.Run(() => cachedMisses.UpdateResponse(e, response, index));
                await Task.CompletedTask;
            }
        }

        public async Task HandleReaction(DiscordClient discord, MessageReactionAddEventArgs e)
        {
            dLog.Log(DataPoint.EventsHandled);
            if (env.IsDevelopment() && e.Message.Channel.GuildId != serverOptions.TestGuild) return;
            if (cachedMisses.TryGetResponse(e.Message.Id, out Response response) && !e.User.IsCurrent && !e.User.IsBot)
            {
                var analyzer = response.Miss.MissAnalyzer;
                int index = Array.FindIndex(numberEmojis, t => t == e.Emoji) - 1;
                if (index >= 0 && index < Math.Min(analyzer.MissCount, numberEmojis.Length - 1))
                {
                    dLog.Log(DataPoint.ReactionCalls);
                    _ = Task.Run(() => cachedMisses.UpdateResponse(e, response, index));
                    await Task.CompletedTask;
                }
            }
        }

        const ulong OWO = 289066747443675143;
        const ulong BISMARCK = 207856807677263874;
        const ulong BOATBOT = 185013154198061056;
        const ulong TINYBOT = 470496878941962251;
        const ulong BATHBOT = 297073686916366336;

        delegate bool BotCall(ServerReplayLoader server, MessageCreateEventArgs e);

        Dictionary<ulong, string> botIds = new Dictionary<ulong, string>
        {
            [OWO] = "owo",
            [BOATBOT] = "boatbot",
            [BISMARCK] = "bismarck",
            // [TINYBOT] = "tinybot",
            // [BATHBOT] = "bathbot",
        };
        Dictionary<ulong, BotCall> rsFunc = new Dictionary<ulong, BotCall>
        {
            [OWO] = (ServerReplayLoader replayLoader, MessageCreateEventArgs e) =>
            {
                if (e.Message.Content != null && e.Message.Content.StartsWith("**Recent osu! Standard Play for") && e.Message.Embeds.Count > 0)
                {
                    replayLoader.UserId = GetIdFromEmbed(e.Message.Embeds[0]);
                    return true;
                }
                return false;
            },
            // [TINYBOT] = (ServerReplayLoader replayLoader, MessageCreateEventArgs e) =>
            // {
            //     if (e.Message.Embeds.Count > 0 && e.Message.Embeds[0].Author != null)
            //     {
            //         string header = e.Message.Embeds[0].Author.Name;
            //         if (header.StartsWith("Most recent osu! Standard play for")
            //             || (guildSettings.Tracking && header.StartsWith("New #") && header.EndsWith("in osu!Standard:")))
            //         {
            //             replayLoader.UserId = GetIdFromEmbed(e.Message.Embeds[0]);
            //             return true;
            //         }
            //     }
            //     return false;
            // },
            // [BATHBOT] = (ServerReplayLoader replayLoader, MessageCreateEventArgs e) =>
            // {
            //     if (e.Message.Embeds.Count > 0 && e.Message.Content.StartsWith("Try #"))
            //     {
            //         string prefix = "https://osu.ppy.sh/users/";
            //         string url = e.Message.Embeds[0].Author.Url.AbsoluteUri;
            //         if (url.StartsWith(prefix) && url.EndsWith("osu"))
            //         {
            //             replayLoader.UserId = url.Substring(prefix.Length).Split('/')[0];
            //             return true;
            //         }
            //     }
            //     return false;
            // },
            [BISMARCK] = (ServerReplayLoader replayLoader, MessageCreateEventArgs e) =>
            {
                if (e.Message.Content.Length == 0 && e.Message.Embeds.Count > 0)
                {
                    var em = e.Message.Embeds[0];
                    string url = em.Url.AbsoluteUri;
                    string prefix = "https://osu.ppy.sh/scores/osu/";
                    string mapPrefix = "https://osu.ppy.sh/beatmapsets/";
                    if (url.StartsWith(prefix) && em.Description.Contains(mapPrefix))
                    {
                        replayLoader.ScoreId = ulong.Parse(url.Substring(prefix.Length));
                        string urlEnd = em.Description.Substring(em.Description.IndexOf(mapPrefix) + mapPrefix.Length);
                        var match = partialBeatmapRegex.Match(urlEnd);
                        var modMatch = modRegex.Match(urlEnd);
                        if (match.Success && modMatch.Success)
                        {
                            replayLoader.BeatmapId = match.Groups[1].Value;
                            replayLoader.Mods = modMatch.Groups[1].Value;
                            return true;
                        }
                    }
                    replayLoader.UserId = GetIdFromEmbed(em);
                    return true;
                }
                return false;
            },
            [BOATBOT] = (ServerReplayLoader replayLoader, MessageCreateEventArgs e) =>
            {
                if (e.Message.Content.StartsWith("Try #") && e.Message.Embeds.Count > 0)
                {
                    replayLoader.UserId = GetIdFromEmbed(e.Message.Embeds[0]);
                    return true;
                }
                return false;
            },
        };
        private static string GetIdFromEmbed(DiscordEmbed embed)
        {
            string url = embed.Author.IconUrl.ToString();
            string prefixStr = null;
            foreach (var s in pfpPrefixes)
            {
                if (url.StartsWith(s)) prefixStr = s;
            }
            if (prefixStr != null)
            {
                return url.Substring(prefixStr.Length).Split('?')[0];
            }
            return null;
        }
    }
}