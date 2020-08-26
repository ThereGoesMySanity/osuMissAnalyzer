using System;
using BMAPI.v1;
using OsuMissAnalyzer.Utils;
using ReplayAPI;

namespace OsuMissAnalyzer.UI
{
    public class ReplayListItem
    {
        public Beatmap beatmap;
        public Replay replay;

        public string[] ToRows()
        {
            return new string[] { (beatmap?.ToString() ?? "Unknown beatmap") + " "
                    + (replay.Mods != 0? "+" + replay.Mods.ToString() + " " : "")
                    , TimeUtils.ToLongString(DateTime.Now - replay.PlayTime) + " ago"
                    };
        }
    }
}