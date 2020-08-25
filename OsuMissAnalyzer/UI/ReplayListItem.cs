using System;
using BMAPI.v1;
using OsuMissAnalyzer.Utils;
using ReplayAPI;

namespace OsuMissAnalyzer.UI
{
    public struct ReplayListItem
    {
        public Beatmap beatmap;
        public Replay replay;

        public override string ToString()
        {
            return (beatmap?.ToString() ?? "Unknown beatmap") + " +" 
                    + replay.Mods.ToString() + " " 
                    + TimeUtils.ToLongString(replay.PlayTime - DateTime.Now) + " ago";
        }
    }
}