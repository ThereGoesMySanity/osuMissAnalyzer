using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Caching.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.Server.Database;
using OsuMissAnalyzer.Server.Settings;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace OsuMissAnalyzer.Server
{
    public enum Source { USER, BOT, ATTACHMENT }
    public class ServerContext
    {
        public OsuApi Api { get; private set; }
        public ServerBeatmapDb BeatmapDb { get; private set; }
        public ServerReplayDb ReplayDb { get; private set; }
        public MemoryCache<ulong, Response> CachedMisses { get; private set; }
        public DiscordShardedClient Discord { get; private set; }
        public ServerSettings Settings { get; private set; }

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

        private Stopwatch status;
        private static TimeSpan statusRefreshRate = new TimeSpan(0, 5, 0);



        public async Task<bool> Init(string[] args)
        {
            Settings = ServerSettings.Load();
            if (!await Settings.Init(args)) return false;
            Logger.Instance = new Logger(Path.Combine(Settings.ServerDir, "log.csv"), Settings.WebHook);
            if (Settings.Test) await Logger.WriteLine("Started in test mode");
            await Logger.WriteLine(Settings.GitCommit);
            Api = new OsuApi(Settings.OsuId, Settings.OsuSecret, Settings.OsuApiKey);
            var apiToken = Api.RefreshToken();

            Directory.CreateDirectory(Path.Combine(Settings.ServerDir, "beatmaps"));
            Directory.CreateDirectory(Path.Combine(Settings.ServerDir, "replays"));
            BeatmapDb = new ServerBeatmapDb(Api, Settings.ServerDir, Settings.Reload);
            ReplayDb = new ServerReplayDb(Api, Settings.ServerDir);
            CachedMisses = new MemoryCache<ulong, Response>(128);
            CachedMisses.SetPolicy(typeof(LfuEvictionPolicy<,>));

            Discord = new DiscordShardedClient(new DiscordConfiguration
            {
                Token = Settings.DiscordToken,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.Guilds |
                DiscordIntents.GuildMessages |
                DiscordIntents.GuildMessageReactions |
                DiscordIntents.DirectMessages |
                DiscordIntents.DirectMessageReactions
            });
            var slash = await Discord.UseSlashCommandsAsync(new SlashCommandsConfiguration
            {
                Services = new ServiceCollection().AddSingleton(this).BuildServiceProvider(),
            });
            if (Settings.Test)
            {
                slash.RegisterCommands<Commands>(Settings.TestGuild);
            }
            else
            {
                slash.RegisterCommands<Commands>();
            }

            numberEmojis = new DiscordEmoji[10];

            for (int i = 0; i < 10; i++)
            {
                numberEmojis[i] = DiscordEmoji.FromUnicode(i+"\ufe0f\u20e3");
            }

            status = new Stopwatch();
            status.Start();

            Discord.MessageCreated += async (d, e) =>
            {
                await HandleMessage(d, e);
                await CheckStatus();
            };

            Discord.MessageReactionAdded += HandleReaction;

            Discord.ComponentInteractionCreated += HandleInteraction;

            Discord.ClientErrored += async (d, e) =>
            {
                await Logger.WriteLine(e.EventName);
                await Logger.LogException(e.Exception);
            };

            Discord.SocketErrored += async (d, e) =>
            {
                await Logger.LogException(e.Exception);
            };

            foreach (var s in slash) {
                s.Value.SlashCommandErrored += async (d, e) =>
                {
                    await Logger.WriteLine(e.Context.CommandName);
                    await Logger.LogException(e.Exception);
                };
            }
            Logger.Instance.UpdateLogs += () => Logger.LogAbsolute(Logging.ServersJoined, Discord.ShardClients.Select(s => s.Value.Guilds?.Count ?? 0).Sum());

            await apiToken;
            return true;
        }

        public async Task Start()
        {
            await Discord.StartAsync();
        }

        public async Task CheckStatus()
        {
            if (status.Elapsed > statusRefreshRate)
            {
                status.Restart();
                string stat = Settings.Test? "Down for maintenance - be back soon!"
                                           : "/help for help!";
                await Discord.UpdateStatusAsync(new DiscordActivity(stat));
            }
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
            if (Settings.Test && e.Guild?.Id != Settings.TestGuild) return;
            var guildSettings = Settings.GetGuild(e.Channel);

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
                    string dest = Path.Combine(Settings.ServerDir, "replays", attachment.FileName);
                    using (WebClient w = new WebClient())
                    {
                        w.DownloadFile(attachment.Url, dest);
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

            if (replayLoader.Source != null)
            {
                Response r = (replayLoader.Source == Source.BOT && guildSettings.Compact)
                                ? new CompactResponse(this, guildSettings, e) : new MessageResponse(this, guildSettings, e);
                Task.Run(() => CreateResponse(discord, r, replayLoader));
            }
        }

        public async Task CreateResponse(DiscordClient discord, Response res, ServerReplayLoader replayLoader)
        {
            try
            {
                replayLoader.ErrorMessage ??= await replayLoader.Load(Api, ReplayDb, BeatmapDb);
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
                        UpdateCache(res, await res.CreateResponse());
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
            if (Settings.Test && e.Message.Channel.GuildId != Settings.TestGuild) return;

            Response response = null;
            if (CachedMisses.Contains(e.Message.Id)) response = CachedMisses[e.Message.Id];
            if (e.Message.Interaction != null && CachedMisses.Contains(e.Message.Interaction.Id)) response = CachedMisses[e.Message.Interaction.Id];

            if (response != null && !e.User.IsCurrent && !e.User.IsBot)
            {
                int index = int.Parse(e.Id) - 1;
                Logger.Log(Logging.ReactionCalls);
                Task.Run(() => UpdateResponse(e, response, index));
                await Task.CompletedTask;
            }
        }

        public async Task HandleReaction(DiscordClient discord, MessageReactionAddEventArgs e)
        {
            Logger.Log(Logging.EventsHandled);
            if (Settings.Test && e.Message.Channel.GuildId != Settings.TestGuild) return;
            if (CachedMisses.Contains(e.Message.Id) && !e.User.IsCurrent && !e.User.IsBot)
            {
                var response = CachedMisses[e.Message.Id];
                var analyzer = response.Miss.MissAnalyzer;
                int index = Array.FindIndex(numberEmojis, t => t == e.Emoji) - 1;
                if (index >= 0 && index < Math.Min(analyzer.MissCount, numberEmojis.Length - 1))
                {
                    Logger.Log(Logging.ReactionCalls);
                    Task.Run(() => UpdateResponse(e, response, index));
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
            DiscordMessageBuilder message = new DiscordMessageBuilder().WithFile("miss.png", GetStream(analyzer.DrawSelectedHitObject(area)));
            return (await (await discord.GetChannelAsync(Settings.DumpChannel)).SendMessageAsync(message)).Attachments[0].Url;
        }
        

        private static MemoryStream GetStream(Image bitmap)
        {
            MemoryStream s = new MemoryStream();
            bitmap.SaveAsPng(s);
            s.Seek(0, SeekOrigin.Begin);
            return s;
        }

        public async Task Close()
        {
            BeatmapDb.Close();
            Settings.Save();
            await Discord.StopAsync();
        }

        const ulong OWO = 289066747443675143;
        const ulong BISMARCK = 207856807677263874;
        const ulong BOATBOT = 185013154198061056;
        const ulong TINYBOT = 470496878941962251;
        const ulong BATHBOT = 297073686916366336;

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