using System;
using System.Drawing;
using osuDodgyMomentsFinder;

namespace BMAPI
{
    public class Point2
    {
        public float X
        {
            get; set;
        }
        public float Y
        {
            get; set;
        }
        public Point2()
        {
            X = Y = 0;
        }
        public Point2(float V)
        {
            X = Y = V;
        }
        public Point2(float X, float Y)
        {
            this.X = X;
            this.Y = Y;
        }
        public float Length
        {
            get
            {
                return (float)Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2));
            }
        }



        public void Normalize()
        {
            float length = Length; //Cache for minor performance improvement
            X = X / length;
            Y = Y / length;
        }
        public float DistanceTo(Point2 P)
        {
            return (float)Math.Sqrt(Math.Pow(P.X - X, 2) + Math.Pow(P.Y - Y, 2));
        }
        public static Point2 operator -(float Left, Point2 Right)
        {
            return new Point2(Left - Right.X, Left - Right.Y);
        }
        public static Point2 operator -(Point2 Left, float Right)
        {
            return new Point2(Left.X - Right, Left.Y - Right);
        }
        public static Point2 operator -(Point2 Left, Point2 Right)
        {
            return new Point2(Left.X - Right.X, Left.Y - Right.Y);
        }
        public static Point2 operator +(float Left, Point2 Right)
        {
            return new Point2(Right.X + Left, Right.Y + Left);
        }
        public static Point2 operator +(Point2 Left, float Right)
        {
            return new Point2(Left.X + Right, Left.Y + Right);
        }
        public static Point2 operator +(Point2 Left, Point2 Right)
        {
            return new Point2(Left.X + Right.X, Left.Y + Right.Y);
        }
        public static Point2 operator *(float Left, Point2 Right)
        {
            return new Point2(Right.X * Left, Right.Y * Left);
        }
        public static Point2 operator *(Point2 Left, float Right)
        {
            return new Point2(Left.X * Right, Left.Y * Right);
        }
        public static Point2 operator *(Point2 Left, Point2 Right)
        {
            return new Point2(Left.X * Right.X, Left.Y * Right.Y);
        }

        internal Vector2 ToVector2()
        {
            return new Vector2(this.X, this.Y);
        }
		internal Point ToPoint()
		{
			return new Point((int)X, (int)Y);
		}
		internal PointF ToPointF()
		{
			return new PointF(X, Y);
		}

        public static Point2 operator /(float Left, Point2 Right)
        {
            return new Point2(Left / Right.X, Left / Right.Y);
        }
        public static Point2 operator /(Point2 Left, float Right)
        {
            return new Point2(Left.X / Right, Left.Y / Right);
        }
        public static Point2 operator /(Point2 Left, Point2 Right)
        {
            return new Point2(Left.X / Right.X, Left.Y / Right.Y);
        }
        public static bool operator ==(Point2 Left, Point2 Right)
        {
            return (Left.X.CompareTo(Right.X) == 0 && Left.Y.CompareTo(Right.Y) == 0);
        }
        public static bool operator !=(Point2 Left, Point2 Right)
        {
            return (Left.X.CompareTo(Right.X) != 0 || Left.Y.CompareTo(Right.Y) != 0);
        }
        public override bool Equals(object obj)
        {
            return this == (Point2)obj;
        }
        public override int GetHashCode()
        {
            return (int)Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2));
        }
        public override string ToString()
        {
            return String.Format("({0}, {1})", this.X, this.Y);
            ;
        }
        public Point2 Lerp(Point2 other, float t)
        {
            return (1 - t) * this + t * other;
        }
        public float Atan2()
        {
            return (float)Math.Atan2(this.Y, this.X);
        }
    }
}
