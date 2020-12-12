using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
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
    }
}