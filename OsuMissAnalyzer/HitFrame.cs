using BMAPI.v1.HitObjects;
using ReplayAPI;

namespace osuDodgyMomentsFinder
{
    public class HitFrame
    {
        public ReplayFrame frame { get; set; }
        public CircleObject note { get; set; }
        public Keys key { get; set; }

        private double _perfectness = -1;
        public double Perfectness { get { return _perfectness > -1 ? _perfectness : calc_perfectness(); } private set { } }

        private double calc_perfectness()
        {
            _perfectness = Utils.pixelPerfectHitFactor(frame, note);
            return _perfectness;
        }

        public HitFrame(CircleObject note, ReplayFrame frame, Keys key)
        {
            this.frame = frame;
            this.note = note;
            this.key = key;
        }


        public override string ToString()
        {
            double hit = Perfectness;
            string res = note.ToString();
            res += hit <= 1 ? "" : " ATTEMPTED";
            res += " HIT at " + frame.Time + "ms";
            res += " (" + (frame.Time - note.StartTime) + "ms error, " + hit + " perfectness)";
            //res += "(" + frame.keyCounter + ")";

            return res;

        }

    }
}
