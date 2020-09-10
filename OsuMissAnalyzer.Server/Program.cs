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
        const int size = 480;
        const string HELP_MESSAGE = @"[osu! Miss Analyzer](https://github.com/ThereGoesMySanity/osuMissAnalyzer) bot
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
                Logger.WriteLine(botLink);
                return;
            }

            if (help || (osuSecret.Length * osuApiKey.Length * discordToken.Length) == 0)
            {
                Logger.WriteLine(
$@"osu! Miss Analyzer, Discord Bot Edition
Bot link: https://discordapp.com/oauth2/authorize?client_id={discordId}&scope=bot&permissions={discordPermissions}");
                opts.WriteOptionDescriptions(Console.Out);
                return;
            }
            if (apiv2Req != null)
            {
                OsuApi api2 = new OsuApi(osuId, osuSecret, osuApiKey);
                Logger.WriteLine(await api2.GetApiv2(apiv2Req));
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

            var cachedMisses = new MemoryCache<DiscordMessage, MissAnalyzer>(128);
            cachedMisses.SetPolicy(typeof(LfuEvictionPolicy<,>));

            var rsTypes = new Dictionary<string, ulong> {
                [">rs"] = OWO,
                [">recent"] = OWO,
                ["%rs"] = BOATBOT,
                ["!!rs"] = BISMARCK,
            };
            var rsCalls = new Dictionary<ulong, Queue<DiscordChannel>>
            {
                [OWO] = new Queue<DiscordChannel>(),
                [BOATBOT] = new Queue<DiscordChannel>(),
                [BISMARCK] = new Queue<DiscordChannel>(),
            };
            var rsFunc = new Dictionary<ulong, Func<ServerReplayLoader, MessageCreateEventArgs, DiscordEmbed>>
            {
                [OWO] = (replayLoader, e) =>
                {
                    if (e.Message.Content.StartsWith("**Most Recent osu! Standard Play for"))
                    {
                        Logger.WriteLine("processing owo message");
                        return e.Message.Embeds[0];
                    }
                    return null;
                },
                [BISMARCK] = (replayLoader, e) =>
                {
                    if (e.Message.Content.Length == 0)
                    {
                        Logger.WriteLine("processing bismarck message");
                        var embed = e.Message.Embeds[0];
                        string url = embed.Url.AbsoluteUri;
                        string prefix = "https://osu.ppy.sh/scores/osu/";
                        string mapPrefix = "https://osu.ppy.sh/beatmapsets/";
                        if (url.StartsWith(prefix) && embed.Description.Contains(mapPrefix))
                        {
                            replayLoader.ScoreId = url.Substring(prefix.Length);
                            string urlEnd = embed.Description.Substring(embed.Description.IndexOf(mapPrefix) + mapPrefix.Length);
                            var match = partialBeatmapRegex.Match(urlEnd);
                            var modMatch = modRegex.Match(urlEnd);
                            if(match.Success && modMatch.Success)
                            {
                                replayLoader.BeatmapId = match.Groups[1].Value;
                                replayLoader.Mods = modMatch.Groups[1].Value;
                                return null;
                            }
                        }
                        return embed;
                    }
                    return null;
                },
                [BOATBOT] = (replayLoader, e) =>
                {
                    Logger.WriteLine("processing boatbot message");
                    return e.Message.Embeds[0];
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
                if (e.Message.Content.StartsWith(">miss help") || 
                    (e.Message.Channel.IsPrivate && e.Message.Content.IndexOf("help", StringComparison.InvariantCultureIgnoreCase) >= 0))
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
                        Logger.WriteLine("processing attachment");
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

                if (rsTypes.ContainsKey(e.Message.Content.Split(' ')[0]))
                {
                    rsCalls[rsTypes[e.Message.Content.Split(' ')[0]]].Enqueue(e.Message.Channel);
                    return;
                }
                if (rsCalls.ContainsKey(e.Author.Id) && rsCalls[e.Author.Id].Count > 0 && rsCalls[e.Author.Id].Peek() == e.Channel)
                {
                    DiscordEmbed embed = rsFunc[e.Author.Id](replayLoader, e);
                    Logger.Log(Logging.BotCalls);
                    if (embed != null)
                    {
                        string url = embed.Author.IconUrl.ToString();
                        if (url.StartsWith(pfpPrefix))
                        {
                            replayLoader.UserId = url.Substring(pfpPrefix.Length).Split('?')[0];
                            replayLoader.UserScores = "recent";
                            replayLoader.PlayIndex = 0;
                        }
                    }
                    source = Source.BOT;
                }

                //user-triggered
                Match messageMatch = messageRegex.Match(e.Message.Content);
                if (messageMatch.Success)
                {
                    Logger.WriteLine("processing user call");
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
                        message = await SendMissMessage(missAnalyzer, e.Message, 0);
                    }
                    else
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
                        cachedMisses[message] = missAnalyzer;
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
                if (!e.User.IsCurrent && cachedMisses.Contains(e.Message))
                {
                    var analyzer = cachedMisses[e.Message];
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
                        var message = await SendMissMessage(analyzer, e.Message, index);
                        Logger.LogAbsolute(Logging.CachedMessages, cachedMisses.Count);
                        cachedMisses[message] = analyzer;
                    }
                }
            };

            discord.ClientErrored += async e =>
            {
                Logger.WriteLine(e.EventName);
                Logger.WriteLine(e.Exception);
            };

            await apiToken;
            Logger.WriteLine("Init complete");

            await discord.ConnectAsync();
            Logger.LogAbsolute(Logging.ServersJoined, discord.Guilds.Count);

            byte[] buffer = new byte[4];
            await interruptPipe.Reading.ReadAsync(buffer, 0, 4);
            await discord.DisconnectAsync();
            beatmapDatabase.Close();
            Logger.Instance.Close();
            Logger.WriteLine("Closed safely");
        }
        private static async Task<bool> CheckApiResult(JToken result, DiscordMessage respondTo)
        {
            if (result == null)
            {
                await respondTo.RespondAsync("Can't find replay on osu! servers - please upload it yourself");
            }
            return result != null;
        }
        private static async Task<DiscordMessage> SendMissMessage(MissAnalyzer analyzer, DiscordMessage respondTo, int index)
        {
            analyzer.CurrentObject = index;
            return await respondTo.RespondWithFileAsync(GetStream(analyzer.DrawSelectedHitObject(area)), "miss.png", $"Miss **{index + 1}** of **{analyzer.MissCount}**");
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