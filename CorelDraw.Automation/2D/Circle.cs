using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CorelDraw.Automation
{
    using System.Numerics;
    using MathNet.Numerics;
    using Geometry.Projective;
    using MathNet.Numerics.LinearAlgebra.Complex;

    /// <summary>
    /// An Euclidean circle.
    /// </summary>
    public class Circle
    {
        public Circle(Point2D center, double radius)
        {
            Center = center.ToAffine();
            Radius = radius;
        }

        public Circle(Point2D p1, Point2D p2, Point2D p3)
        {
            Center = CenterCircle(p1, p2, p3);
            if (Center != null)
            {
                Radius = (Center.Distance(p1.ToAffine()) +
                          Center.Distance(p2.ToAffine()) +
                          Center.Distance(p3.ToAffine())) / 3;
            }
            else
            {
                Radius = double.PositiveInfinity;
                var line1 = p2.Join(p3);
                var line2 = p1.Join(p3);
                var line3 = p1.Join(p2);
                Line = new Line2D((line1[0] + line2[0] + line3[0]) / 3, (line1[1] + line2[1] + line3[1]) / 3, (line1[2] + line2[2] + line3[2]) / 3);
            }
        }

        public Line2D Line { get; private set; }
        public VectorC2 Center { get; private set; }
        public double Radius { get; private set; }
        public bool IsLine { get { return Line != null; } }

        public List<Point2D> GetPoints(int numberofpoints = 100)
        {
            if (Radius <= Extensions.PrecisionZero && Center != null)
            {
                return new List<Point2D> { new Point2D(Center) };
            }

            var rv = new List<Point2D>();

            if (IsLine)
            {
                double delta = (2 * Math.PI) / numberofpoints;
                for (double angle = 0; angle < 2 * Math.PI; angle += delta)
                {
                    rv.Add(new Point2D(Radius * Math.Cos(angle), Radius * Math.Sin(angle)));
                }
            }
            else if (Center != null)
            {
                double delta = (2 * Math.PI) / numberofpoints;
                for (double angle = 0; angle < 2 * Math.PI; angle += delta)
                {
                    rv.Add(new Point2D(Center[0] + Radius * Math.Cos(angle), Center[1] + Radius * Math.Sin(angle)));
                }
            }
            return rv;
        }

        /// <summary>
        /// Calculate the center of the circle through three given points.<para>
        /// When the points are not different or when any point lies at infinity, `null` is returned.</para>
        /// </summary>
        public VectorC2 CenterCircle(Point2D p1, Point2D p2, Point2D p3)
        {
            if (p1.IsAtInfinity() || p2.IsAtInfinity() || p3.IsAtInfinity())
            {
                return null;
            }

            if (p1.Equals(p2) || p2.Equals(p3) || p1.Equals(p3))
            {
                return null;
            }

            var mid1 = (p3.ToAffine() + p2.ToAffine()) * 0.5;
            var mid2 = (p1.ToAffine() + p3.ToAffine()) * 0.5;
            var mid3 = (p1.ToAffine() + p2.ToAffine()) * 0.5;

            var inf1 = p3.Join(p2).Meet(Line2D.Infinity);
            var inf2 = p1.Join(p3).Meet(Line2D.Infinity);
            var inf3 = p1.Join(p2).Meet(Line2D.Infinity);

            var line1 = new Point2D(mid1).Join(new Point2D(0, -inf1[2], inf1[1]));
            var line2 = new Point2D(mid2).Join(new Point2D(0, -inf2[2], inf2[1]));
            var line3 = new Point2D(mid3).Join(new Point2D(0, -inf3[2], inf3[1]));

            var mp1 = line3.Meet(line2).ToAffine();
            var mp2 = line3.Meet(line1).ToAffine();
            var mp3 = line1.Meet(line2).ToAffine();

            if (mp1 == null || mp2 == null || mp3 == null)
            {
                return null;
            }
            return (mp1 + mp2 + mp3) * (1.0 / 3.0);
        }
    }
}
