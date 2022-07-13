using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BMAPI.v1;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.Server;
using OsuMissAnalyzer.Server.Database;
using ReplayAPI;
using SixLabors.ImageSharp;

namespace OsuMissAnalyzer.Tests
{
    [TestFixture]
    public class ServerTests
    {
        private string root = ProjectSourcePath.Value;
        private HttpClient client;
        private OsuApi api;
        private ServerReplayDb replays;
        private ServerBeatmapDb beatmaps;
        [OneTimeSetUp]
        public void Init()
        {
            string[] keys = File.ReadAllLines("Resources/keys.dat");
            client = new HttpClient();
            Logger.Instance = new TestLogger();
            api = new OsuApi(client, "2558", keys[0], keys[1]);
            replays = new ServerReplayDb(api, Path.Combine(root, "serverdata"));
            beatmaps = new ServerBeatmapDb(api, Path.Combine(root, "serverdata"), true);
        }

        [OneTimeTearDown]
        public void Close()
        {
            beatmaps.Close();
        }

        [TestCase("d41d8cd98f00b204e9800998ecf8427e")]
        public async Task GetBeatmap(string beatmapHash)
        {
            Beatmap b = await beatmaps.GetBeatmap(beatmapHash);
        }

        [TestCase("3243485950", "replay-osu_1859001_3243485950.osr")]
        public async Task TestApiDownload(string scoreId, string compareFile)
        {
            Replay compare = new Replay($"Resources/{compareFile}");
            Beatmap b = await beatmaps.GetBeatmap(compare.MapHash);
            if (File.Exists(Path.Combine("serverdata", $"{scoreId}.osr")))
            {
                File.Delete(Path.Combine("serverdata", $"{scoreId}.osr"));
            }
            Replay r = await replays.GetReplayFromOnlineId(scoreId, compare.Mods.ModsToString(), b);
            CollectionAssert.AreEqual(compare.ReplayFrames, r.ReplayFrames);
            Assert.AreEqual(compare.Mods, r.Mods);
            Assert.AreEqual(compare.OnlineId, r.OnlineId);
        }

        [TestCase("3205642-old?.osu","3205642.osu","3205642","replay-osu_3205642_3960657933.osr")]
        public async Task TestRedownload(string oldFile, string copyTo, string id, string replay)
        {
            File.Copy(Path.Combine("Resources", oldFile), Path.Combine("serverdata", "beatmaps", copyTo), true);
            Replay r = new Replay(Path.Combine("Resources", replay));
            Beatmap old = await beatmaps.GetBeatmapFromId(id);
            Beatmap newBeatmap = await beatmaps.GetBeatmapFromId(id, true);
            Assert.AreNotEqual(old.BeatmapHash, newBeatmap.BeatmapHash);
        }

        [TestCase("3243485950")]
        public async Task TestApiv2(string scoreId)
        {
            JToken s = await api.GetApiv2($"scores/osu/{scoreId}/download");
            Console.WriteLine(s);
        }

        [TestCase("312b50442dd47de159257dfac2c8da50-133009249532585046.osr")]
        public async Task TestLoadBeatmap(string replayFile)
        {
            Replay r = new Replay(Path.Combine("Resources", replayFile));
            Beatmap b = await beatmaps.GetBeatmap(r.MapHash);
        }
        [TestCase("2283307549")]
        [TestCase("2040036498")]
        public async Task TestReplayLoaderByScore(string scoreId)
        {
            ServerReplayLoader replayLoader = new ServerReplayLoader();
            replayLoader.ScoreId = scoreId;
            Assert.IsNull(await replayLoader.Load(api, replays, beatmaps));
            Assert.True(replayLoader.Loaded);
            var analyzer = new MissAnalyzer(replayLoader);
            var images = analyzer.DrawAllMisses(new SixLabors.ImageSharp.Rectangle(0, 0, 480, 480));
            int i = 0;
            foreach(var image in images)
            {
                string filename = Path.Combine(root, "out", $"{scoreId}.{i++}.png");
                await image.SaveAsPngAsync(filename);
            }
        }
    }
}