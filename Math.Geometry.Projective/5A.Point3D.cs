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
    /// A homogeneous vector with 4 complex (or real) homogeneous coordinates, representing a point in 3-dimensional projective space.<para>
    /// When the first coordinate is 0, the point is at infinity.</para><para>
    /// When the first coordinate is 1, the other 3 coordinates are the affine or euclidean coordinates of the point.</para>
    /// </summary>
    public class Point3D : HVector
    {
        #region constructors
        /// <summary>
        /// The vector data are copied into the coordinates of the new point.
        /// </summary>
        public Point3D(HVector hvector) : base(hvector) { initialize(); }
        /// <summary>
        /// The vector data are copied into the coordinates of the new point.
        /// </summary>
        public Point3D(Vector<Complex> vector) : base(vector) { initialize(); }
        /// <summary>
        /// The values are copied into the coordinates of the new point.
        /// </summary>
        public Point3D(Complex[] values) : base(values) { initialize(); }
        /// <summary>
        /// The values are copied into the data of the new point.
        /// </summary>
        public Point3D(double[] values) : base(values) { initialize(); }
        /// <summary>
        /// The values (complex or real) are copied into the coordinates of the new point.
        /// </summary>
        public Point3D(Complex x0, Complex x1, Complex x2, Complex x3) : this(new Complex[] { x0, x1, x2, x3 }) { }
        /// <summary>
        /// The values (complex or real) are copied into the data of the new vector.<para>
        /// The first coordinate will be One.</para>
        /// </summary>
        public Point3D(Complex x, Complex y, Complex z) : this(new Complex[] { Complex.One, x, y, z }) { }
        /// <summary>
        /// The vector data are copied into the coordinates of the new point.<para>
        /// The fourth coordinate 'z' will be zero.</para>
        /// </summary>
        public Point3D(Point2D point2d) : this(point2d[0], point2d[1], point2d[2], Complex.Zero) { }
        /// <summary>
        /// The vector data are copied into the coordinates of the new point.<para>
        /// The first coordinate will be One.</para>
        /// </summary>
        public Point3D(VectorC3 vector3d) : this(vector3d[0], vector3d[1], vector3d[2]) { }
        /// <summary>
        /// The values (complex or real) are copied into the coordinates of the new point.<para>
        /// The first coordinate will be One, the fourth coordinate 'z' will be zero.</para>
        /// </summary>
        public Point3D(Complex x, Complex y) : this(new Complex[] { Complex.One, x, y, Complex.Zero }) { }
        void initialize()
        {
            if (vector.Count != 4) throw new ArgumentException("4 coordinates required");
            conjugate = new Lazy<Point3D>(() => new Point3D(vector.Conjugate()));
        }
        #endregion

        /// <summary>
        /// Create a new point, identical to this one.
        /// </summary>
        public new Point3D Clone() { return new Point3D(this.vector); }

        #region meet and join
        /// <summary>
        /// Return the line through this point and another point, or null when the points are identical.
        /// </summary>
        public Line3D Join(Point3D point)
        {
            if (point == null) throw new ArgumentNullException("point");
            if (this.Equals(point)) return null;
            return Line3D.Create(this, point);
        }
        /// <summary>
        /// Return the plane through this point and a line, or null when this point is on the line.
        /// </summary>
        public Plane3D Join(Line3D line)
        {
            if (line == null) throw new ArgumentNullException("line");
            return line.Join(this);
        }
        /// <summary>
        /// Return the plane through this point and two other points, or null when two points are identical or the three points are collinear.
        /// </summary>
        public Plane3D Join(Point3D point1, Point3D point2)
        {
            if (point1 == null) throw new ArgumentNullException("point1");
            if (point2 == null) throw new ArgumentNullException("point2");
            var line = point1.Join(point2);
            if (line == null) return null;
            var rv = this.Join(line);
            return rv;
        }
        /// <summary>
        /// Check whether a given point lies in this plane.
        /// </summary>
        public bool IsIncident(Plane3D plane)
        {
            if (plane == null) throw new ArgumentNullException("plane");
            var product = this.ToVector() * plane.ToVector();
            return product.IsZero();
        }
        /// <summary>
        /// Check whether a given hvector is incident with this point.
        /// </summary>
        [Obsolete("It's better to use a paramter of type 'Plane'.")]
        public new bool IsIncident(HVector other) { return base.IsIncident(other); }
        /// <summary>
        /// Check whether this point lies on a given line.
        /// </summary>
        public bool IsIncident(Line3D line)
        {
            if (line == null) throw new ArgumentNullException("line");
            return line.IsIncident(this);
        }
        /// <summary>
        /// Check whether the point lies in the plan at infinity.
        /// </summary>
        public bool IsAtInfinity
        {
            get { return IsIncident(Plane3D.Infinity); }
        }
        #endregion

        /// <summary>
        /// The corresponding 3-dimensional affine or euclidean coordinates of the point are returned.<para>
        /// When the point is at infinity, 'null' is returned.</para>
        /// </summary>
        public VectorC3 ToAffine()
        {
            if (vector[0].IsZero()) return null;
            else return new VectorC3(vector[1] / vector[0], vector[2] / vector[0], vector[3] / vector[0]);
        }


        /// <summary>
        /// When the point is at infinity, its remaining 3-dimensional affine or euclidean coordinates are returned.<para>
        /// Otherwise 'null' is returned.</para>
        /// </summary>
        public VectorC3 AsDirection()
        {
            if (vector[0].IsZero())
            {
                var rv = new VectorC3(vector[1], vector[2], vector[3]).Normalize();
                if ((rv * VectorC3.EX).Real.IsZero())
                {
                    if ((rv * VectorC3.EY).Real.IsZero())
                    {
                        if ((rv * VectorC3.EZ).Real < 0)
                        {
                            return -rv;
                        }
                    }
                    else if ((rv * VectorC3.EY).Real < 0)
                    {
                        return -rv;
                    }
                }
                else if ((rv * VectorC3.EX).Real < 0)
                {
                    return -rv;
                }
                return rv;
            }
            else return null;
        }

        #region new incident objects
        /// <summary>
        /// Get a random plane from the sheave of planes through this point.
        /// </summary>
        public Plane3D GetPlane(bool real = true, IEnumerable<Plane3D> exclude = null)
        {
            var incident = this.GetRandomIncident(real, exclude);
            return new Plane3D(incident);
        }
        /// <summary>
        /// Get a random line from the sheave of lines through this point.
        /// </summary>
        public Line3D GetLine(bool real = true)
        {
            Point3D point = new Point3D(Extensions.PickRandomHVector(4, real));
            while (this.Equals(point))
            {
                point = new Point3D(Extensions.PickRandomHVector(4, real));
            }
            return Line3D.Create(this, point);
        }
        /// <summary>
        /// Get a random line from the pencil of lines through this point in a given plane.
        /// </summary>
        public Line3D GetLine(Plane3D plane, bool real = true)
        {
            if (plane == null) throw new ArgumentNullException("plane");
            if (!IsIncident(plane)) return null;
            Point3D point = plane.GetPoint(real, new List<Point3D>() { this });
            return Line3D.Create(this, point);
        }
        #endregion

        /// <summary>
        /// When the point is imaginary (complex), this is the line that carries the elliptical involution associated with the imaginary point.<para>
        /// For a real point the result value is 'null'.</para>
        /// </summary>
        public Line3D CarrierLine()
        {
            if (IsReal()) return null;

            return Line3D.Create(this, new Point3D(vector.Conjugate()));
        }

        Lazy<Point3D> conjugate;
        /// <summary>
        /// The point with complex conjugate coordinates.
        /// </summary>
        public new Point3D Conjugate { get { return conjugate.Value; } }

        #region constants
        /// <summary>
        /// (1 0 0 0)
        /// </summary>
        public static readonly Point3D Origin = new Point3D(1, 0, 0, 0);
        /// <summary>
        /// (0 1 0 0)
        /// </summary>
        public static readonly Point3D InfinityX = new Point3D(0, 1, 0, 0);
        /// <summary>
        /// (0 0 1 0)
        /// </summary>
        public static readonly Point3D InfinityY = new Point3D(0, 0, 1, 0);
        /// <summary>
        /// (0 0 0 1)
        /// </summary>
        public static readonly Point3D InfinityZ = new Point3D(0, 0, 0, 1);
        /// <summary>
        /// (1 1 0 0)
        /// </summary>
        public static readonly Point3D UnityX = new Point3D(1, 1, 0, 0);
        /// <summary>
        /// (1 0 1 0)
        /// </summary>
        public static readonly Point3D UnityY = new Point3D(1, 0, 1, 0);
        /// <summary>
        /// (1 0 0 1)
        /// </summary>
        public static readonly Point3D UnityZ = new Point3D(1, 0, 0, 1);
        /// <summary>
        /// (1 1 1 1)
        /// </summary>
        public static readonly Point3D Unity = new Point3D(1, 1, 1, 1);
        #endregion
    }
}
