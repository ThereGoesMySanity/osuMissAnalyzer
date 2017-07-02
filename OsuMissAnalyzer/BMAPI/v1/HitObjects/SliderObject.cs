using osuDodgyMomentsFinder;
using ReplayViewer.Curves;
using System.Collections.Generic;

namespace BMAPI.v1.HitObjects
{
    public class SliderObject : CircleObject
    {
        public SliderObject()
        {
        }
        public SliderObject(CircleObject baseInstance) : base(baseInstance) { }

        public new SliderType Type = SliderType.Linear;
        public List<Point2> Points = new List<Point2>();
        public int RepeatCount
        {
            get; set;
        }
        private float _PixelLength = 0f;
        public float PixelLength
        {
            get
            {
                return this._PixelLength;
            }
            set
            {
                this._PixelLength = value;
            }
        }
        public int duration
        {
            get; set;
        }
        private float _TotalLength = -1;
        public float TotalLength
        {
            get
            {
                return this._TotalLength;
            }
            set
            {
                this._TotalLength = value;
            }
        }
        private float _SegmentEndTime = -1;
        public float SegmentEndTime
        {
            get
            {
                if(this._SegmentEndTime < 0)
                {
                    this._SegmentEndTime = this.StartTime + this.TotalLength / this.Velocity;
                }
                return this._SegmentEndTime;
            }
        }
        public float Velocity
        {
            get; set;
        }
        public float MaxPoints
        {
            get; set;
        }
        // a list of every curve in the slider
        // a new curve is denoted by a red slider control point in the editor
        public List<Curve> Curves
        {
            get; set;
        }

        public void CreateCurves()
        {
            /* not used
             * if (StartTime == 35915)
            {
                int u = 0;
            }*/

            this.Curves = new List<Curve>();
            int n = this.Points.Count;
            if(n == 0)
            {
                return;
            }
            Point2 lastPoint = this.Points[0];
            Curve currentCurve = null;
            for(int i = 0; i < n; i++)
            {
                // if there are two points in a row that are the same control point then it is a red control point
                // and a new curve exists
                if(lastPoint.Equals(this.Points[i]))
                {
                    currentCurve = this.CreateCurve();
                    this.Curves.Add(currentCurve);
                }
                currentCurve.AddPoint(this.Points[i].ToVector2());
                lastPoint = this.Points[i];
            }
            this._TotalLength = 0;
            int lastIndex = this.Curves.Count - 1;
            for(int i = 0; i < lastIndex; i++)
            {
                this.Curves[i].Init();
                this._TotalLength += this.Curves[i].Length;
            }
            if(lastIndex >= 0)
            {
                // the last curve will be affected by the 'pixel length' property of sliders which
                // limit how long it is (so that way it ends in time with the beat, not geometrically)
                Curve lastCurve = this.Curves[lastIndex];
                lastCurve.PixelLength = this.PixelLength - this._TotalLength;
                lastCurve.Init();
                this._TotalLength += lastCurve.Length;
            }
        }

        private Curve CreateCurve()
        {
            if(this.Points.Count == 0)
            {
                return null;
            }
            else if(this.Points.Count == 1)
            {
                return new Catmull();
            }
            else if(this.Points.Count == 2)
            {
                return new Line();
            }
            else if(this.Points.Count > 3)
            {
                // sometimes there will be sliders that say that they are using the passthrough
                // algorithm but have more than 3 points
                // in these cases we just use bezier algorithm
                return new Bezier();
            }
            switch(this.Type)
            {
                case SliderType.Linear:
                    return new Line();
                case SliderType.Bezier:
                    return new Bezier();
                case SliderType.PSpline:
                    return new Circle();
                case SliderType.CSpline:
                    return new Catmull();
                default:
                    return null;
            }
        }

        public Vector2 PositionAtTime(float t)
        {
            return this.PositionAtDistance(this.TotalLength * t);
        }

        public Vector2 PositionAtDistance(float d)
        {
            float sum = 0;
            foreach(Curve curve in this.Curves)
            {
                // find out which curve's algorithm is being used at a distance
                if(sum + curve.Length >= d)
                {
                    // put distance relative to the curve's start point
                    return curve.PositionAtDistance(d - sum);
                }
                sum += curve.Length;
            }
            Curve lastCurve = this.Curves[this.Curves.Count - 1];
            return lastCurve.PositionAtDistance(d - (sum - lastCurve.Length));
        }
    }
}
