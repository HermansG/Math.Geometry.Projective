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
    /// A homogeneous vector with 3 complex (or real) homogeneous coordinates, representing a point in 2-dimensional projective space.<para>
    /// When the first coordinate is 0, the point is at infinity.</para><para>
    /// When the first coordinate is 1, the other 2 coordinates are the affine or euclidean coordinates of the point.</para>
    /// </summary>
    public class Point2D : HVector
    {
        #region constructors
        /// <summary>
        /// The vector data are copied into the coordinates of the new point.
        /// </summary>
        public Point2D(HVector hvector)
            : base(hvector)
        {
            if (hvector.Count != 3) throw new ArgumentException("hvector must have 3 coordinates");
        }
        /// <summary>
        /// The vector data are copied into the coordinates of the new point.
        /// </summary>
        public Point2D(Vector<Complex> vector)
            : base(vector)
        {
            if (vector.Count != 3) throw new ArgumentException("vector must have 3 coordinates");
        }
        /// <summary>
        /// The values are copied into the coordinates of the new point.
        /// </summary>
        public Point2D(Complex[] values)
            : base(values)
        {
            if (values.Length != 3) throw new ArgumentException("values must have 3 entries");
        }
        /// <summary>
        /// The values are copied into the data of the new point.
        /// </summary>
        public Point2D(double[] values)
            : base(values)
        {
            if (values.Length != 3) throw new ArgumentException("values must have 3 entries");
        }
        /// <summary>
        /// The affine or Euclidean values (complex or real) are copied into the coordinates of the new point.<para>
        /// The first coordinate will be One.</para>
        /// </summary>
        public Point2D(Complex x, Complex y) : this(new Complex[] { Complex.One, x, y }) { }
        /// <summary>
        /// The values (complex or real) are copied into the coordinates of the new point.<para>
        /// When x0=1, x1 is the affine or Euclidean value of x and x2 that of y.</para>
        /// </summary>
        public Point2D(Complex x0, Complex x1, Complex x2) : this(new Complex[] { x0, x1, x2 }) { }
        /// <summary>
        /// The affine or Euclidean values (complex or real) are copied into the coordinates of the new point.<para>
        /// The first coordinate will be One.</para>
        /// </summary>
        public Point2D(Vector2 position) : this(new Complex[] { Complex.One, position[0], position[1] }) { }
        #endregion

        /// <summary>
        /// Create a new point, identical to this one.
        /// </summary>
        public new Point2D Clone() { return new Point2D(this.vector); }

        #region meet and join
        /// <summary>
        /// Return the line through this point and another point, or null when the points are identical.
        /// </summary>
        public Line2D Join(Point2D point)
        {
            if (point == null) throw new ArgumentNullException("point");
            if (this.Equals(point)) return null;
            return new Line2D(Extensions.CrossProduct(this.ToArray(), point.ToArray()));
        }
        /// <summary>
        /// Check whether a this point is at infinity.
        /// </summary>
        public bool IsAtInfinity()
        {
            return IsIncident(Line2D.Infinity);
        }
        /// <summary>
        /// Check whether a given line passes through this point.
        /// </summary>
        public bool IsIncident(Line2D line)
        {
            if (line == null) throw new ArgumentNullException("line");
            var product = this.ToVector() * line.ToVector();
            return product.IsZero();
        }
        /// <summary>
        /// Check whether a given hvector is incident with this point.
        /// </summary>
        [Obsolete("It's better to use a paramter of type 'Line2D'.")]
        public new bool IsIncident(HVector other) { return base.IsIncident(other); }
        #endregion

        /// <summary>
        /// Interpret the point a san Euclidean point and return its distance from the origin.
        /// </summary>
        public double DistanceOrigin()
        {
            var arrow = this.ToAffine() - Vector2.Origin;
            return arrow.Norm();
        }

        /// <summary>
        /// The corresponding 2-dimensional affine or euclidean coordinates of the point are returned.<para>
        /// When the point is at infinity, 'null' is returned.</para>
        /// </summary>
        public Vector2 ToAffine()
        {
            if (vector[0].IsZero()) return null;
            else return new Vector2(vector[1] / vector[0], vector[2] / vector[0]);
        }

        /// <summary>
        /// When the point is at infinity, its remaining 2-dimensional affine or euclidean coordinates are returned.<para>
        /// Otherwise 'null' is returned.</para>
        /// </summary>
        public Vector2 AsDirection()
        {
            if (vector[0].IsZero())
            {
                var rv = new Vector2(vector[1], vector[2]).Normalize();
                if ((rv * Vector2.EX).Real.IsZero())
                {
                    if ((rv * Vector2.EY).Real < 0)
                    {
                        return -rv;
                    }
                }
                else if ((rv * Vector2.EX).Real < 0)
                {
                    return -rv;
                }
                return rv;
            }
            else return null;
        }

        /// <summary>
        /// A string representation for the corresponding 2-dimensional affine or euclidean coordinates of the point.<para>
        /// When the point is at infinity, "(direction towards infinity)", is added.</para>
        /// </summary>
        public string ToAffineString()
        {
            if (vector[0].IsZero())
            {
                Complex[] array = new Complex[] { vector[1], vector[2] };
                return array.ToVectorString() + " (direction towards infinity)";
            }
            else
            {
                Complex[] array = new Complex[] { vector[1] / vector[0], vector[2] / vector[0] };
                return array.ToVectorString();
            }
        }

        #region new incident objects
        /// <summary>
        /// Get a random line from the pencil of lines through this point.
        /// </summary>
        public Line2D GetLine(bool real = true, IEnumerable<Line2D> exclude = null)
        {
            var incident = this.GetRandomIncident(real, exclude);
            return new Line2D(incident);
        }
        #endregion

        #region constants
        /// <summary>
        /// (1 0 0)
        /// </summary>
        public static readonly Point2D Origin = new Point2D(1, 0, 0);
        /// <summary>
        /// (0 1 0)
        /// </summary>
        public static readonly Point2D InfinityX = new Point2D(0, 1, 0);
        /// <summary>
        /// (0 0 1)
        /// </summary>
        public static readonly Point2D InfinityY = new Point2D(0, 0, 1);
        /// <summary>
        /// (1 1 1)
        /// </summary>
        public static readonly Point2D Unity = new Point2D(1, 1, 1);
        #endregion
    }
}