using System;
using System.Linq;
using NUnit.Framework;
using OsuMissAnalyzer.Core;

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

    }
}