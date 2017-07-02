using osuDodgyMomentsFinder;

namespace ReplayViewer.Curves
{
    public class Line : Curve
    {
        public Line() : base(BMAPI.v1.SliderType.Linear)
        {
        }

        protected override Vector2 Interpolate(float t)
        {
            if(this.Points.Count != 2)
            {
                return Vector2.Zero;
            }
            return this.Lerp(this.Points[0], this.Points[1], t);
        }
    }
}
