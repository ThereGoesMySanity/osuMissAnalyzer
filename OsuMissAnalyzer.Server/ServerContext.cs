using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Caching.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Mono.Options;
using Newtonsoft.Json;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.Server.Database;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server
{
    public enum Source { USER, BOT, ATTACHMENT }
    public class ServerContext
    {
        public OsuApi Api { get; private set; }
        public ServerBeatmapDb BeatmapDb { get; private set; }
        public ServerReplayDb ReplayDb { get; private set; }
        public MemoryCache<DiscordMessage, SavedMiss> CachedMisses { get; private set; }
        public DiscordClient Discord { get; private set; }

        const int size = 480;
        const string HelpMessage = @"osu! Miss Analyzer (https://github.com/ThereGoesMySanity/osuMissAnalyzer) bot
```
Usage:
  ${user-recent|user-top} <username> [<index>]
    Finds #index recent/top play for username (index defaults to 1)
  $beatmap <beatmap id/beatmap link> [<index>]
    Finds #index score on beatmap (index defaults to 1)

Automatically responds to >rs from owo bot if the replay is saved online
Automatically responds to uploaded replay files
DM ThereGoesMySanity#2622 if you need help/want this bot on your server
```
Full readme at https://github.com/ThereGoesMySanity/osuMissAnalyzer/tree/missAnalyzer/OsuMissAnalyzer.Server";
        private static string[] pfpPrefixes = {"https://a.ppy.sh/", "http://s.ppy.sh/a/"};
        private static Regex messageRegex = new Regex("^(user-recent|user-top|beatmap) (.+?)(?: (\\d+))?$");
        private static Regex settingsRegex = new Regex("^settings (\\d+ )?(get|set ([A-Za-z]+) (.+))$");
        private static Regex beatmapRegex = new Regex("^(?:https?://(?:osu|old).ppy.sh/(?:beatmapsets/\\d+#osu|b)/)?(\\d+)");
        private static Regex partialBeatmapRegex = new Regex("^\\d+#osu/(\\d+)");
        private static Regex modRegex = new Regex("](?: \\+([A-Z]+))?\\n");
        private static Rectangle area = new Rectangle(0, 0, size, size);
        private static string[] numbers = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
        private DiscordEmoji[] numberEmojis;

        private Stopwatch status;
        private static TimeSpan statusRefreshRate = new TimeSpan(0, 5, 0);

        private ServerSettings Settings;


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
            CachedMisses = new MemoryCache<DiscordMessage, SavedMiss>(128);
            CachedMisses.SetPolicy(typeof(LfuEvictionPolicy<,>));

            Discord = new DiscordClient(new DiscordConfiguration
            {
                Token = Settings.DiscordToken,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.Guilds |
                DiscordIntents.GuildMessages |
                DiscordIntents.GuildMessageReactions |
                DiscordIntents.DirectMessages |
                DiscordIntents.DirectMessageReactions
            });

            numberEmojis = new DiscordEmoji[10];

            for (int i = 0; i < 10; i++)
            {
                numberEmojis[i] = DiscordEmoji.FromName(Discord, $":{numbers[i]}:");
            }

            status = new Stopwatch();
            status.Start();

            Discord.MessageCreated += async (d, e) =>
            {
                await HandleMessage(d, e);
                await CheckStatus();
            };

            Discord.MessageReactionAdded += HandleReaction;

            Discord.ClientErrored += async (d, e) =>
            {
                await Logger.WriteLine(e.EventName);
                await Logger.LogException(e.Exception);
            };

            Discord.SocketErrored += async (d, e) =>
            {
                await Logger.LogException(e.Exception);
            };

            Logger.Instance.UpdateLogs += () => Logger.LogAbsolute(Logging.ServersJoined, Discord?.Guilds?.Count ?? 0);
            await apiToken;
            return true;
        }

        public async Task Start()
        {
            await Discord.ConnectAsync();
        }

        public async Task CheckStatus()
        {
            if (status.Elapsed > statusRefreshRate)
            {
                status.Restart();
                string stat = Settings.Test? "Down for maintenance - be back soon!"
                                           : ">miss help for help!";
                await Discord.UpdateStatusAsync(new DiscordActivity(stat));
            }
        }

        public async Task HandleMessage(DiscordClient discord, MessageCreateEventArgs e)
        {
            Logger.Log(Logging.EventsHandled);
            if (Settings.Test && e.Guild?.Id != Settings.TestChannel) return;
            var guildSettings = Settings.GetGuild(e.Channel);
            if (IsHelpRequest(e, guildSettings))
            {
                await e.Message.RespondAsync(HelpMessage.Replace("$", guildSettings.GetCommand("")));
                Logger.Log(Logging.HelpMessageCreated);
                return;
            }

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
            if (botIds.ContainsKey(e.Author.Id) && rsFunc[e.Author.Id](replayLoader, guildSettings, e))
            {
                await Logger.WriteLine($"processing {botIds[e.Author.Id]} message");
                Logger.Log(Logging.BotCalls);
                replayLoader.UserScores = "recent";
                replayLoader.FailedScores = true;
                replayLoader.PlayIndex = 0;
                replayLoader.Source = Source.BOT;
            }

            //user-triggered
            string prefix = guildSettings.GetCommand("");
            if (e.Message.Content.StartsWith(prefix))
            {
                Match messageMatch = messageRegex.Match(e.Message.Content.Substring(prefix.Length));
                if (messageMatch.Success)
                {
                    await Logger.WriteLine("processing user call");
                    Logger.Log(Logging.UserCalls);
                    replayLoader.Source = Source.USER;

                    replayLoader.PlayIndex = 0;
                    if (messageMatch.Groups.Count == 4 && messageMatch.Groups[3].Success)
                        replayLoader.PlayIndex = int.Parse(messageMatch.Groups[3].Value) - 1;

                    switch (messageMatch.Groups[1].Value)
                    {
                        case "user-recent":
                        case "user-top":
                            replayLoader.Username = messageMatch.Groups[2].Value;
                            replayLoader.UserScores = messageMatch.Groups[1].Value == "user-recent" ? "recent" : "best";
                            break;
                        case "beatmap":
                            var bmMatch = beatmapRegex.Match(messageMatch.Groups[2].Value);
                            if (bmMatch.Success)
                            {
                                replayLoader.BeatmapId = bmMatch.Groups[1].Value;
                            }
                            else
                            {
                                replayLoader.ErrorMessage = "Invalid beatmap link";
                            }
                            break;
                    }
                }
                //settings
                Match settingsMatch = settingsRegex.Match(e.Message.Content.Substring(prefix.Length));
                if (settingsMatch.Success)
                {
                    ulong? guildId = settingsMatch.Groups[1].Success? (ulong?)ulong.Parse(settingsMatch.Groups[1].Value) : null;
                    guildId ??= (!e.Channel.IsPrivate)? (ulong?)e.Channel.GuildId : null;
                    string response = null;
                    if (guildId != null)
                    {
                        var dGuild = await Discord.GetGuildAsync(guildId.Value);
                        var user = await dGuild.GetMemberAsync(e.Author.Id);
                        if (user.IsOwner || user.PermissionsIn(dGuild.GetDefaultChannel()).HasPermission(Permissions.Administrator))
                        {
                            var guild = Settings.GetGuild(guildId.Value);
                            if (!settingsMatch.Groups[2].Success || settingsMatch.Groups[2].Value.StartsWith("get"))
                            {
                                response = string.Join("\n", guild.GetSettings()
                                        .Select(s => $"{s.Key}: {s.Value}"));
                            }
                            else
                            {
                                string setting = settingsMatch.Groups[3].Value, value = settingsMatch.Groups[4].Value;
                                try
                                {
                                    response = guild.SetSetting(setting, value)? "Set successfully" : $"Setting {setting} does not exist";
                                }
                                catch (TargetInvocationException ex) when (ex.InnerException is FormatException)
                                {
                                    response = $"{value} not valid for setting {setting}";
                                }
                            }
                        }
                    } else response = "Please specify for which guild";
                    if (response != null) await e.Message.RespondAsync(response);
                }
            }

            if (replayLoader.Source != null)
            {
                Task.Run(() => ProcessMessage(e, guildSettings, replayLoader))
                    .ContinueWith(t => 
                    {
                        Console.WriteLine((t.Exception as AggregateException).InnerException)
                    }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        public async Task ProcessMessage(MessageCreateEventArgs e, GuildSettings guildSettings, ServerReplayLoader replayLoader)
        {
            try
            {
                replayLoader.ErrorMessage ??= await replayLoader.Load(Api, ReplayDb, BeatmapDb);
                if (replayLoader.Loaded)
                {
                    DiscordMessage message = null;
                    MissAnalyzer missAnalyzer = new MissAnalyzer(replayLoader);
                    if (missAnalyzer.MissCount == 0)
                    {
                        replayLoader.ErrorMessage = "No misses found.";
                    }
                    else if (replayLoader.Source == Source.BOT && guildSettings.Compact)
                    {
                        message = e.Message;
                        await SendReactions(message, missAnalyzer.MissCount);
                    }
                    else if (missAnalyzer.MissCount == 1)
                    {
                        string miss = await SendMissMessage(missAnalyzer, 0);
                        Logger.Log(Logging.MessageCreated);
                        await e.Message.RespondAsync(miss);
                    }
                    else if (missAnalyzer.MissCount > 1)
                    {
                        Logger.Log(Logging.MessageCreated);
                        message = await e.Message.RespondAsync($"Found **{missAnalyzer.MissCount}** misses");
                        await SendReactions(message, missAnalyzer.MissCount);
                    }
                    if (message != null)
                    {
                        CachedMisses[message] = new SavedMiss(missAnalyzer);
                        Logger.LogAbsolute(Logging.CachedMessages, CachedMisses.Count);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                replayLoader.ErrorMessage = ex.Message;
            }
            catch (Exception exc)
            {
                await Logger.LogException(exc);
            }

            if (replayLoader.ErrorMessage != null && (replayLoader.Source == Source.USER || replayLoader.Source == Source.ATTACHMENT))
            {
                Logger.Log(Logging.MessageCreated);
                Logger.Log(Logging.ErrorHandled);
                await Logger.WriteLine($"Error handled: {replayLoader.ErrorMessage}");
                await e.Message.RespondAsync(replayLoader.ErrorMessage);
            }
        }

        private async Task SendReactions(DiscordMessage message, int missCount)
        {
            for (int i = 1; i < Math.Min(missCount + 1, numberEmojis.Length); i++)
            {
                await message.CreateReactionAsync(numberEmojis[i]);
            }
        }

        public async Task HandleReaction(DiscordClient discord, MessageReactionAddEventArgs e)
        {
            Logger.Log(Logging.EventsHandled);
            if (Settings.Test && e.Message.Channel.GuildId != Settings.TestChannel) return;
            var guildSettings = Settings.GetGuild(e.Channel);
            if (CachedMisses.Contains(e.Message) && !e.User.IsCurrent && !e.User.IsBot)
            {
                var savedMiss = CachedMisses[e.Message];
                var analyzer = savedMiss.MissAnalyzer;
                // switch (e.Emoji.GetDiscordName())
                // {
                //     case ":heavy_plus_sign:":

                //     break;
                //     case ":heavy_minus_sign:":

                //     break;
                //     case ":"

                // }
                int index = Array.FindIndex(numberEmojis, t => t == e.Emoji) - 1;
                if (index >= 0 && index < Math.Min(analyzer.MissCount, numberEmojis.Length - 1))
                {
                    Task.Run(() => ProcessReaction(e, guildSettings, savedMiss, index));
                }
            }
        }

        public async Task ProcessReaction(MessageReactionAddEventArgs e, GuildSettings guildSettings, SavedMiss savedMiss, int index)
        {
            var analyzer = savedMiss.MissAnalyzer;
            Logger.Log(Logging.ReactionCalls);
            if (savedMiss.MissUrls[index] == null)
            {
                savedMiss.MissUrls[index] = await SendMissMessage(analyzer, index);
            }
            var message = e.Message;
            if (!message.Author.IsCurrent)
            {
                message = savedMiss.Response;
                if (message == null)
                {
                    var response = await e.Message.RespondAsync(savedMiss.MissUrls[index]);
                    Logger.Log(Logging.MessageCreated);
                    savedMiss.Response = response;
                    if (!guildSettings.Compact)
                    {
                        CachedMisses[response] = savedMiss;
                        await SendReactions(response, analyzer.MissCount);
                    }
                }
            }
            if (message != null)
            {
                Logger.Log(Logging.MessageEdited);
                await message.ModifyAsync(savedMiss.MissUrls[index]);
            }
        }

        private async Task<string> SendMissMessage(MissAnalyzer analyzer, int index)
        {
            analyzer.CurrentObject = index;
            DiscordMessageBuilder message = new DiscordMessageBuilder().WithFile("miss.png", GetStream(analyzer.DrawSelectedHitObject(area)));
            return (await (await Discord.GetChannelAsync(Settings.DumpChannel)).SendMessageAsync(message)).Attachments[0].Url;
        }
        
        private static MemoryStream GetStream(Bitmap bitmap)
        {
            MemoryStream s = new MemoryStream();
            bitmap.Save(s, System.Drawing.Imaging.ImageFormat.Png);
            s.Seek(0, SeekOrigin.Begin);
            return s;
        }

        public bool IsHelpRequest(MessageCreateEventArgs e, GuildSettings guildSettings)
        {
            return !e.Author.IsCurrent && (e.Message.Content.StartsWith(guildSettings.GetCommand("help"))
                    || ((e.Message.Channel.IsPrivate || (e.MentionedUsers?.Any(u => u?.IsCurrent ?? false) ?? false)) 
                            && e.Message.Content.IndexOf("help", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    || e.Message.Content == guildSettings.Prefix);
        }

        public async Task Close()
        {
            BeatmapDb.Close();
            Settings.Save();
            await Discord.DisconnectAsync();
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
                if (e.Message.Content.StartsWith("**Recent osu! Standard Play for"))
                {
                    replayLoader.UserId = GetIdFromEmbed(e.Message.Embeds[0]);
                    return true;
                }
                return false;
            },
            [TINYBOT] = (ServerReplayLoader replayLoader, GuildSettings guildSettings, MessageCreateEventArgs e) =>
            {
                if (e.Message.Embeds.Count > 0)
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
                    string prefix = "https://osu.ppy.sh/u/";
                    string url = e.Message.Embeds[0].Author.Url.AbsoluteUri;
                    if (url.StartsWith(prefix))
                    {
                        replayLoader.UserId = url.Substring(prefix.Length);
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