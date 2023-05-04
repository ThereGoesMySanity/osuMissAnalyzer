using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.Server.Database;
using OsuMissAnalyzer.Server.Settings;
using DSharpPlus.SlashCommands;
using SixLabors.ImageSharp;
using System.Net.Http;
using System.Net.WebSockets;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Reflection;
using System.Text;

namespace OsuMissAnalyzer.Server
{
    public enum Source { USER, BOT, ATTACHMENT }
    public class ServerContext : IHostedService
    {
        public MemoryCache<ulong, Response> CachedMisses { get; private set; }

        const int size = 480;
        public const string HelpMessage = @"osu! Miss Analyzer bot
```
Usage:
  /user {recent|top} <username> [<index>]
    Finds #index recent/top play for username (index defaults to 1)
  /beatmap <beatmap id/beatmap link> [<index>]
    Finds #index score on beatmap (index defaults to 1)

Automatically responds to >rs from owo bot if the replay is saved online
Automatically responds to uploaded replay files
Click ""Add to Server"" on the bot's profile to get this bot in your server!
DM ThereGoesMySanity#2622 if you need help
```
Full readme and source at https://github.com/ThereGoesMySanity/osuMissAnalyzer/tree/missAnalyzer/OsuMissAnalyzer.Server";
        private static string[] pfpPrefixes = {"https://a.ppy.sh/", "http://s.ppy.sh/a/"};
        private static Regex settingsRegex = new Regex("^settings (\\d+ )?(get|set ([A-Za-z]+) (.+))$");
        private static Regex partialBeatmapRegex = new Regex("^\\d+#osu/(\\d+)");
        private static Regex modRegex = new Regex("](?: \\+([A-Z]+))?\\n");
        private static Rectangle area = new Rectangle(0, 0, size, size);
        private static string[] numbers = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
        public static DiscordEmoji[] numberEmojis;

        public ServerContext(ServerOptions serverOptions, DiscordShardedClient discord, OsuApi api, HttpClient webClient,
                ServerBeatmapDb beatmapDb, ServerReplayDb replayDb,
                GuildManager guildManager,
                ILogger logger)
        {
            this.serverOptions = serverOptions;
            this.discord = discord;
            this.api = api;
            this.webClient = webClient;
            this.beatmapDb = beatmapDb;
            this.replayDb = replayDb;
            this.guildManager = guildManager;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (serverOptions.Test) await Logger.WriteLine("Started in test mode");
            string gitCommit;
            using (var stream = Assembly.GetEntryAssembly().GetManifestResourceStream("OsuMissAnalyzer.Server.Resources.GitCommit.txt"))
            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            {
                gitCommit = streamReader.ReadToEnd();
            }
            await Logger.WriteLine(gitCommit);

            var apiToken = api.RefreshToken();

            CachedMisses = new MemoryCache<ulong, Response>(128);
            CachedMisses.SetPolicy(typeof(LfuEvictionPolicy<,>));

            numberEmojis = new DiscordEmoji[10];

            for (int i = 0; i < 10; i++)
            {
                numberEmojis[i] = DiscordEmoji.FromUnicode(i+"\ufe0f\u20e3");
            }

            discord.MessageCreated += async (d, e) =>
            {
                await HandleMessage(d, e);
            };

            discord.MessageReactionAdded += HandleReaction;

            discord.ComponentInteractionCreated += HandleInteraction;

            discord.ClientErrored += async (d, e) =>
            {
                await Logger.WriteLine(e.EventName);
                await Logger.LogException(e.Exception);
            };

            discord.SocketErrored += async (d, e) =>
            {
                if (e.Exception.GetType() == typeof(WebSocketException)
                    && e.Exception.HResult == unchecked((int)0x80004005)) return;

                await Logger.LogException(e.Exception);
            };

            Logger.Instance.UpdateLogs += () => Logger.LogAbsolute(Logging.ServersJoined, discord.ShardClients.Select(s => s.Value.Guilds?.Count ?? 0).Sum());

            await apiToken;
            await discord.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await discord.StopAsync();
        }

