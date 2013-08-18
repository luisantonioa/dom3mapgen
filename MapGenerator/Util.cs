using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BenTools.Mathematics;

namespace LambdaImageProcessing.Core
{
    public static class BitmapExtensionMethods
    {
        public static void ExecuteForEachPixel(this Bitmap bitmap, Action<Point, Bitmap> action)
        {
            Point point = new Point(0, 0);
            for (int x = 0; x < bitmap.Width; x++)
            {
                point.X = x;
                for (int y = 0; y < bitmap.Height; y++)
                {
                    point.Y = y;
                    action(point, bitmap);
                }
            }
        }

        public static void ExecuteForEachPixel(this Bitmap bitmap, Action<Point> action)
        {
            Point point = new Point(0, 0);
            for (int x = 0; x < bitmap.Width; x++)
            {
                point.X = x;
                for (int y = 0; y < bitmap.Height; y++)
                {
                    point.Y = y;
                    action(point);
                }
            }
        }

        public static void SetEachPixelColour(this Bitmap bitmap, Func<Point, Color> colourFunc)
        {
            Point point = new Point(0, 0);
            for (int x = 0; x < bitmap.Width; x++)
            {
                point.X = x;
                for (int y = 0; y < bitmap.Height; y++)
                {
                    point.Y = y;
                    bitmap.SetPixel(x, y, colourFunc(point));
                }
            }
        }

        public static void SetEachPixelColour(this Bitmap bitmap, Func<Point, Color, Color> colourFunc)
        {
            Point point = new Point(0, 0);
            for (int x = 0; x < bitmap.Width; x++)
            {
                point.X = x;
                for (int y = 0; y < bitmap.Height; y++)
                {
                    point.Y = y;
                    bitmap.SetPixel(x, y, colourFunc(point, bitmap.GetPixel(x, y)));
                }
            }
        }
    }

    public static class Utils
    {
        public static bool PointInPolygon(Vector p, IList<Vector> poly)
        {
            PointD p1, p2;
            bool inside = false;
            if (poly.Count < 3)
            {
                return false;
            }

            var oldPoint = new PointD(poly[poly.Count - 1][0], poly[poly.Count - 1][0]);

            foreach (Vector t in poly)
            {
                var newPoint = new PointD(t[0], t[1]);

                if (newPoint.X > oldPoint.X)
                {
                    p1 = oldPoint;
                    p2 = newPoint;
                }
                else
                {
                    p1 = newPoint;
                    p2 = oldPoint;
                }
                if ((newPoint.X < p[0]) == (p[0] <= oldPoint.X) && ((long)p[1] - (long)p1.Y) * (long)(p2.X - p1.X) < ((long)p2.Y - (long)p1.Y) * (long)(p[0] - p1.X))
                {
                    inside = !inside;
                }
                oldPoint = newPoint;
            }
            return inside;
        }

        public static Tuple<PointF, PointF> ToTuple(this VoronoiEdge edge)
        {
            return Tuple.Create(new PointF((float) edge.VVertexA[0], (float) edge.VVertexA[1]),
                                new PointF((float) edge.VVertexB[0], (float) edge.VVertexB[1]));
        }

        public static PointF ToPointF(this Vector vector)
        {
            return new PointF((float)vector[0], (float)vector[1]);
        }

        public static Point ToPoint(this Vector vector)
        {
            return new Point(Convert.ToInt32(vector[0]), Convert.ToInt32(vector[1]));
        }

        public static bool ContainsSegment(this Rectangle r, Segment segment)
        {
            return (r.Contains(new Point((int) segment.Start.X, (int) segment.Start.Y)) &&
                    r.Contains(new Point((int) segment.End.X, (int) segment.End.Y)));
        }

        public static bool ContainsEdge(this Rectangle r, VoronoiEdge edge)
        {
            return r.ContainsLine(edge.VVertexA.ToPointF(), edge.VVertexB.ToPointF());
        }

        public static bool ContainsLine(this Rectangle r, PointF p1, PointF p2)
        {
            return (r.Contains(new Point((int) p1.X, (int) p1.Y)) &&
                    r.Contains(new Point((int) p2.X, (int) p2.Y)));
        }

        public static bool IntersectsLine(this Rectangle r, PointF p1, PointF p2)
        {
            return LineIntersectsLine(p1, p2, new Point(r.X, r.Y), new Point(r.X + r.Width, r.Y)) ||
                LineIntersectsLine(p1, p2, new Point(r.X + r.Width, r.Y), new Point(r.X + r.Width, r.Y + r.Height)) ||
                LineIntersectsLine(p1, p2, new Point(r.X + r.Width, r.Y + r.Height), new Point(r.X, r.Y + r.Height)) ||
                LineIntersectsLine(p1, p2, new Point(r.X, r.Y + r.Height), new Point(r.X, r.Y));
        }

