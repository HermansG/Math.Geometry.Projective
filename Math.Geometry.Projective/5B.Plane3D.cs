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
    /// A homogeneous vector with 4 complex (or real) homogeneous coordinates, representing a plane in 3-dimensional projective space.<para>
    /// Indices 1, 2, 3 give the Euclidean coordinates of the vector normal to the plane.</para><para>
    /// Index 0 divided by the norm of the normal vector gives the distance of the plane with respect to the origin.</para>
    /// </summary>
    public class Plane3D : HVector
    {
        #region constructors
        /// <summary>
        /// The vector data are copied into the coordinates of the new plane.
        /// </summary>
        public Plane3D(HVector hvector) : base(hvector) { initialize(); }
        /// <summary>
        /// The vector data are copied into the coordinates of the new plane.
        /// </summary>
        public Plane3D(Vector<Complex> vector) : base(vector) { initialize(); }
        /// <summary>
        /// The values are copied into the coordinates of the new plane.
        /// </summary>
        public Plane3D(Complex[] values) : base(values) { initialize(); }
        /// <summary>
        /// The values are copied into the coordinates of the new plane.
        /// </summary>
        public Plane3D(double[] values) : base(values) { initialize(); }
        /// <summary>
        /// The values are copied into the coordinates of the new plane.<para>
        /// The first coordinate will be Zero.</para>
        /// </summary>
        public Plane3D(Complex u1, Complex u2, Complex u3) : this(new Complex[] { Complex.Zero, u1, u2, u3 }) { }
        /// <summary>
        /// The values are copied into the coordinates of the new plane.
        /// </summary>
        public Plane3D(Complex u0, Complex u1, Complex u2, Complex u3) : this(new Complex[] { u0, u1, u2, u3 }) { }
        /// <summary>
        /// The vector data are copied into the coordinates of the new plane.<para>
        /// The first coordinate will be Zero.</para>
        /// </summary>
        public Plane3D(Vector3 vector3d) : this(vector3d[0], vector3d[1], vector3d[2]) { }
        void initialize()
        {
            if (vector.Count != 4) throw new ArgumentException("4 coordinates required");
            conjugate = new Lazy<Plane3D>(() => new Plane3D(vector.Conjugate()));
        }
        #endregion

        /// <summary>
        /// Create a new plane, identical to this one.
        /// </summary>
        public new Plane3D Clone() { return new Plane3D(this.vector); }

        /// <summary>
        /// Return the 3-dimensional vector normal to the plane. The vector will be zero when the plane is at infinity.
        /// </summary>
        public Vector3 NormalVector { get { return new Vector3(this[1], this[2], this[3]); } }
        /// <summary>
        /// Interpret the plane as an Euclidean plane and return the distance from this plane to the origin.
        /// </summary>
        public double Distance()
        {
            var nv = NormalVector;
            if (nv.IsZero()) return double.PositiveInfinity;
            var norm = nv.Norm();
            var distance = this[0] / norm;
            return distance.CoerceZero().Magnitude;
        }

        /// <summary>
        /// Return the line through a given 'point', perpendicular to this plane.<para>
        /// The 'point' may not lie in the plane; the plane may not be at infinity.</para>
        /// </summary>
        public Line3D PerpendicularLine(Point3D point)
        {
            if (IsIncident(point))
            {
                throw new ArgumentException("The point may not be incident with the plane");
            }
            if (Equals(Plane3D.Infinity))
            {
                throw new ArgumentException("A line perpendicualr to the plane at infinity is not defined");
            }
            return point.Join(new Point3D(point.ToAffine() + NormalVector));
        }

        #region meet and join
        /// <summary>
        /// Return the line where this plane and another plane meet, or null when the planes are identical.
        /// </summary>
        public Line3D Meet(Plane3D plane)
        {
            if (plane == null) throw new ArgumentNullException("plane");
            if (this.Equals(plane)) return null;
            return Line3D.Create(this, plane);
        }
        /// <summary>
        /// Return the point where this plane meets a given line, or null when the given line lies in this plane.
        /// </summary>
        public Point3D Meet(Line3D line)
        {
            if (line == null) throw new ArgumentNullException("line");
            return line.Meet(this);
        }
        /// <summary>
        /// Return the point where this plane and two other planes meet, or null when two planes are identical or the three planes are coaxial.
        /// </summary>
        public Point3D Meet(Plane3D plane1, Plane3D plane2)
        {
            if (plane1 == null) throw new ArgumentNullException("plane1");
            if (plane2 == null) throw new ArgumentNullException("plane2");
            var line = plane1.Meet(plane2);
            if (line == null) return null;
            return this.Meet(line);
        }
        /// <summary>
        /// Check whether a given point lies in this plane.
        /// </summary>
        public bool IsIncident(Point3D point)
        {
            if (point == null) throw new ArgumentNullException("point");
            var product = this.ToVector() * point.ToVector();
            return product.IsZero();
        }
        /// <summary>
        /// Check whether a given hvector is incident with this plane.
        /// </summary>
        [Obsolete("It's better to use a paramter of type 'Point'.")]
        public new bool IsIncident(HVector other) { return base.IsIncident(other); }
        /// <summary>
        /// Check whether this plane goes through a given line.
        /// </summary>
        public bool IsIncident(Line3D line)
        {
            if (line == null) throw new ArgumentNullException("line");
            return line.IsIncident(this);
        }
        #endregion

        #region new incident objects
        /// <summary>
        /// Get a random point from the field of points in this plane.
        /// </summary>
        public Point3D GetPoint(bool real = true, IEnumerable<Point3D> exclude = null)
        {
            var incident = this.GetRandomIncident(real, exclude);
            return new Point3D(incident);
        }
        /// <summary>
        /// Get a random point from the field of points in this plane.
        /// </summary>
        public Point3D GetPoint(bool real = true, params Point3D[] exclude)
        {
            if (exclude == null)
            {
                return GetPoint(real);
            }
            else
            {
                return GetPoint(real, exclude.ToList());
            }
        }
        /// <summary>
        /// Get a random line from the field of lines in this plane.
        /// </summary>
        public Line3D GetLine(bool real = true)
        {
            Plane3D plane = new Plane3D(Extensions.PickRandomHVector(4, real));
            while (this.Equals(plane))
            {
                plane = new Plane3D(Extensions.PickRandomHVector(4, real));
            }
            return Line3D.Create(this, plane);
        }
        /// <summary>
        /// Get a random line from the pencil of lines in this plane and through a given point.<para>
        /// The given point must be incident with this plane.</para>
        /// </summary>
        public Line3D GetLine(Point3D point, bool real = true)
        {
            if (point == null) throw new ArgumentNullException("point");
            if (!IsIncident(point)) return null;
            Plane3D plane = point.GetPlane(real, new List<Plane3D>() { this });
            return Line3D.Create(this, plane);
        }
        #endregion

        /// <summary>
        /// When the plane is imaginary (complex), this is the line that carries the elliptical involution associated with the imaginary plane.<para>
        /// For a real plane the result value is 'null'.</para>
        /// </summary>
        public Line3D CarrierLine()
        {
            if (IsReal()) return null;

            return Line3D.Create(this, new Plane3D(vector.Conjugate()));
        }

        Lazy<Plane3D> conjugate;
        /// <summary>
        /// The plane with complex conjugate coordinates.
        /// </summary>
        public new Plane3D Conjugate { get { return conjugate.Value; } }

        #region constants
        /// <summary>
        /// [0 1 0 0]
        /// </summary>
        public static readonly Plane3D YZ = new Plane3D(0, 1, 0, 0);
        /// <summary>
        /// [0 0 1 0]
        /// </summary>
        public static readonly Plane3D XZ = new Plane3D(0, 0, 1, 0);
        /// <summary>
        /// [0 0 0 1]
        /// </summary>
        public static readonly Plane3D XY = new Plane3D(0, 0, 0, 1);
        /// <summary>
        /// [1 0 0 0]
        /// </summary>
        public static readonly Plane3D Infinity = new Plane3D(1, 0, 0, 0);
        /// <summary>
        /// [1 1 1 1]
        /// </summary>
        public static readonly Plane3D Unity = new Plane3D(1, 1, 1, 1);
        #endregion
    }
}
