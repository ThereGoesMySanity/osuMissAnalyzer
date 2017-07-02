
namespace BMAPI.v1.HitObjects
{
    public class SpinnerObject : CircleObject
    {
        public SpinnerObject()
        {
        }
        public SpinnerObject(CircleObject baseInstance) : base(baseInstance) { }

        public float EndTime
        {
            get; set;
        }

        public override bool ContainsPoint(Point2 Point)
        {
            return (Point.X >= 0 && Point.X <= 512 && Point.Y >= 0 && Point.Y <= 384);
        }
    }
}
