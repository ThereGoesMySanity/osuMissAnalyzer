using BMAPI.v1.HitObjects;
using ReplayAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osuDodgyMomentsFinder
{
    public class ClickFrame
    {
        public ReplayFrame frame { get; set; }
        public Keys key { get; set; }

        public ClickFrame(ReplayFrame frame, Keys key)
        {
            this.frame = frame;
            this.key = key;
        }

        public override string ToString()
        {
            string res = "";
            res += "* " + frame.Time + "ms";
            res += " (" + key + ")";

            return res;

        }
    }
}
