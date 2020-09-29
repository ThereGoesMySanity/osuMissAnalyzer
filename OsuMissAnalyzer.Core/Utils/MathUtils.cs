using System.Drawing;
using BMAPI;
using osuDodgyMomentsFinder;
using ReplayAPI;

namespace OsuMissAnalyzer.Core.Utils
{
    public static class MathUtils
    {
        public static PointF ToPointF(this Point2 point)
        {
            return new PointF(point.X, point.Y);
        }
        public static PointF GetPointF(this ReplayFrame frame)
        {
            return new PointF(frame.X, frame.Y);
        }
        public static PointF ToPointF(this Vector2 vect)
        {
            return new PointF((float)vect.X, (float)vect.Y);
        }
        /// <summary>
        /// Flips point about the center of the screen if the Hard Rock mod is on, does nothing otherwise.
        /// </summary>
        /// <returns>A possibly-flipped point.</returns>
        /// <param name="p">The point to be flipped.</param>
        /// <param name="s">The height of the rectangle it's being flipped in</param>
        /// <param name="hr">Whether or not Hard Rock is on.</param>
        public static PointF flip(PointF p, float s, bool hr)
        {
            if (!hr) return p;
            p.Y = s - p.Y;
            return p;
        }

        /// <summary>
        /// Changes origin to top right of rect and flips p if hr is <c>true</c>.
        /// </summary>
        /// <returns>A point relative to rect</returns>
        /// <param name="p1">The point.</param>
        /// <param name="rect">The bounding rectangle to subtract from</param>
        /// <param name="hr">Whether or not Hard Rock is on.</param>
        public static PointF pSub(PointF p1, RectangleF rect, bool hr = false)
        {
            PointF p = PointF.Subtract(p1, new SizeF(rect.Location));
            return flip(p, rect.Height, hr);
        }

        /// <summary>
        /// Scales point p by scale factor s.
        /// </summary>
        /// <returns>The scaled point.</returns>
        /// <param name="p">Point to be scaled.</param>
        /// <param name="s">Scale.</param>
        public static PointF Scale(PointF p, float s)
        {
            return new PointF(p.X * s, p.Y * s);
        }
        public static PointF Scale(PointF point, SizeF size)
        {
            return new PointF(point.X * size.Width, point.Y * size.Height);
        }
        public static SizeF Scale(SizeF point, SizeF size)
        {
            return new SizeF(point.Width * size.Width, point.Height * size.Height);
        }
        public static SizeF Div(SizeF one, SizeF two)
        {
            return new SizeF(one.Width / two.Width, one.Height / two.Height);
        }

        public static SizeF Scale(SizeF p, float s)
        {
            return new SizeF(p.Width * s, p.Height * s);
        }

        public static RectangleF Scale(RectangleF rect, float s)
        {
            return new RectangleF(Scale(rect.Location, s), Scale(rect.Size, s));
        }

        public static PointF ScaleToRect(PointF p, RectangleF rect, Rectangle area)
        {
            return ScaleToRect(p, rect, area.Size);
        }
        public static PointF ScaleToRect(PointF p, RectangleF rect, SizeF sz)
        {
            PointF ret = Scale(p, Div(sz, rect.Size));
            return ret;
        }

        public static RectangleF ScaleToRect(RectangleF p, RectangleF rect, Rectangle area)
        {
            return ScaleToRect(p, rect, area.Size);
        }
        public static RectangleF ScaleToRect(RectangleF p, RectangleF rect, SizeF sz)
        {
            return new RectangleF(PointF.Subtract(ScaleToRect(PointF.Add(p.Location, Scale(p.Size, 0.5f)), rect, sz),
                Scale(p.Size, Scale(Div(sz, rect.Size), 0.5f))),
                Scale(p.Size, Div(sz, rect.Size)));
        }
    }
}