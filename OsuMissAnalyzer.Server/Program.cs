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
        static async Task MainAsync(string[] args)
        {
            var database = new ServerBeatmapDb("beatmaps");
            OsuApi api = new OsuApi();
            string pfpPrefix = "https://a.ppy.sh/";
            Dictionary<DiscordUser, List<Bitmap>> cachedMisses = new Dictionary<DiscordUser, List<Bitmap>>();
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = File.ReadAllText("token.dat"),
                TokenType = TokenType.Bot
            });
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
                        misses = GetMisses(new ServerReplayLoader(attachment.FileName, database));
                    }
                }
                //owo
                if (e.Author.Id == 289066747443675143 && e.Message.Content.StartsWith("**Most Recent osu! Standard Play for"))
                {
                    string url = e.Message.Embeds[0].Author.IconUrl.ToString();
                    if (url.StartsWith(pfpPrefix))
                    {
                        var data = api.GetUserRecent(url.Substring(pfpPrefix.Length), 1);
                        misses = GetMisses(new ServerReplayLoader(data.Item1, data.Item2, database));
                    }
                }
            };

            await discord.ConnectAsync();
            byte[] buffer = new byte[4];
            await interruptPipe.Reading.ReadAsync(buffer, 0, 4);
            await discord.DisconnectAsync();
            database.Close();
        }
        private static List<Bitmap> GetMisses(ReplayLoader loader)
        {
            return new MissAnalyzer(loader).DrawAllMisses(new Rectangle(0, 0, size, size)).ToList();
        }
    }
}