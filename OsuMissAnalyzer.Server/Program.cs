using DSharpPlus;
using System;
using System.Threading.Tasks;
using Mono.Unix;
using System.Threading;
using System.Net;
using OsuMissAnalyzer.Server.Database;
using System.IO;
using OsuMissAnalyzer.Core;
using DSharpPlus.Entities;
using System.Drawing;
using System.Text.RegularExpressions;
using ReplayAPI;
using System.Runtime.Caching.Generic;
using Mono.Options;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using DSharpPlus.EventArgs;

namespace OsuMissAnalyzer.Server
{
    public class Program
    {
        const ulong OWO = 289066747443675143;
        const ulong BISMARCK = 207856807677263874;
        const ulong BOATBOT = 185013154198061056;

        delegate bool BotCall(ServerReplayLoader server, MessageCreateEventArgs args, ref DiscordEmbed embed);
        const ulong DUMP_CHANNEL = 753788360425734235L;
        const int size = 480;
        const string HELP_MESSAGE = @"osu! Miss Analyzer (https://github.com/ThereGoesMySanity/osuMissAnalyzer) bot
```
Usage:
  >miss {user-recent|user-top} <username> [<index>]
    Finds #index recent/top play for username (index defaults to 1)
  >miss beatmap <beatmap id/beatmap link> [<index>]
    Finds #index score on beatmap (index defaults to 1)

Automatically responds to >rs from owo bot if the replay is saved online
Automatically responds to uploaded replay files
DM ThereGoesMySanity#2622 if you need help/want this bot on your server
```";
        private static Rectangle area = new Rectangle(0, 0, size, size);
        static DiscordClient discord;
        static UnixPipes interruptPipe;
        [STAThread]
        public static void Main(string[] args)
        {
            interruptPipe = UnixPipes.CreatePipes();
            UnixSignal[] signals = new UnixSignal[] {
                new UnixSignal(Mono.Unix.Native.Signum.SIGTERM),
                new UnixSignal(Mono.Unix.Native.Signum.SIGINT),
            };
            Thread signalThread = new Thread(delegate ()
            {
                int index = UnixSignal.WaitAny(signals);
                interruptPipe.Writing.Write(BitConverter.GetBytes(index), 0, 4);
            });
            signalThread.IsBackground = true;
            signalThread.Start();
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        public enum Source { USER, BOT, ATTACHMENT }
        static async Task MainAsync(string[] args)
        {
            string serverDir = "";
            string osuId = "2558";
            string osuSecret = "";
            string osuApiKey = "";
            string discordToken = "";
            string webHook = "";
            string discordId = "752035690237394944";
            string discordPermissions = "100416";
            bool help = false, link = false, test = false;
            string apiv2Req = null;
            var opts = new OptionSet() {
                {"d|dir=", "Set server storage dir (default: ./)", b => serverDir = b},
                {"s|secret=", "Set client secret (osu!) (required)", s => osuSecret = s},
                {"k|key=", "osu! api v1 key (required)", k => osuApiKey = k},
                {"id=", "osu! client id (default: mine)", id => osuId = id},
                {"t|token=", "discord bot token (required)", t => discordToken = t},
                {"h|help", "displays help", a => help = a != null},
                {"l|link", "displays bot link and exits", l => link = l != null},
                {"apiRequest=", "does api request", a => apiv2Req = a},
                {"test", "test server only", t => test = t != null},
                {"w|webhook=", "webhook for output", w => webHook = w},
            };
            opts.Parse(args);
            string botLink = $"https://discordapp.com/oauth2/authorize?client_id={discordId}&scope=bot&permissions={discordPermissions}";
            if (link)
            {
                Console.WriteLine(botLink);
                return;
            }

            if (help || (osuSecret.Length * osuApiKey.Length * discordToken.Length) == 0)
            {
                Console.WriteLine(
$@"osu! Miss Analyzer, Discord Bot Edition
Bot link: https://discordapp.com/oauth2/authorize?client_id={discordId}&scope=bot&permissions={discordPermissions}");
                opts.WriteOptionDescriptions(Console.Out);
                return;
            }
            if (apiv2Req != null)
            {
                OsuApi api2 = new OsuApi(osuId, osuSecret, osuApiKey);
                Console.WriteLine(await api2.GetApiv2(apiv2Req));
                return;
            }

            Logger.Instance = new Logger(Path.Combine(serverDir, "log.csv"), webHook);
            OsuApi api = new OsuApi(osuId, osuSecret, osuApiKey);
            var apiToken = api.RefreshToken();
            Directory.CreateDirectory(Path.Combine(serverDir, "beatmaps"));
            Directory.CreateDirectory(Path.Combine(serverDir, "replays"));
            var beatmapDatabase = new ServerBeatmapDb(api, serverDir);
            var replayDatabase = new ServerReplayDb(api, serverDir);
            string pfpPrefix = "https://a.ppy.sh/";
            Regex messageRegex = new Regex("^>miss (user-recent|user-top|beatmap) (.+?)(?: (\\d+))?$");
            Regex beatmapRegex = new Regex("^(?:https?://(?:osu|old).ppy.sh/(?:beatmapsets/\\d+#osu|b)/)?(\\d+)");
            Regex partialBeatmapRegex = new Regex("^\\d+#osu/(\\d+)");
            Regex modRegex = new Regex("](?: \\+([A-Z]+))?\\n");

            var cachedMisses = new MemoryCache<DiscordMessage, SavedMiss>(128);
            cachedMisses.SetPolicy(typeof(LfuEvictionPolicy<,>));

            var botIds = new Dictionary<ulong, string>
            {
                [OWO] = "owo",
                [BOATBOT] = "boatbot",
                [BISMARCK] = "bismarck",
            };
            var rsFunc = new Dictionary<ulong, BotCall>
            {
                [OWO] = (ServerReplayLoader replayLoader, MessageCreateEventArgs e, ref DiscordEmbed embed) =>
                {
                    if (e.Message.Content.StartsWith("**Most Recent osu! Standard Play for"))
                    {
                        embed = e.Message.Embeds[0];
                        return true;
                    }
                    return false;
                },
                [BISMARCK] = (ServerReplayLoader replayLoader, MessageCreateEventArgs e, ref DiscordEmbed embed) =>
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
                            if(match.Success && modMatch.Success)
                            {
                                replayLoader.BeatmapId = match.Groups[1].Value;
                                replayLoader.Mods = modMatch.Groups[1].Value;
                                return true;
                            }
                        }
                        embed = em;
                        return true;
                    }
                    return false;
                },
                [BOATBOT] = (ServerReplayLoader replayLoader, MessageCreateEventArgs e, ref DiscordEmbed embed) =>
                {
                    if (e.Message.Content.StartsWith("Try #") && e.Message.Embeds.Count > 0)
                    {
                        embed = e.Message.Embeds[0];
                        return true;
                    }
                    return false;
                },
            };

            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = discordToken,
                TokenType = TokenType.Bot
            });

            string[] numbers = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
            DiscordEmoji[] numberEmojis = new DiscordEmoji[10];

            for (int i = 0; i < 10; i++)
            {
                numberEmojis[i] = DiscordEmoji.FromName(discord, $":{numbers[i]}:");
            }
            discord.MessageCreated += async e =>
            {
                if (test && e.Guild.Id != 753465280465862757L) return;
                Logger.LogAbsolute(Logging.ServersJoined, discord.Guilds.Count);
                Logger.Log(Logging.EventsHandled);
                if (e.Message.Content.StartsWith(">miss help")
                    || (e.Message.Channel.IsPrivate && e.Message.Content.IndexOf("help", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    || e.Message.Content == ">miss")
                {
                    await e.Message.RespondAsync(HELP_MESSAGE);
                    return;
                }
                ServerReplayLoader replayLoader = new ServerReplayLoader();
                Source? source = null;

                //attachment
                foreach (var attachment in e.Message.Attachments)
                {
                    if (attachment.FileName.EndsWith(".osr"))
                    {
                        await Logger.WriteLine("processing attachment");
                        Logger.Log(Logging.AttachmentCalls);
                        string dest = Path.Combine(serverDir, "replays", attachment.FileName);
                        using (WebClient w = new WebClient())
                        {
                            w.DownloadFile(attachment.Url, dest);
                        }
                        replayLoader.ReplayFile = dest;
                        source = Source.ATTACHMENT;
                    }
                }
                
                //bot
                if (botIds.ContainsKey(e.Author.Id))
                {
                    DiscordEmbed embed = null;
                    if (rsFunc[e.Author.Id](replayLoader, e, ref embed))
                    {
                        await Logger.WriteLine($"processing {botIds[e.Author.Id]} message");
                        Logger.Log(Logging.BotCalls);
                        if (embed != null)
                        {
                            string url = embed.Author.IconUrl.ToString();
                            if (url.StartsWith(pfpPrefix))
                            {
                                replayLoader.UserId = url.Substring(pfpPrefix.Length).Split('?')[0];
                                await Logger.WriteLine($"found embed with userid {replayLoader.UserId}");
                                replayLoader.UserScores = "recent";
                                replayLoader.FailedScores = true;
                                replayLoader.PlayIndex = 0;
                            }
                        }
                        source = Source.BOT;
                    }
                }

                //user-triggered
                Match messageMatch = messageRegex.Match(e.Message.Content);
                if (messageMatch.Success)
                {
                    await Logger.WriteLine("processing user call");
                    Logger.Log(Logging.UserCalls);
                    source = Source.USER;

                    replayLoader.PlayIndex = 0;
                    if (messageMatch.Groups.Count == 4 && messageMatch.Groups[3].Success)
                        replayLoader.PlayIndex = int.Parse(messageMatch.Groups[3].Value) - 1;

                    switch (messageMatch.Groups[1].Value)
                    {
                        case "user-recent":
                        case "user-top":
                            replayLoader.Username = messageMatch.Groups[2].Value;
                            replayLoader.UserScores = messageMatch.Groups[1].Value == "user-recent"? "recent" : "best";
                            break;
                        case "beatmap":
                            var bmMatch = beatmapRegex.Match(messageMatch.Groups[2].Value);
                            if (bmMatch.Success)
                            {
                                replayLoader.BeatmapId = bmMatch.Groups[1].Value;
                            }
                            else
                            {
                                await e.Message.RespondAsync("Invalid beatmap link");
                                return;
                            }
                            break;
                    }
                }

                if (await replayLoader.Load(api, replayDatabase, beatmapDatabase))
                {
                    DiscordMessage message = null;
                    MissAnalyzer missAnalyzer = new MissAnalyzer(replayLoader);
                    if (missAnalyzer.MissCount == 0 && (source == Source.USER || source == Source.ATTACHMENT))
                    {
                        await e.Message.RespondAsync("No misses found.");
                    }
                    else if (missAnalyzer.MissCount == 1)
                    {
                        string miss = await SendMissMessage(missAnalyzer, 0);
                        await e.Message.RespondAsync(miss);
                    }
                    else if (missAnalyzer.MissCount > 1)
                    {
                        message = await e.Message.RespondAsync($"Found **{missAnalyzer.MissCount}** miss{(missAnalyzer.MissCount != 1 ? "es" : "")}");
                        for (int i = 1; i < Math.Min(missAnalyzer.MissCount + 1, numberEmojis.Length); i++)
                        {
                            await message.CreateReactionAsync(numberEmojis[i]);
                        }
                    }
                    if (message != null)
                    {
                        Logger.LogAbsolute(Logging.CachedMessages, cachedMisses.Count);
                        cachedMisses[message] = new SavedMiss(missAnalyzer);
                    }
                }
                else if (source == Source.USER || source == Source.ATTACHMENT)
                {
                    await e.Message.RespondAsync($"Couldn't find {(replayLoader.Replay == null? "replay" : "beatmap")}");
                }
            };

            discord.MessageReactionAdded += async e =>
            {
                if (test && e.Message.Channel.GuildId != 753465280465862757L) return;
                Logger.Log(Logging.EventsHandled);
                if (!e.User.IsCurrent && !e.User.IsBot && cachedMisses.Contains(e.Message))
                {
                    var savedMiss = cachedMisses[e.Message];
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
                        Logger.Log(Logging.ReactionCalls);
                        if (savedMiss.MissUrls[index] == null)
                        {
                            savedMiss.MissUrls[index] = await SendMissMessage(analyzer, index);
                        }
                        await e.Message.ModifyAsync(savedMiss.MissUrls[index]);
                    }
                }
            };

            discord.ClientErrored += async e =>
            {
                await Logger.WriteLine(e.EventName);
                await Logger.WriteLine(e.Exception, Logger.LogLevel.ALERT);
            };

            discord.SocketErrored += async e =>
            {
                await Logger.WriteLine(e.Exception, Logger.LogLevel.ALERT);
            };

            await apiToken;
            await Logger.WriteLine("Init complete");

            await discord.ConnectAsync();
            Logger.LogAbsolute(Logging.ServersJoined, discord.Guilds.Count);
            await discord.UpdateStatusAsync(new DiscordGame(">miss help for help!"));

            byte[] buffer = new byte[4];
            await interruptPipe.Reading.ReadAsync(buffer, 0, 4);
            await discord.DisconnectAsync();
            beatmapDatabase.Close();
            Logger.Instance.Close();
            await Logger.WriteLine("Closed safely");
        }
        private static async Task<bool> CheckApiResult(JToken result, DiscordMessage respondTo)
        {
            if (result == null)
            {
                await respondTo.RespondAsync("Can't find replay on osu! servers - please upload it yourself");
            }
            return result != null;
        }
        private static async Task<string> SendMissMessage(MissAnalyzer analyzer, int index)
        {
            analyzer.CurrentObject = index;
            return (await (await discord.GetChannelAsync(DUMP_CHANNEL)).SendFileAsync(GetStream(analyzer.DrawSelectedHitObject(area)), 
                    "miss.png", "")).Attachments[0].Url;
        }
        private static MemoryStream GetStream(Bitmap bitmap)
        {
            MemoryStream s = new MemoryStream();
            bitmap.Save(s, System.Drawing.Imaging.ImageFormat.Png);
            s.Seek(0, SeekOrigin.Begin);
            return s;
        }
    }
}