        public bool IsHelpRequest(MessageCreateEventArgs e, GuildSettings guildSettings)
        {
            return !e.Author.IsCurrent && (e.Message.Content.StartsWith(guildSettings.GetCommand("help"))
                    || ((e.Message.Channel.IsPrivate || (e.MentionedUsers?.Any(u => u?.IsCurrent ?? false) ?? false)) 
                            && e.Message.Content.IndexOf("help", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    || e.Message.Content == guildSettings.Prefix);
        }

        public async Task HandleMessage(DiscordClient discord, MessageCreateEventArgs e)
        {
            Logger.Log(Logging.EventsHandled);
            if (serverOptions.Test && e.Guild?.Id != serverOptions.TestGuild) return;
            var guildSettings = guildManager.GetGuild(e.Channel);

            // if (IsHelpRequest(e, guildSettings)) 
            // {
            //     await e.Message.RespondAsync("MissAnalyzer now uses slash commands! Type /help for help.");
            // }

            ServerReplayLoader replayLoader = new ServerReplayLoader();
            //attachment
            foreach (var attachment in e.Message.Attachments)
            {
                if (attachment.FileName.EndsWith(".osr"))
                {
                    await Logger.WriteLine("processing attachment");
                    Logger.Log(Logging.AttachmentCalls);
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
            if (guildSettings.AutoResponses && botIds.ContainsKey(e.Author.Id) && rsFunc[e.Author.Id](replayLoader, guildSettings, e))
            {
                await Logger.WriteLine($"processing {botIds[e.Author.Id]} message");
                Logger.Log(Logging.BotCalls);
                replayLoader.UserScores = "recent";
                replayLoader.FailedScores = true;
                replayLoader.PlayIndex = 0;
                replayLoader.Source = Source.BOT;
            }

            if (replayLoader.Source.HasValue)
            {
                Response r = MessageResponse.CreateMessageResponse(replayLoader.Source.Value, this, guildSettings, e.Message);
                _ = Task.Run(() => CreateResponse(discord, r, replayLoader));
            }
        }

        public async Task<ulong?> CreateResponse(DiscordClient discord, Response res, ServerReplayLoader replayLoader)
        {
            ulong? response = null;
            try
            {
                replayLoader.ErrorMessage ??= await replayLoader.Load(res.GuildSettings, this);
                if (replayLoader.Loaded)
                {
                    MissAnalyzer missAnalyzer = new MissAnalyzer(replayLoader);
                    res.Miss = new SavedMiss(discord, missAnalyzer);
                    if (missAnalyzer.MissCount == 0)
                    {
                        replayLoader.ErrorMessage = "No misses found.";
                    }
                    else
                    {
                        response = await res.CreateResponse();
                        UpdateCache(res, response);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                replayLoader.ErrorMessage = ex.Message;
            }
            catch (Exception exc)
            {
                await Logger.LogException(exc, Logger.LogLevel.NORMAL);
            }

            if (replayLoader.ErrorMessage != null && (replayLoader.Source == Source.USER || replayLoader.Source == Source.ATTACHMENT))
            {
                Logger.Log(Logging.MessageCreated);
                Logger.Log(Logging.ErrorHandled);
                await Logger.WriteLine($"Error handled: {replayLoader.ErrorMessage}");
                await res.CreateErrorResponse(replayLoader.ErrorMessage);
            }
            return response;
        }

        private void UpdateCache(Response res, ulong? key) {
            if (key.HasValue)
            {
                Logger.Log(Logging.MessageCreated);
                CachedMisses[key.Value] = res;
                Logger.LogAbsolute(Logging.CachedMessages, CachedMisses.Count);
            }
        }

        public async Task HandleInteraction(DiscordClient discord, ComponentInteractionCreateEventArgs e)
        {
            Logger.Log(Logging.EventsHandled);
            if (serverOptions.Test && e.Message.Channel.GuildId != serverOptions.TestGuild) return;

            Response response = null;
            if (CachedMisses.Contains(e.Message.Id)) response = CachedMisses[e.Message.Id];
            if (e.Message.Interaction != null && CachedMisses.Contains(e.Message.Interaction.Id)) response = CachedMisses[e.Message.Interaction.Id];

            if (response != null && !e.User.IsCurrent && !e.User.IsBot)
            {
                int index = int.Parse(e.Id) - 1;
                Logger.Log(Logging.ReactionCalls);
                _ = Task.Run(() => UpdateResponse(e, response, index));
                await Task.CompletedTask;
            }
        }

        public async Task HandleReaction(DiscordClient discord, MessageReactionAddEventArgs e)
        {
            Logger.Log(Logging.EventsHandled);
            if (serverOptions.Test && e.Message.Channel.GuildId != serverOptions.TestGuild) return;
            if (CachedMisses.Contains(e.Message.Id) && !e.User.IsCurrent && !e.User.IsBot)
            {
                var response = CachedMisses[e.Message.Id];
                var analyzer = response.Miss.MissAnalyzer;
                int index = Array.FindIndex(numberEmojis, t => t == e.Emoji) - 1;
                if (index >= 0 && index < Math.Min(analyzer.MissCount, numberEmojis.Length - 1))
                {
                    Logger.Log(Logging.ReactionCalls);
                    _ = Task.Run(() => UpdateResponse(e, response, index));
                    await Task.CompletedTask;
                }
            }
        }

        public async Task UpdateResponse(object e, Response response, int index)
        {
            Logger.Log(Logging.MessageEdited);
            UpdateCache(response, await response.UpdateResponse(e, index));
        }

        public async Task<string> SendMissMessage(DiscordClient discord, MissAnalyzer analyzer, int index)
        {
            analyzer.CurrentObject = index;
            DiscordMessageBuilder message = new DiscordMessageBuilder().AddFile("miss.png", await GetStream(analyzer.DrawSelectedHitObject(area)));
            return (await (await discord.GetChannelAsync(serverOptions.DumpChannel)).SendMessageAsync(message)).Attachments[0].Url;
        }
        

        private static async Task<MemoryStream> GetStream(Image bitmap)
        {
            MemoryStream s = new MemoryStream();
            await bitmap.SaveAsPngAsync(s);
            s.Seek(0, SeekOrigin.Begin);
            return s;
        }


        const ulong OWO = 289066747443675143;
        const ulong BISMARCK = 207856807677263874;
        const ulong BOATBOT = 185013154198061056;
        const ulong TINYBOT = 470496878941962251;
        const ulong BATHBOT = 297073686916366336;
        private readonly ServerOptions serverOptions;
        private readonly DiscordShardedClient discord;
        private readonly OsuApi api;
        private readonly HttpClient webClient;
        private readonly ServerBeatmapDb beatmapDb;
        private readonly ServerReplayDb replayDb;
        private readonly GuildManager guildManager;
        private readonly ILogger logger;

        delegate bool BotCall(ServerReplayLoader server, GuildSettings guildSettings, MessageCreateEventArgs args);

        Dictionary<ulong, string> botIds = new Dictionary<ulong, string>
        {
            [OWO] = "owo",
            [BOATBOT] = "boatbot",
            [BISMARCK] = "bismarck",
            [TINYBOT] = "tinybot",
            [BATHBOT] = "bathbot",
        };
        Dictionary<ulong, BotCall> rsFunc = new Dictionary<ulong, BotCall>
        {
            [OWO] = (ServerReplayLoader replayLoader, GuildSettings guildSettings, MessageCreateEventArgs e) =>
            {
                if (e.Message.Content != null && e.Message.Content.StartsWith("**Recent osu! Standard Play for") && e.Message.Embeds.Count > 0)
                {
                    replayLoader.UserId = GetIdFromEmbed(e.Message.Embeds[0]);
                    return true;
                }
                return false;
            },
            [TINYBOT] = (ServerReplayLoader replayLoader, GuildSettings guildSettings, MessageCreateEventArgs e) =>
            {
                if (e.Message.Embeds.Count > 0 && e.Message.Embeds[0].Author != null)
                {
                    string header = e.Message.Embeds[0].Author.Name;
                    if (header.StartsWith("Most recent osu! Standard play for")
                        || (guildSettings.Tracking && header.StartsWith("New #") && header.EndsWith("in osu!Standard:")))
                    {
                        replayLoader.UserId = GetIdFromEmbed(e.Message.Embeds[0]);
                        return true;
                    }
                }
                return false;
            },
            [BATHBOT] = (ServerReplayLoader replayLoader, GuildSettings guildSettings, MessageCreateEventArgs e) =>
            {
                if (e.Message.Embeds.Count > 0 && e.Message.Content.StartsWith("Try #"))
                {
                    string prefix = "https://osu.ppy.sh/users/";
                    string url = e.Message.Embeds[0].Author.Url.AbsoluteUri;
                    if (url.StartsWith(prefix) && url.EndsWith("osu"))
                    {
                        replayLoader.UserId = url.Substring(prefix.Length).Split('/')[0];
                        return true;
                    }
                }
                return false;
            },
            [BISMARCK] = (ServerReplayLoader replayLoader, GuildSettings guildSettings, MessageCreateEventArgs e) =>
            {
                if (e.Message.Content.Length == 0 && e.Message.Embeds.Count > 0)
                {
                    var em = e.Message.Embeds[0];
                    string url = em.Url.AbsoluteUri;
                    string prefix = "https://osu.ppy.sh/scores/osu/";
                    string mapPrefix = "https://osu.ppy.sh/beatmapsets/";
                    if (url.StartsWith(prefix) && em.Description.Contains(mapPrefix))
                    {
                        replayLoader.ScoreId = url.Substring(prefix.Length);
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
            [BOATBOT] = (ServerReplayLoader replayLoader, GuildSettings guildSettings, MessageCreateEventArgs e) =>
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