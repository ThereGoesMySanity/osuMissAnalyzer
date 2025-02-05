using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BMAPI.v1;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.Server;
using OsuMissAnalyzer.Server.Database;
using OsuMissAnalyzer.Server.Logging;
using OsuMissAnalyzer.Server.Settings;
using ReplayAPI;
using SixLabors.ImageSharp;

namespace OsuMissAnalyzer.Tests
{
    [TestFixture]
    public class ServerTests
    {
        private string root = ProjectSourcePath.Value;
        private OsuApi api => serviceProvider.GetRequiredService<OsuApi>();
        private ServerReplayDb replays => serviceProvider.GetRequiredService<ServerReplayDb>();
        private ServerBeatmapDb beatmaps => serviceProvider.GetRequiredService<ServerBeatmapDb>();
        private ServiceProvider serviceProvider;

        [OneTimeSetUp]
        public void Init()
        {
            string[] keys = File.ReadAllLines("Resources/keys.dat");
            var services = new ServiceCollection();
            IConfiguration configurationRoot = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();
            services.AddSingleton<IConfiguration>(configurationRoot);
            services.Configure<ServerOptions>(configurationRoot.GetRequiredSection(nameof(ServerOptions)));
            services.Configure<DiscordOptions>(configurationRoot.GetRequiredSection(nameof(DiscordOptions)));
            services.Configure<OsuApiOptions>(configurationRoot.GetRequiredSection(nameof(OsuApiOptions)));
            services.AddSingleton<IDataLogger, TestLogger>();
            services.AddSingleton<OsuApi>();
            services.AddSingleton<ServerBeatmapDb>();
            services.AddSingleton<ServerReplayDb>();
            services.AddSingleton<RequestContext, TestRequestContext>();
            services.AddHttpClient();

            serviceProvider = services.BuildServiceProvider();
        }

        [OneTimeTearDown]
        public void Close()
        {
            serviceProvider.Dispose();
        }

        [TestCase("d41d8cd98f00b204e9800998ecf8427e")]
        public async Task GetBeatmap(string beatmapHash)
        {
            Beatmap b = await beatmaps.GetBeatmap(beatmapHash);
        }

        [TestCase(3243485950UL, "replay-osu_1859001_3243485950.osr")]
        public async Task TestApiDownload(ulong scoreId, string compareFile)
        {
            Replay compare = new Replay($"Resources/{compareFile}");
            Beatmap b = await beatmaps.GetBeatmap(compare.MapHash);
            if (File.Exists(Path.Combine(root, "serverdata", $"{scoreId}.osr")))
            {
                File.Delete(Path.Combine(root, "serverdata", $"{scoreId}.osr"));
            }
            Replay r = await replays.GetReplayFromOnlineId(scoreId);
            Assert.That(r.ReplayFrames, Is.EqualTo(compare.ReplayFrames));
            Assert.That(r.Mods, Is.EqualTo(compare.Mods));
            Assert.That(r.OnlineId, Is.EqualTo(compare.OnlineId));
        }

        [TestCase("3205642-old?.osu","3205642.osu","3205642","replay-osu_3205642_3960657933.osr")]
        public async Task TestRedownload(string oldFile, string copyTo, string id, string replay)
        {
            File.Copy(Path.Combine("Resources", oldFile), Path.Combine(root, "serverdata", "beatmaps", copyTo), true);
            Replay r = new Replay(Path.Combine("Resources", replay));
            Beatmap old = await beatmaps.GetBeatmapFromId(id);
            Beatmap newBeatmap = await beatmaps.GetBeatmapFromId(id, true);
            Assert.That(old.BeatmapHash, Is.Not.EqualTo(newBeatmap.BeatmapHash));
        }

        [TestCase("3243485950")]
        public async Task TestApiv2(string scoreId)
        {
            JToken s = await api.GetApiv2Json($"scores/osu/{scoreId}");
            Console.WriteLine(s);
        }

        [TestCase("312b50442dd47de159257dfac2c8da50-133009249532585046.osr")]
        [TestCase("replay-osu_1695980_4012554317.osr")]
        [TestCase("3489388060.osr")]
        [TestCase("3534866519.osr")]
        public async Task TestLoadBeatmap(string replayFile)
        {
            Replay r = new Replay(Path.Combine("Resources", replayFile));
            Beatmap b = await beatmaps.GetBeatmap(r.MapHash);
            Console.WriteLine(b.BeatmapID);
        }
        [TestCase(2283307549UL)]
        [TestCase(2040036498UL)]
        public async Task TestReplayLoaderByScore(ulong scoreId)
        {
            ServerReplayLoader replayLoader = ActivatorUtilities.CreateInstance<ServerReplayLoader>(serviceProvider);
            replayLoader.ScoreId = scoreId;
            await TestAnalyzer(replayLoader);
        }
        [TestCase("3534866519.osr")]
        [TestCase("MyAngelAku_-_kakichoco_-_Zanei_Illusion_2022-06-09_Osu.osr")]
        public async Task TestReplayLoaderByFile(string file)
        {
            ServerReplayLoader replayLoader = ActivatorUtilities.CreateInstance<ServerReplayLoader>(serviceProvider);
            replayLoader.ReplayFile = Path.Combine("Resources", file);
            await TestAnalyzer(replayLoader);
        }
        private async Task TestAnalyzer(ServerReplayLoader replayLoader)
        {
            Assert.That(await replayLoader.Load(), Is.Null);
            Assert.That(replayLoader.Loaded, Is.True);
            var analyzer = new MissAnalyzer(replayLoader);
            var images = analyzer.DrawAllMisses(new Rectangle(0, 0, 480, 480));
            int i = 0;
            foreach(var image in images)
            {
                string filename = Path.Combine(root, "out", $"{replayLoader}.{i++}.png");
                await image.SaveAsPngAsync(filename);
            }
        }
    }
}