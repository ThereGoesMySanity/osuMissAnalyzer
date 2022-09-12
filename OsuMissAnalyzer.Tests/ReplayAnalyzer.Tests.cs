using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using BMAPI.v1;
using BMAPI.v1.HitObjects;
using NUnit.Framework;
using OsuMissAnalyzer.Core;
using ReplayAPI;

namespace OsuMissAnalyzer.Tests
{
    [TestFixture]
    public class ReplayAnalyzerTests
    {
        private OsuDbAPI.OsuDbFile db;
        [OneTimeSetUp]
        public void setup()
        {
            db = new OsuDbAPI.OsuDbFile("/home/will/osu!/osu!.db", byHash: true, byId: true);
        }
        // [Test]
        // public void TestOsuDb()
        // {
        //     CollectionAssert.AllItemsAreNotNull(db.Beatmaps.Select(b => b.Hash));
        //     Assert.True(db.Beatmaps.All(b => db.BeatmapsByHash.ContainsKey(b.Hash)));
        //     Console.WriteLine(db.BeatmapsByHash.Count);
        // }
        [TestCase("Resources/scores.db")]
        public void TestScoresDb(string file)
        {
            ScoresDb db = null;
            Assert.DoesNotThrow(() => db = new ScoresDb(file));
            foreach (string name in db.scores.Values.SelectMany(s => s).Select(s => s.playerName).ToHashSet())
            {
                Console.WriteLine(name);
            }
        }
        [Test]
        public void TestSlider()
        {
            Beatmap b = db.BeatmapsById[539697].Load("/home/will/A/osu!/Songs");
            Replay r = new Replay("/home/will/osu!/Data/r/20b064985202e1a5219432c774476b8b-132562134499563000.osr");
            MissAnalyzer analyzer = new MissAnalyzer(r, b);
            SliderObject s = (SliderObject)b.HitObjects[465];
            Console.WriteLine(s);
            Console.WriteLine(string.Join(", ", s.Curves));
            Console.WriteLine(string.Join(", ", s.Points));
            Console.WriteLine(string.Join(", ", s.Curves.SelectMany(curve => curve.CurveSnapshots)));

        }
        //127002 to 127641
        [TestCase("Resources/3489388060.osr", 0, "Resources/1632673.osu")]
        //15246
        [TestCase("Resources/3534866519.osr", 0, "Resources/64780.osu")]
        [TestCase("Resources/replay-osu_151229_2646617863.osr", 0)]
        [TestCase("Resources/replay-osu_1695980_4012554317.osr", 0, "Resources/1695980.osu")]
        public void TestStacking(string replayFile, int missCount, string beatmapFile = null)
        {
            Replay r = new Replay(replayFile);
            Beatmap b = beatmapFile != null ? new Beatmap(beatmapFile) : db.BeatmapsByHash[r.MapHash].Load("/home/will/A/osu!/Songs");
            MissAnalyzer analyzer = new MissAnalyzer(r, b);
            Assert.AreEqual(missCount, analyzer.MissCount);
            // foreach(var m in analyzer.DrawAllMisses(new System.Drawing.Rectangle(0, 0, 320, 320)))
            //     m.Save($"miss{Path.GetFileName(replayFile)}{i++}.png", ImageFormat.Png);
        }

        [TestCase("Resources/Sinaeb - Hanazawa Kana - Masquerade [Hard] (2022-08-23) Osu.osr", 1, 
                    "Resources/Hanazawa Kana - Masquerade (Scorpiour) [Hard].osu")]
        public void TestEarlyHit(string replayFile, int missCount, string beatmapFile = null)
        {
            Replay r = new Replay(replayFile);
            Beatmap b = beatmapFile != null ? new Beatmap(beatmapFile) : db.BeatmapsByHash[r.MapHash].Load("/home/will/A/osu!/Songs");
            MissAnalyzer analyzer = new MissAnalyzer(r, b);
            Assert.AreEqual(missCount, analyzer.MissCount);
        }
    }
}