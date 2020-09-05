using DSharpPlus;
using System;
using System.Threading.Tasks;
using Mono.Unix;
using System.Threading;
using System.Net;
using OsuMissAnalyzer.Server.Database;
using System.IO;
using OsuMissAnalyzer.Core;
using System.Collections.Generic;
using DSharpPlus.Entities;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using ReplayAPI;

namespace OsuMissAnalyzer.Server
{
    public class Program
    {
        const int size = 480;
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
                while (true)
                {
                    int index = UnixSignal.WaitAny(signals);
                    interruptPipe.Writing.Write(BitConverter.GetBytes(index), 0, 4);
                }
            });
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        public enum Source { USER, OWO, ATTACHMENT }
        static async Task MainAsync(string[] args)
        {
            OsuApi api = new OsuApi();
            var beatmapDatabase = new ServerBeatmapDb(api, "beatmaps");
            var replayDatabase = new ServerReplayDb(api, "replays");
            string pfpPrefix = "https://a.ppy.sh/";
            Regex messageRegex = new Regex("^>miss (user-recent|user-top|beatmap) (.+?)(?: (\\d+))?$");
            Source source = Source.USER;
            Dictionary<DiscordMessage, MissAnalyzer> cachedMisses = new Dictionary<DiscordMessage, MissAnalyzer>();
            Dictionary<Replay, MissAnalyzer> cachedReplays = new Dictionary<Replay, MissAnalyzer>();
            string[] numbers = {"zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"};
            DiscordEmoji[] numberEmojis = new DiscordEmoji[10];
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = File.ReadAllText("token.dat"),
                TokenType = TokenType.Bot
            });
            for (int i = 0; i < 10; i++)
            {
                numberEmojis[i] = DiscordEmoji.FromName(discord, $":{numbers[i]}:");
            }
            discord.MessageCreated += async e =>
            {
                List<Bitmap> misses = null;
                foreach (var attachment in e.Message.Attachments)
                {
                    if (attachment.FileName.EndsWith(".osr"))
                    {
                        using (WebClient w = new WebClient())
                        {
                            w.DownloadFile(attachment.Url, attachment.FileName);
                        }
                        misses = GetMisses(new ServerReplayLoader(attachment.FileName, beatmapDatabase));
                        source  = Source.ATTACHMENT;
                    }
                }
                //owo
                if (e.Author.Id == 289066747443675143 && e.Message.Content.StartsWith("**Most Recent osu! Standard Play for"))
                {
                    string url = e.Message.Embeds[0].Author.IconUrl.ToString();
                    if (url.StartsWith(pfpPrefix))
                    {
                        var data = api.GetUserRecentv2(url.Substring(pfpPrefix.Length), 1);
                        misses = GetMisses(new ServerReplayLoader(data.Item1, data.Item2, beatmapDatabase));
                        source = Source.OWO;
                    }
                }
                //user-triggered
                Match m = messageRegex.Match(e.Message.Content);
                if (m.Success)
                {
                    int playIndex;
                    ReplayLoader loader = null;
                    if (m.Groups.Count == 4) playIndex = int.Parse(m.Groups[3].Value);
                    switch (m.Groups[1].Value)
                    {
                        case "user-recent":
                            var recent = api.GetUserRecentv2(api.GetUserIdv1(m.Groups[2].Value), m.Groups.Count < 3? 1 : int.Parse(m.Groups[2].Value));
                            loader = new ServerReplayLoader(replayDatabase.GetReplayFromOnlineId(recent.Item1), beatmapDatabase.GetBeatmapFromId(recent.Item2));
                            break;
                        case "user-top":
                            break;
                        case "beatmap":
                            break;
                    }
                    if (loader != null)
                    {
                        misses = GetMisses(loader);
                        source = Source.USER;
                    }
                }
                if (misses != null)
                {
                    DiscordMessage message = null;
                    if (misses.Count == 0 && source != Source.OWO)
                    {
                        await e.Message.RespondAsync("No misses found.");
                    }
                    else if (misses.Count == 1)
                    {
                        message = await e.Message.RespondWithFileAsync(GetStream(misses[0]), "miss.png", "**Miss 1 of 1**");
                    }
                    else
                    {
                        message = await e.Message.RespondAsync($"**Found {misses.Count} misses**");
                        for (int i = 1; i <= misses.Count; i++)
                        {
                            await message.CreateReactionAsync(numberEmojis[i]);
                        }
                    }
                    if (message != null)
                    {
                        cachedMisses[message] = misses;
                    }
                }
            };

            discord.MessageReactionAdded += async e =>
            {
                if (cachedMisses.ContainsKey(e.Message))
                {
                    e.Emoji
                }
            };

            await discord.ConnectAsync();
            byte[] buffer = new byte[4];
            await interruptPipe.Reading.ReadAsync(buffer, 0, 4);
            await discord.DisconnectAsync();
            beatmapDatabase.Close();
        }
        private static List<Bitmap> GetMisses(ReplayLoader loader)
        {
            return new MissAnalyzer(loader).DrawAllMisses(new Rectangle(0, 0, size, size)).ToList();
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