        public static bool IntersectsSegment(this Rectangle r, Segment segment)
        {
            return r.IntersectsLine(segment.Start, segment.End);
        }

        public static bool IntersectsEdge(this Rectangle r, VoronoiEdge edge)
        {
            return r.IntersectsLine(edge.VVertexA.ToPointF(), edge.VVertexB.ToPointF());
        }

        public static Point ToPoint(this PointF point)
        {
            return new Point(Convert.ToInt32(point.X), Convert.ToInt32(point.Y));
        }

        public static bool LineIntersectsLine(PointF lineApointA, PointF lineApointB, PointF lineBpointA, PointF lineBpointB)
        {
            float q = (lineApointA.Y - lineBpointA.Y) * (lineBpointB.X - lineBpointA.X) - (lineApointA.X - lineBpointA.X) * (lineBpointB.Y - lineBpointA.Y);
            float d = (lineApointB.X - lineApointA.X) * (lineBpointB.Y - lineBpointA.Y) - (lineApointB.Y - lineApointA.Y) * (lineBpointB.X - lineBpointA.X);

            if (Math.Abs(d - 0) < Double.Epsilon)
            {
                return false;
            }

            float r = q / d;

            q = (lineApointA.Y - lineBpointA.Y) * (lineApointB.X - lineApointA.X) - (lineApointA.X - lineBpointA.X) * (lineApointB.Y - lineApointA.Y);
            float s = q / d;

            if (r < 0 || r > 1 || s < 0 || s > 1)
            {
                return false;
            }

            return true;
        }
    }

    public class RectangleFComparer : IEqualityComparer<RectangleF>
    {
        #region IEqualityComparer<T> Members
    
        public bool Equals(RectangleF x, RectangleF y)
        {
            //if the xValue is null then we consider them equal if and only if yValue is null
            if (x == RectangleF.Empty)
                return y == RectangleF.Empty;
            
            //use the default comparer for whatever type the comparison property is.
            return x.Equals(y);
        }
    
        public int GetHashCode(RectangleF rectangle)
        {
            if (rectangle == RectangleF.Empty)
                return 0;
            else
                return rectangle.GetHashCode();
        }
    
        #endregion
    }

    [Flags]
    public enum MirrorType
    {
        NoMirror,
        MirrorRight,
        MirrorDown,
        MirrorOpposite
    }

    public class Segment
    {
        private readonly Rectangle _bounds;
        public readonly PointF Start;
        public readonly PointF End;
        public readonly PointF Left;
        public readonly PointF Right;

        public Segment(VoronoiEdge edge, Rectangle bounds)
        {
            _bounds = bounds;



            Left = edge.LeftData.ToPointF();
            Right = edge.RightData.ToPointF();

            var x1 = (int)edge.VVertexA[0];
            var y1 = (int)edge.VVertexA[1];
            var x2 = (int)edge.VVertexB[0];
            var y2 = (int)edge.VVertexB[1];

            if (x1 > x2 || (x1 == x2 && y1 > y2))
            {
                Start = edge.VVertexB.ToPointF();
                End = edge.VVertexA.ToPointF();
            }
            else
            {
                Start = edge.VVertexA.ToPointF();
                End = edge.VVertexB.ToPointF();
            }
        }

        public bool IsInBounds()
        {
            return _bounds.IntersectsSegment(this);
        }

        public bool Equals(Segment segment)
        {
            return (Start == segment.Start && End == segment.End) || (Start == segment.End && Start == segment.End);
        }

        public override int GetHashCode()
        {
            return Start.GetHashCode() + End.GetHashCode();
        }

        public double Length()
        {
            return Math.Sqrt(LengthSquared());
        }

        public double LengthSquared()
        {
            return (Start.X - End.X) * (Start.X - End.X)
                + (Start.Y - End.Y) * (Start.Y - End.Y);
        }

        public static double Length(PointD start, PointD end)
        {
            return Math.Sqrt((start.X - end.X) * (start.X - end.X)
                + (start.Y - end.Y) * (start.Y - end.Y));
        }
    }

    public struct PointD
    {
        public readonly double X;
        public readonly double Y;

        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Point ToPoint()
        {
            return new Point((int)X, (int)Y);
        }

        public override bool Equals(object obj)
        {
            return obj is PointD && this == (PointD)obj;
        }
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }
        public static bool operator ==(PointD a, PointD b)
        {
            return Math.Abs(a.X - b.X) < 0.1 && Math.Abs(a.Y - b.Y) < 0.1;
        }
        public static bool operator !=(PointD a, PointD b)
        {
            return !(a == b);
        }
    }
}