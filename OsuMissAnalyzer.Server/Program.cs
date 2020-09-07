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
            Thread signalThread = new Thread(delegate () {
                int index = UnixSignal.WaitAny(signals);
                interruptPipe.Writing.Write(BitConverter.GetBytes(index), 0, 4);
            });
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

            OsuApi api = new OsuApi(osuId, osuSecret, osuApiKey);
            Directory.CreateDirectory(Path.Combine(serverDir, "beatmaps"));
            Directory.CreateDirectory(Path.Combine(serverDir, "replays"));
            var beatmapDatabase = new ServerBeatmapDb(api, serverDir);
            var replayDatabase = new ServerReplayDb(api, serverDir);
            string pfpPrefix = "https://a.ppy.sh/";
            Regex messageRegex = new Regex("^>miss (user-recent|user-top|beatmap) (.+?)(?: (\\d+))?$");
            Regex beatmapRegex = new Regex("^(https?://(?:osu|old).ppy.sh/(?:beatmapsets/\\d+#osu|b)/)?(\\d+)");
            Source source = Source.USER;

            var cachedMisses = new MemoryCache<DiscordMessage, MissAnalyzer>(128);
            cachedMisses.SetPolicy(typeof(LfuEvictionPolicy<,>));
            // Dictionary<DiscordMessage, MissAnalyzer> cachedMisses = new Dictionary<DiscordMessage, MissAnalyzer>();
            string[] numbers = {"zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"};
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
                MissAnalyzer missAnalyzer = null;
                foreach (var attachment in e.Message.Attachments)
                {
                    if (attachment.FileName.EndsWith(".osr"))
                    {
                        string dest = Path.Combine(serverDir, "replays", attachment.FileName);
                        using (WebClient w = new WebClient())
                        {
                            w.DownloadFile(attachment.Url, dest);
                        }
                        missAnalyzer = new MissAnalyzer(new ServerReplayLoader(new Replay(dest), beatmapDatabase));
                        source  = Source.ATTACHMENT;
                    }
                }
                /*
                 * Can't do any of these until I figure out how to actually obtain a replay file
                 */
                //owo
                if (e.Author.Id == 289066747443675143 && e.Message.Content.StartsWith("**Most Recent osu! Standard Play for"))
                {
                    Console.WriteLine("owo");
                    string url = e.Message.Embeds[0].Author.IconUrl.ToString();
                    if (url.StartsWith(pfpPrefix))
                    {
                        var data = api.GetUserScoresv2(url.Substring(pfpPrefix.Length), "recent", 0);
                        missAnalyzer = new MissAnalyzer(new ServerReplayLoader(data, replayDatabase, beatmapDatabase));
                        source = Source.OWO;
                    }
                }
                //user-triggered
                Match m = messageRegex.Match(e.Message.Content);
                if (m.Success)
                {
                    Console.WriteLine(">miss");
                    int playIndex = 0;
                    IReplayLoader loader = null;
                    if (m.Groups.Count == 4 && m.Groups[3].Success) playIndex = int.Parse(m.Groups[3].Value) - 1;
                    Console.WriteLine(m.Groups[1].Value);
                    switch (m.Groups[1].Value)
                    {
                        case "user-recent":
                            var recent = api.GetUserScoresv2(api.GetUserIdv1(m.Groups[2].Value), "recent", playIndex);
                            if (await CheckApiResult(recent, e.Message))
                            {
                                Console.WriteLine(m.Groups[2].Value);
                                Console.WriteLine(recent.ToString());
                                loader = new ServerReplayLoader(recent, replayDatabase, beatmapDatabase);
                                Console.WriteLine(">recent");
                            }
                            break;
                        case "user-top":
                            var top = api.GetUserScoresv2(api.GetUserIdv1(m.Groups[2].Value), "best", playIndex);
                            if (await CheckApiResult(top, e.Message))
                            {
                                loader = new ServerReplayLoader(top, replayDatabase, beatmapDatabase);
                            }
                            break;
                        case "beatmap":
                            var match = beatmapRegex.Match(m.Groups[2].Value);
                            if (match.Success)
                            {
                                var bmTop = api.GetBeatmapScoresv2(match.Groups[1].Value, playIndex);
                                if (await CheckApiResult(bmTop, e.Message))
                                {
                                    loader = new ServerReplayLoader(bmTop, replayDatabase, beatmapDatabase);
                                }
                            }
                            else
                            {
                                await e.Message.RespondAsync("Invalid beatmap link");
                            }
                            break;
                    }
                    if (loader != null)
                    {
                        missAnalyzer = new MissAnalyzer(loader);
                        source = Source.USER;
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
                        message = await e.Message.RespondAsync($"Found **{missAnalyzer.MissCount}** miss{(missAnalyzer.MissCount != 1?"es":"")}");
                        for (int i = 1; i < Math.Min(missAnalyzer.MissCount + 1, numberEmojis.Length); i++)
                        {
                            await message.CreateReactionAsync(numberEmojis[i]);
                        }
                    }
                    if (message != null)
                    {
                        cachedMisses[message] = missAnalyzer;
                    }
                }
            };

            discord.MessageReactionAdded += async e =>
            {
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
                    if (index >= 0 && index < analyzer.MissCount)
                    {
                        var message = await SendMissMessage(analyzer, e.Message, index);
                        cachedMisses[message] = analyzer;
                    }
                }
            };
            
            Console.WriteLine("Init complete");

            await discord.ConnectAsync();
            byte[] buffer = new byte[4];
            await interruptPipe.Reading.ReadAsync(buffer, 0, 4);
            await discord.DisconnectAsync();
            beatmapDatabase.Close();
            Console.WriteLine("Closed safely");
        }
        private static async Task<bool> CheckApiResult(JToken result, DiscordMessage respondTo)
        {
            if(result == null)
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