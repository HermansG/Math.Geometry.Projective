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
    /// The Möbiusplane is the extended Gaussian plane for complex numbers.
    /// </summary>
    public class MoebiusPlane
    {
        /// <summary>
        /// The paramaters are three different complex numbers that form the base system in the Möbiusplane<para>
        /// w.r.t. the standard base system, which are the real numbers 0, 1 and infinity.</para>
        /// </summary>
        public MoebiusPlane(Complex origin, Complex unity, Complex infinity)
        {
            Origin = origin;
            Unity = unity;
            Infinity = infinity;

            if (origin.Norm() >= Extensions.PrecisionInfinity)
            {
                Origin = complex_infinity;
            }
            if (unity.Norm() >= Extensions.PrecisionInfinity)
            {
                Unity = complex_infinity;
            }
            if (infinity.Norm() >= Extensions.PrecisionInfinity)
            {
                Infinity = complex_infinity;
            }

            if (Unity.EqualsWithinPrecision(Origin) || Unity.EqualsWithinPrecision(Infinity) || Infinity.EqualsWithinPrecision(Origin))
            {
                throw new ArgumentException("The möbiusplane requires three different complex numbers as a base");
            }

            Unity2D = new Point2D(Unity.Real, Unity.Imaginary);
            Origin2D = new Point2D(Origin.Real, Origin.Imaginary);
            Infinity2D = new Point2D(Infinity.Real, Infinity.Imaginary);
        }

        /// <summary>
        /// The complex point that is the origin in the chosen base of the möbiusplane.
        /// </summary>
        public Complex Origin { get; private set; }
        Point2D Origin2D;
        /// <summary>
        /// The complex point that is unity in the chosen base of the möbiusplane.
        /// </summary>
        public Complex Unity { get; private set; }
        Point2D Unity2D;
        /// <summary>
        /// The complex point that is infinity in the chosen base of the möbiusplane.
        /// </summary>
        public Complex Infinity { get; private set; }
        Point2D Infinity2D;
        Complex complex_infinity = new Complex(double.PositiveInfinity, double.PositiveInfinity);
        /// <summary>
        /// Calculate the coordinates w.r.t. the standard base (0, 1, infinity) for the<para>
        /// möbius coordinate (i.e. the parameter of this function) w.r.t. the current base system.</para>
        /// </summary>
        public Complex ToStandard(Complex moebiuscoordinate)
        {
            return (Origin + moebiuscoordinate * Infinity) / (1 + moebiuscoordinate);
        }

        /// <summary>
        /// Calculate the coordinates w.r.t. the standard base (0, 1, infinity) for the<para>
        /// möbius coordinates (i.e. the parameters of this function) w.r.t. the current base system.</para>
        /// </summary>
        public List<Complex> ToStandard(List<Complex> moebiuscoordinates)
        {
            var rv = new List<Complex>();
            foreach (var item in moebiuscoordinates)
            {
                var wrtstandard = ToStandard(item);
                rv.Add(wrtstandard);
            }
            return rv;
        }

        /// <summary>
        /// Calculate the coordinates w.r.t. the standard base system of `count' points lying<para>
        /// on the circle of Apollonius with radius `radius' w.r.t. the current base system.</para>
        /// </summary>
        public Circle CircleApollonius(double radius)
        {
            // choose phi=0, 2pi/3, 4pi/3 as three points on the circle
            var complexpoint1 = ToStandard(new Complex(radius * Math.Cos(0), radius * Math.Sin(0)));
            var point2d1 = new Point2D(complexpoint1.Real, complexpoint1.Imaginary);

            var complexpoint2 = ToStandard(new Complex(radius * Math.Cos(Math.PI / 1.5), radius * Math.Sin(Math.PI / 1.5)));
            var point2d2 = new Point2D(complexpoint2.Real, complexpoint2.Imaginary);

            var complexpoint3 = ToStandard(new Complex(radius * Math.Cos(Math.PI / 0.75), radius * Math.Sin(Math.PI / 0.75)));
            var point2d3 = new Point2D(complexpoint3.Real, complexpoint3.Imaginary);

            return new Circle(point2d1, point2d2, point2d3);
        }

        /// <summary>
        /// Calculate the coordinates w.r.t. the standard base system of `count' points lying<para>
        /// on the coaxial circle with angle `phi' w.r.t. the current base system.</para>
        /// </summary>
        public Circle CircleCoaxial(double phi)
        {
            var complexpoint = ToStandard(new Complex(Math.Cos(phi), Math.Sin(phi)));
            var point2d = new Point2D(complexpoint.Real, complexpoint.Imaginary);

            return new Circle(Origin2D, Infinity2D, point2d);
        }
    }
}
