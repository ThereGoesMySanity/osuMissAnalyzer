using System;

namespace OsuMissAnalyzer.Utils
{
    public static class TimeUtils
    {
        public static string ToLongString(this TimeSpan time)
        {
            string output = String.Empty;

            if (time.Days > 0)
                output += time.Days + " days ";

            if ((time.Days == 0 || time.Days == 1) && time.Hours > 0)
                output += time.Hours + " hr ";

            if (time.Days == 0 && time.Minutes > 0)
                output += time.Minutes + " min ";

            if (output.Length == 0)
                output += time.Seconds + " sec";

            return output.Trim();
        }
    }
}