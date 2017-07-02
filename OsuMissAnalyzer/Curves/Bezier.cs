using osuDodgyMomentsFinder;

namespace ReplayViewer.Curves
{
    public class Bezier : Curve
    {
        public Bezier() : base(BMAPI.v1.SliderType.Bezier)
        {
        }

        protected override Vector2 Interpolate(float t)
        {
            int n = this.Points.Count;
            if(n == 2)
            {
                return this.Lerp(this.Points[0], this.Points[1], t);
            }
            Vector2[] pts = new Vector2[n];

            for(int i = 0; i < n; i++)
            {
                pts[i] = this.Points[i] + new Vector2();
            }

            for(int k = 1; k < n; k++)
            {
                for(int i = 0; i < n - k; i++)
                {
                    pts[i] = this.Lerp(pts[i], pts[i + 1], t);
                }
            }
            return pts[0];
        }
    }
}
