using System;
using BMAPI.v1;
using OsuMissAnalyzer.Core.Utils;
using ReplayAPI;

namespace OsuMissAnalyzer.UI
{
    public class ReplayListItem
    {
        public Beatmap beatmap;
        public Replay replay;

        public string[] ToRows()
        {
            return new string[] { $"{(beatmap?.ToString() ?? "Unknown beatmap")}",
                    replay.Mods.ModsToString(),
                    $"{TimeUtils.ToLongString(DateTime.Now.ToUniversalTime() - replay.PlayTime.ToUniversalTime())} ago"
                    };
        }
    }
}