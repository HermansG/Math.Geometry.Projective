using System;
using System.Linq;
using System.Collections.Generic;

namespace Geometry.Projective
{
    using System.Numerics;
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Complex;

    /// <summary>
    /// A homogeneous vector with 3 complex (or real) homogeneous coordinates, representing a line in 2-dimensional projective space.<para>
    /// Line y = ax + b has coordinates (b, a, -1), line x = c has coordinates (-c, 1, 0).</para>
    /// </summary>
    public class Line2D : HVector
    {
        #region constructors
        /// <summary>
        /// The vector data are copied into the coordinates of the new line.
        /// </summary>
        public Line2D(HVector hvector)
            : base(hvector)
        {
            if (hvector.Count != 3) throw new ArgumentException("hvector must have 3 coordinates");
        }
        /// <summary>
        /// The vector data are copied into the coordinates of the new line.
        /// </summary>
        public Line2D(Vector<Complex> vector)
            : base(vector)
        {
            if (vector.Count != 3) throw new ArgumentException("vector must have 3 coordinates");
        }
        /// <summary>
        /// The values are copied into the coordinates of the new line.
        /// </summary>
        public Line2D(Complex[] values)
            : base(values)
        {
            if (values.Length != 3) throw new ArgumentException("values must have 3 entries");
        }
        /// <summary>
        /// The values are copied into the coordinates of the new line.
        /// </summary>
        public Line2D(double[] values)
            : base(values)
        {
            if (values.Length != 3) throw new ArgumentException("values must have 3 entries");
        }
        /// <summary>
        /// The values of the line in the form y=ax+b.
        /// </summary>
        public Line2D(Complex slope, Complex offsetYaxis) : this(new Complex[] { offsetYaxis, slope, -Complex.One }) { }
        /// <summary>
        /// The values of the line in the form x=c.
        /// </summary>
        public Line2D(Complex offsetXaxis) : this(new Complex[] { -offsetXaxis, Complex.One, 0 }) { }
        /// <summary>
        /// The values are copied into the coordinates of the new line.
        /// </summary>
        public Line2D(Complex u0, Complex u1, Complex u2) : this(new Complex[] { u0, u1, u2 }) { }
        #endregion

        /// <summary>
        /// Create a new line, identical to this one.
        /// </summary>
        public new Line2D Clone() { return new Line2D(this.vector); }

        /// <summary>
        /// Return the 2-dimensional direction vector of the line. The vector will be zero when the line is at infinity.
        /// </summary>
        public Vector2 Direction() { return new Vector2(this[2], -this[1]); }

        /// <summary>
        /// Return the offset on the x-axis.
        /// </summary>
        public Complex OffsetX()
        {
            var meet = this.Meet(Xaxis);
            if (meet[0].IsZero()) return double.PositiveInfinity;
            else return meet[1] / meet[0];
        }

        /// <summary>
        /// Return the offset on the y-axis.
        /// </summary>
        public Complex OffsetY()
        {
            var meet = this.Meet(Yaxis);
            if (meet[0].IsZero()) return double.PositiveInfinity;
            else return meet[2] / meet[0];
        }

        /// <summary>
        /// Interpret the line as an Eclidean line and return its distance from the origin.
        /// </summary>
        public double DistanceOrigin()
        {
            var point2d = new Point2D(Complex.One, this[1], this[2]);
            var perpendicularline = point2d.Join(Point2D.Origin);
            var meet = perpendicularline.Meet(this);
            return meet.DistanceOrigin();
        }

        #region meet and join
        /// <summary>
        /// Calculate the outer- or cross-product of two 3-dimensional vectors.
        /// </summary>
        Complex[] CrossProduct(Line2D other)
        {
            Complex[] values = new Complex[3];
            values[0] = this[1] * other[2] - this[2] * other[1];
            values[1] = this[2] * other[0] - this[0] * other[2];
            values[2] = this[0] * other[1] - this[1] * other[0];
            return values;
        }
        /// <summary>
        /// Return the point where this line meets another line, or null when the lines are identical.
        /// </summary>
        public Point2D Meet(Line2D line)
        {
            if (line == null) throw new ArgumentNullException("line");
            if (this.Equals(line)) return null;
            return new Point2D(this.CrossProduct(line));
        }
        /// <summary>
        /// Check whether a given 2D point lies on this line.
        /// </summary>
        public bool IsIncident(Point2D point)
        {
            if (point == null) throw new ArgumentNullException("point");
            var product = this.ToVector() * point.ToVector();
            return product.IsZero();
        }
        /// <summary>
        /// Check whether a given hvector is incident with this line.
        /// </summary>
        [Obsolete("It's better to use a paramter of type 'Point2D'.")]
        public new bool IsIncident(HVector other) { return base.IsIncident(other); }
        #endregion

        /// <summary>
        /// A string representation for the corresponding 2-dimensional affine or euclidean equation of the line.<para>
        /// Either in the form y=ax+b, x=c or "line at infinity".</para>
        /// </summary>
        public string ToAffineString()
        {
            if (Equals(Line2D.Infinity))
            {
                return "line at infinity";
            }
            else if (vector[2].IsZero())
            {
                return "x = " + (-vector[0] / vector[1]).ToString();
            }
            else
            {
                return "y = (" + (-vector[1] / vector[2]).ToString() + ") x + (" + (-vector[0] / vector[2]).ToString() + ")";
            }

        }

        #region new incident objects
        /// <summary>
        /// Get a random point from the pencil of points on this line.
        /// </summary>
        public Point2D GetPoint(bool real = true, IEnumerable<Point2D> exclude = null)
        {
            var incident = this.GetRandomIncident(real, exclude);
            return new Point2D(incident);
        }
        #endregion

        #region constants
        /// <summary>
        /// [1 0 0]
        /// </summary>
        public static readonly Line2D Infinity = new Line2D(1, 0, 0);
        /// <summary>
        /// [0 0 1]
        /// </summary>
        public static readonly Line2D Xaxis = new Line2D(0, 0, 1);
        /// <summary>
        /// [0 1 0]
        /// </summary>
        public static readonly Line2D Yaxis = new Line2D(0, 1, 0);
        /// <summary>
        /// [1 1 1]
        /// </summary>
        public static readonly Line2D Unity = new Line2D(1, 1, 1);
        #endregion
    }
}
