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
    //using Geometry.Projective.Extensions;
    using MathNet.Numerics.LinearAlgebra.Complex;

    public static class Functions
    {
        public static ParameterList<Point2D> NodesLemniscate(double focal_distance, Vector2 center = null, double rotationangle = 0)
        {
            if (center == null) center = Vector2.Origin;

            var origin = new Point2D(1, 0 + center[0], 0 + center[1]);

            var list = new ParameterList<Point2D>(Lemniscate(focal_distance, center, rotationangle));

            double step = 0.1;

            for (double t = 0; t < 2 * Math.PI; t += step)
            {
                if (t > Math.PI / 2 && (t - step) < Math.PI / 2)
                {
                    list.Add(origin);
                }
                else if (t > Math.PI * 1.5 && (t - step) < Math.PI * 1.5)
                {
                    list.Add(origin);
                }

                list.Add(t);
            }

            return list;
        }

        static Func<Complex, Point2D> Lemniscate(double focal_distance, Vector2 center = null, double rotationangle = 0)
        {
            if (center == null) center = Vector2.Origin;

            Func<Complex, Complex> X = t => (focal_distance * Math.Sqrt(2) * Trig.Cos(t)) / (1 + Trig.Sin(t).Square());
            Func<Complex, Complex> Y = t => X(t) * Trig.Sin(t);

            Complex cos = Trig.Cos(Trig.DegreeToRadian(rotationangle));
            Complex sin = Trig.Sin(Trig.DegreeToRadian(rotationangle));

            Func<Complex, Complex, Complex> XRotated = (x, y) => x * cos - y * sin;
            Func<Complex, Complex, Complex> YRotated = (x, y) => x * sin + y * cos;

            return t => new Point2D(1, XRotated(X(t), Y(t)) + center[0], YRotated(X(t), Y(t)) + center[1]);
        }

        //public static List<Point3D> NodesCircle(Point3D center, Plane3D plane, double radius)
        //{
        //    if (center == null) throw new ArgumentException("center");
        //    if (plane == null) throw new ArgumentException("plane");
        //    if (radius <= Extensions.PrecisionZero) throw new ArgumentException("radius");
        //    if (!plane.IsIncident(center)) throw new ArgumentException("center of circle not in the given plane");

        //    Point3D other = plane.GetPoint(exclude: center);
        //    while (other.IsAtInfinity)
        //    {
        //        other = plane.GetPoint(exclude: center);
        //    }

        //    Vector3 radius_x = radius * (other.ToAffine() - center.ToAffine()).Normalize();
        //    Vector3 radius_y = radius * plane.NormalVector.CrossProduct(radius_x).Normalize();

        //    System.Diagnostics.Debug.Assert(Extensions.IsZero(radius_x * radius_y));
        //    System.Diagnostics.Debug.Assert(Extensions.IsZero(radius_x * plane.NormalVector));
        //    System.Diagnostics.Debug.Assert(Extensions.IsZero(radius_y * plane.NormalVector));

        //    double step = 0.05;

        //    Func<double, Point3D> CiclePoint = t => new Point3D(center.ToAffine() + Math.Cos(t) * radius_x + Math.Sin(t) * radius_y);

        //    var nodes = new List<Point3D>();

        //    for (double t = 0; t < 2 * Math.PI; t += step)
        //    {
        //        nodes.Add(CiclePoint(t));
        //    }
        //    return nodes;
        //}

        public static ParameterList<Point3D> NodesCircle(Point3D center, Plane3D plane, double radius)
        {
            if (center == null) throw new ArgumentException("center");
            if (plane == null) throw new ArgumentException("plane");
            if (radius <= Extensions.PrecisionZero) throw new ArgumentException("radius");
            if (!plane.IsIncident(center)) throw new ArgumentException("center of circle not in the given plane");

            Point3D other = plane.GetPoint(exclude: center);
            while (other.IsAtInfinity)
            {
                other = plane.GetPoint(exclude: center);
            }

            Vector3 radius_x = radius * (other.ToAffine() - center.ToAffine()).Normalize();
            Vector3 radius_y = radius * plane.NormalVector.CrossProduct(radius_x).Normalize();

            System.Diagnostics.Debug.Assert(Extensions.IsZero(radius_x * radius_y));
            System.Diagnostics.Debug.Assert(Extensions.IsZero(radius_x * plane.NormalVector));
            System.Diagnostics.Debug.Assert(Extensions.IsZero(radius_y * plane.NormalVector));

            double step = 0.05;

            Func<Complex, Point3D> CirclePoint = t => new Point3D(center.ToAffine() + Trig.Cos(t) * radius_x + Trig.Sin(t) * radius_y);

            var nodes = new ParameterList<Point3D>(CirclePoint);

            for (double t = 0; t < 2 * Math.PI; t += step)
            {
                nodes.Add(CirclePoint(t), t);
            }
            return nodes;
        }
    }
}
