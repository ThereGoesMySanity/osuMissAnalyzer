using System;
using BMAPI.v1;
using OsuMissAnalyzer.Core.Utils;
using ReplayAPI;

namespace OsuMissAnalyzer.UI.Models
{
    public class ReplayListItem
    {
        public Beatmap Beatmap { get; set; }
        public Replay Replay { get; set; }

        public string BeatmapName => Beatmap.ToString();
        public string ReplayMods => Replay.Mods.ToString();

        public string TimeAgo => $"{TimeUtils.ToLongString(DateTime.Now.ToUniversalTime() - Replay.PlayTime.ToUniversalTime())} ago";
    }
}