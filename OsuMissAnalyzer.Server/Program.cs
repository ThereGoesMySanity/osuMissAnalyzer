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

namespace OsuMissAnalyzer.Server
{
    public class Program
    {
        const int size = 480;
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
        public enum Source { USER, OWO, ATTACHMENT }
        static async Task MainAsync(string[] args)
        {
            string serverDir = "";
            string osuId = "2558";
            string osuSecret = "";
            string osuApiKey = "";
            string discordToken = "";
            string discordId = "752035690237394944";
            string discordPermissions = "100416";
            bool help = false, link = false;
            var opts = new OptionSet() {
                {"d|dir=", "Set server storage dir (default: ./)", b => serverDir = b},
                {"s|secret=", "Set client secret (osu!) (required)", s => osuSecret = s},
                {"k|key=", "osu! api v1 key (required)", k => osuApiKey = k},
                {"id=", "osu! client id (default: mine)", id => osuId = id},
                {"t|token=", "discord bot token (required)", t => discordToken = t},
                {"h|help", "displays help", a => help = a != null},
                {"l|link", "displays bot link and exits", l => link = l != null}
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

            Logger.Instance = new Logger(Path.Combine(serverDir, "log.csv"));
            OsuApi api = new OsuApi(osuId, osuSecret, osuApiKey);
            var apiToken = api.RefreshToken();
            Directory.CreateDirectory(Path.Combine(serverDir, "beatmaps"));
            Directory.CreateDirectory(Path.Combine(serverDir, "replays"));
            var beatmapDatabase = new ServerBeatmapDb(api, serverDir);
            var replayDatabase = new ServerReplayDb(api, serverDir);
            string pfpPrefix = "https://a.ppy.sh/";
            Regex messageRegex = new Regex("^>miss (user-recent|user-top|beatmap) (.+?)(?: (\\d+))?$");
            Regex beatmapRegex = new Regex("^(?:https?://(?:osu|old).ppy.sh/(?:beatmapsets/\\d+#osu|b)/)?(\\d+)");
            Source source = Source.USER;

            var cachedMisses = new MemoryCache<DiscordMessage, MissAnalyzer>(128);
            cachedMisses.SetPolicy(typeof(LfuEvictionPolicy<,>));
            // Dictionary<DiscordMessage, MissAnalyzer> cachedMisses = new Dictionary<DiscordMessage, MissAnalyzer>();
            string[] numbers = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
            DiscordEmoji[] numberEmojis = new DiscordEmoji[10];
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = discordToken,
                TokenType = TokenType.Bot
            });
            for (int i = 0; i < 10; i++)
            {
                numberEmojis[i] = DiscordEmoji.FromName(discord, $":{numbers[i]}:");
            }
            discord.MessageCreated += async e =>
            {
                try
                {
                    Logger.LogAbsolute(Logging.ServersJoined, discord.Guilds.Count);
                    Logger.Log(Logging.EventsHandled);
                    MissAnalyzer missAnalyzer = null;
                    //attachment
                    foreach (var attachment in e.Message.Attachments)
                    {
                        if (attachment.FileName.EndsWith(".osr"))
                        {
                            Console.WriteLine("processing attachment");
                            Logger.Log(Logging.AttachmentCalls);
                            string dest = Path.Combine(serverDir, "replays", attachment.FileName);
                            using (WebClient w = new WebClient())
                            {
                                w.DownloadFile(attachment.Url, dest);
                            }
                            var replay = new Replay(dest);
                            missAnalyzer = new MissAnalyzer(replay, await beatmapDatabase.GetBeatmap(replay.MapHash));
                            source = Source.ATTACHMENT;
                        }
                    }
                    //owo
                    if (e.Author.Id == 289066747443675143 && e.Message.Content.StartsWith("**Most Recent osu! Standard Play for"))
                    {
                        Console.WriteLine("processing owo message");
                        Logger.Log(Logging.OwoCalls);
                        string url = e.Message.Embeds[0].Author.IconUrl.ToString();
                        if (url.StartsWith(pfpPrefix))
                        {
                            var data = await api.GetUserScoresv2(url.Substring(pfpPrefix.Length).Split('?')[0], "recent", 0);
                            if (data != null)
                            {
                                var beatmap = await beatmapDatabase.GetBeatmapFromId((string)data["beatmap"]["id"]);
                                missAnalyzer = new MissAnalyzer(await replayDatabase.GetReplayFromOnlineId(data, beatmap), beatmap);
                                source = Source.OWO;
                            }
                        }
                    }
                    //user-triggered
                    Match messageMatch = messageRegex.Match(e.Message.Content);
                    if (messageMatch.Success)
                    {
                        Console.WriteLine("processing user call");
                        Logger.Log(Logging.UserCalls);
                        int playIndex = 0;
                        Task<JToken> scoreTask = null;
                        if (messageMatch.Groups.Count == 4 && messageMatch.Groups[3].Success) playIndex = int.Parse(messageMatch.Groups[3].Value) - 1;
                        switch (messageMatch.Groups[1].Value)
                        {
                            case "user-recent":
                                scoreTask = api.GetUserScoresv2(await api.GetUserIdv1(messageMatch.Groups[2].Value), "recent", playIndex);
                                break;
                            case "user-top":
                                scoreTask = api.GetUserScoresv2(await api.GetUserIdv1(messageMatch.Groups[2].Value), "best", playIndex);
                                break;
                            case "beatmap":
                                var bmMatch = beatmapRegex.Match(messageMatch.Groups[2].Value);
                                if (bmMatch.Success)
                                {
                                    scoreTask = api.GetBeatmapScoresv2(bmMatch.Groups[1].Value, playIndex);
                                }
                                else
                                {
                                    await e.Message.RespondAsync("Invalid beatmap link");
                                }
                                break;
                        }
                        if (scoreTask != null)
                        {
                            var score = await scoreTask; 
                            if (await CheckApiResult(score, e.Message))
                            {
                                var beatmap = await beatmapDatabase.GetBeatmapFromId((string)score["beatmap"]["id"]);
                                missAnalyzer = new MissAnalyzer(await replayDatabase.GetReplayFromOnlineId(score, beatmap), beatmap);
                                source = Source.USER;
                            }
                        }
                    }
                    if (missAnalyzer != null)
                    {
                        DiscordMessage message = null;
                        if (missAnalyzer.MissCount == 0 && source != Source.OWO)
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
                }
                catch (Exception exc) { Console.WriteLine(exc.ToString()); }
            };

            discord.MessageReactionAdded += async e =>
            {
                try
                {
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
                }
                catch (Exception exc) { Console.WriteLine(exc.ToString()); }
            };

            await apiToken;
            Console.WriteLine("Init complete");

            await discord.ConnectAsync();
            Logger.LogAbsolute(Logging.ServersJoined, discord.Guilds.Count);

            byte[] buffer = new byte[4];
            await interruptPipe.Reading.ReadAsync(buffer, 0, 4);
            await discord.DisconnectAsync();
            beatmapDatabase.Close();
            Console.WriteLine("Closed safely");
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