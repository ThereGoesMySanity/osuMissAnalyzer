using System.IO;
using System.Threading.Tasks;
using BMAPI.v1;
using NUnit.Framework;
using ReplayAPI;

namespace OsuMissAnalyzer.Tests
{
    [TestFixture]
    public class ServerTests
    {
        private OsuApi api;
        private ServerReplayDb replays;
        private ServerBeatmapDb beatmaps;
        [OneTimeSetUp]
        public void Init()
        {
            string[] keys = File.ReadAllLines("Resources/keys.dat");
            api = new OsuApi("2558", keys[0], keys[1]);
            replays = new ServerReplayDb(api, "serverdata");
            beatmaps = new ServerBeatmapDb(api, "serverdata");
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
            Assert.AreEqual(compare.MapHash, r.MapHash);
            Assert.AreEqual(compare.Mods, r.Mods);
            Assert.AreEqual(compare.OnlineId, r.OnlineId);
        }
    }
}