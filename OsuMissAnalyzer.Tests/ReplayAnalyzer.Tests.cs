using System;
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
        [Test]
        public void TestOsuDb()
        {
            var db = new OsuDbAPI.OsuDbFile("Resources/osu!.db", byHash: true);
            CollectionAssert.AllItemsAreNotNull(db.Beatmaps.Select(b => b.Hash));
            Assert.True(db.Beatmaps.All(b => db.BeatmapsByHash.ContainsKey(b.Hash)));
            Console.WriteLine(db.BeatmapsByHash.Count);
        }
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
            var db = new OsuDbAPI.OsuDbFile("/home/will/osu!/osu!.db", byId: true);
            Beatmap b = db.BeatmapsById[539697].Load("/home/will/A/osu!/Songs");
            Replay r = new Replay("/home/will/osu!/Data/r/20b064985202e1a5219432c774476b8b-132562134499563000.osr");
            MissAnalyzer analyzer = new MissAnalyzer(r, b);
            SliderObject s = (SliderObject)b.HitObjects[465];
            Console.WriteLine(s);
            Console.WriteLine(string.Join(", ", s.Curves));
            Console.WriteLine(string.Join(", ", s.Points));
            Console.WriteLine(string.Join(", ", s.Curves.SelectMany(curve => curve.CurveSnapshots)));

        }
    }
}