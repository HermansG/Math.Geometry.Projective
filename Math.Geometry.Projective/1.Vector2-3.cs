using System;
using System.Diagnostics;

namespace Geometry.Projective
{
    using System.Numerics;
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Complex;

    /// <summary>
    /// Any non-homogeneous vector with 2 complex (or real) coordinates.<para>
    /// E.g. a free vector or a position vector in 2-dimensional affine or euclidean space.</para>
    /// </summary>
    public class Vector2
    {
        #region constructors
        /// <summary>
        /// The values are copied into the data of the new vector.
        /// </summary>
        public Vector2(Complex[] values)
        {
            if (values == null) throw new ArgumentNullException("values");
            vector = DenseVector.OfEnumerable(values);
            init();
        }
        /// <summary>
        /// The values are copied into the data of the new vector.
        /// </summary>
        public Vector2(double[] values)
        {
            if (values == null) throw new ArgumentNullException("values");
            vector = DenseVector.OfEnumerable(values.ToComplex());
            init();
        }
        /// <summary>
        /// The vector data are copied into the data of the new vector.
        /// </summary>
        public Vector2(DenseVector vector)
        {
            if (vector == null) throw new ArgumentNullException("vector");
            this.vector = DenseVector.OfVector(vector);
            init();
        }
        /// <summary>
        /// The vector data are copied into the data of the new vector.<para>
        /// Operators in MathNet.Numerics.LinearAlgebra.Complex.DenseVector return the abstract type Vector[Complex]</para>
        /// </summary>
        Vector2(Vector<Complex> vector)
        {
            if (vector == null) throw new ArgumentNullException("vector");
            this.vector = DenseVector.OfVector(vector);
            init();
        }
        /// <summary>
        /// The data are coordinates of the new vector.
        /// </summary>
        public Vector2(Complex x, Complex y)
        {
            this.vector = new DenseVector(new Complex[] { x, y });
            init();
        }
        void init()
        {
            if (vector.Count != 2) throw new ArgumentOutOfRangeException("Vector2 has 2 coordinates.");
            vector.CoerceZero();
        }
        #endregion

        /// <summary>
        /// Get a copy of the data of this vector.
        /// </summary>
        public Vector2 Clone()
        {
            return new Vector2(vector);
        }

        /// <summary>
        /// The internal vector data.
        /// </summary>
        protected DenseVector vector;
        /// <summary>
        /// The coordinate at position 'index'.
        /// </summary>
        public Complex this[int index] { get { return vector[index]; } }

        #region operators
        /// <summary>
        /// The negative of the vector.
        /// </summary>
        public static Vector2 operator -(Vector2 rightSide)
        {
            if (rightSide == null) throw new ArgumentNullException("rightside");
            return -1 * rightSide;
        }
        /// <summary>
        /// The inner- or dot-product of two 2-dimensional vectors.
        /// </summary>
        public static Complex operator *(Vector2 leftSide, Vector2 rightSide)
        {
            if (leftSide == null) throw new ArgumentNullException("leftside");
            if (rightSide == null) throw new ArgumentNullException("rightside");
            return leftSide.vector * rightSide.vector;
        }
        /// <summary>
        /// The product of the vector with a complex (or real) number.
        /// </summary>
        public static Vector2 operator *(Complex leftSide, Vector2 rightSide)
        {
            if (rightSide == null) throw new ArgumentNullException("rightside");
            return new Vector2(leftSide * rightSide.vector);
        }
        /// <summary>
        /// The product of the vector with a complex (or real) number.
        /// </summary>
        public static Vector2 operator *(Vector2 leftSide, Complex rightSide)
        {
            if (leftSide == null) throw new ArgumentNullException("leftside");
            return new Vector2(rightSide * leftSide.vector);
        }
        /// <summary>
        /// The difference of two vectors.
        /// </summary>
        public static Vector2 operator -(Vector2 leftSide, Vector2 rightSide)
        {
            if (leftSide == null) throw new ArgumentNullException("leftside");
            if (rightSide == null) throw new ArgumentNullException("rightside");
            return new Vector2(leftSide.vector - rightSide.vector);
        }
        /// <summary>
        /// The sum of two vectors.
        /// </summary>
        public static Vector2 operator +(Vector2 leftSide, Vector2 rightSide)
        {
            if (leftSide == null) throw new ArgumentNullException("leftside");
            if (rightSide == null) throw new ArgumentNullException("rightside");
            return new Vector2(leftSide.vector + rightSide.vector);
        }
        #endregion

        /// <summary>
        /// Check whether this and another vector have equal coordinates within a certain precision.
        /// </summary>
        public bool Equals(Vector2 other)
        {
            if (other == null) throw new ArgumentNullException("other");
            var difference = vector - other.vector;
            return difference.IsZero();
        }

        /// <summary>
        /// Check whether this and another vector have equal coordinates within a certain precision.<para>
        /// Note that other.Equals(this) returns 'false' because 'this' is not a Vector[Conplex]</para>
        /// </summary>
        public bool Equals(Vector<Complex> other)
        {
            if (other == null) throw new ArgumentNullException("other");
            if (other.Count != 2) return false;
            var difference = vector - other;
            return difference.IsZero();
        }

        /// <summary>
        /// Calculate the cross-product of two 2-dimensional vectors ([0][1]-[1][0]).
        /// </summary>
        public Complex CrossProduct(Vector2 other)
        {
            return this[0] * other[1] - this[1] * other[0];
        }

        /// <summary>
        /// Calculate the normalized vector with unit length.<para>
        /// A null value is returned when the vector is zero.</para>
        /// </summary>
        public Vector2 Normalize()
        {
            if (vector.IsZero()) return null;
            return new Vector2(vector.Normalize(2));
        }

        /// <summary>
        /// The vector is interpreted as a directional vector and the perpendicular vector (90 degree turn) is returned.<para>
        /// When the vector is zero a zero vector is returned</para>
        /// </summary>
        public Vector2 Perpendicular()
        {
            return new Vector2(-vector[1], vector[0]);
        }

        /// <summary>
        /// Calculates the euclidean norm of the vector.<para>
        /// The root of the sum of squares of the absolute value of each coordinate.</para>
        /// </summary>
        public double Norm() { return vector.L2Norm(); }

        /// <summary>
        /// Interpret the vector as a point and calculate<para>
        /// the Euclidean distance to another point.</para>
        /// </summary>
        public double Distance(Vector2 other) { return (this - other).Norm(); }

        /// <summary>
        /// Check whether all coordinates of this vector are zero with respect to a certain precision.
        /// </summary>
        public bool IsZero() { return vector.IsZero(); }

        /// <summary>
        /// Check whether one of the coordinates is Infinity or NaN.
        /// </summary>
        public bool IsValid()
        {
            return vector.Values.IsValid();
        }

        /// <summary>
        /// Check whether all coordinates are valid and real.
        /// </summary>
        public bool IsReal()
        {
            return vector.Values.IsReal();
        }

        /// <summary>
        /// Get a copy of the raw vector.
        /// </summary>
        public Vector<Complex> ToVector() { return vector.Clone(); }

        /// <summary>
        /// Get a copy of the data of this vector.
        /// </summary>
        public Complex[] ToArray() { return vector.ToArray(); }

        /// <summary>
        /// The vector is interpreted as a postion vector in a plane and transformed into the corresponding 2D projective point.
        /// </summary>
        public Point2D ToPoint2D() { return new Point2D(this[0], this[1]); }

        /// <summary>
        /// The vector is interpreted as directional vector of a line in a plane and transformed into the corresponding 2D projective line at a complex "distance" from the origin.<para>
        /// The line at infinity is returned when the vector is zero.</para>
        /// </summary>
        public Line2D ToLine2D(Complex distance)
        {
            if (vector.IsZero()) return Line2D.Infinity;
            var point_offset = distance * Perpendicular().Normalize();
            var second_point = (point_offset + this).ToPoint2D();
            return second_point.Join(point_offset.ToPoint2D());
        }

        /// <summary>
        /// A string representation of the type of vector and the values of the vector in the form (a+bi, c+di, ..)
        /// </summary>
        public override string ToString()
        {
            return string.Concat(string.Format("{0} {1}-{2} {3}", GetType().Name, vector.Count, "Complex", Environment.NewLine), vector.ToVectorString());
        }

        /// <summary>
        /// Possibly a name for the object.
        /// </summary>
        public string Name { get; set; }

        #region constants
        /// <summary>
        /// (0 0)
        /// </summary>
        public static readonly Vector2 Origin = new Vector2(0, 0);
        /// <summary>
        /// (1 0)
        /// </summary>
        public static readonly Vector2 EX = new Vector2(1, 0);
        /// <summary>
        /// (0 1)
        /// </summary>
        public static readonly Vector2 EY = new Vector2(0, 1);
        /// <summary>
        /// (1 1)
        /// </summary>
        public static readonly Vector2 E = new Vector2(1, 1);
        #endregion
    }

    /// <summary>
    /// Any non-homogeneous vector with 3 complex (or real) coordinates.<para>
    /// E.g. a free vector, a position vector, a polar or axial vector in 3-dimensional affine or euclidean space.</para>
    /// </summary>
    public class Vector3
    {
        #region constructors
        /// <summary>
        /// The values are copied into the data of the new vector.
        /// </summary>
        public Vector3(Complex[] values)
        {
            if (values == null) throw new ArgumentNullException("values");
            vector = DenseVector.OfEnumerable(values);
            init();
        }
        /// <summary>
        /// The values are copied into the data of the new vector.
        /// </summary>
        public Vector3(double[] values)
        {
            if (values == null) throw new ArgumentNullException("values");
            vector = DenseVector.OfEnumerable(values.ToComplex());
            init();
        }
        /// <summary>
        /// The vector data are copied into the data of the new vector.
        /// </summary>
        public Vector3(DenseVector vector)
        {
            if (vector == null) throw new ArgumentNullException("vector");
            this.vector = DenseVector.OfVector(vector);
            init();
        }
        /// <summary>
        /// The vector data are copied into the data of the new vector.<para>
        /// Operators in MathNet.Numerics.LinearAlgebra.Complex.DenseVector return the abstract type Vector[Complex]</para>
        /// </summary>
        Vector3(Vector<Complex> vector)
        {
            if (vector == null) throw new ArgumentNullException("vector");
            this.vector = DenseVector.OfVector(vector);
            init();
        }
        /// <summary>
        /// The data are coordinates of the new vector.
        /// </summary>
        public Vector3(Complex x, Complex y, Complex z)
        {
            vector = new DenseVector(new Complex[] { x, y, z });
            init();
        }
        /// <summary>
        /// The data are coordinates of the new vector.
        /// </summary>
        public Vector3(double x, double y, double z)
        {
            vector = new DenseVector(new Complex[] { x, y, z });
            init();
        }
        void init()
        {
            if (vector.Count != 3) throw new ArgumentOutOfRangeException("Vector2 has 2 coordinates.");
            vector.CoerceZero();
        }
        #endregion

        /// <summary>
        /// Get a copy of the data of this vector.
        /// </summary>
        public Vector3 Clone()
        {
            return new Vector3(vector);
        }

        /// <summary>
        /// The internal vector data.
        /// </summary>
        protected DenseVector vector;
        /// <summary>
        /// The coordinate at position 'index'.
        /// </summary>
        public Complex this[int index] { get { return vector[index]; } }

        #region operators
        /// <summary>
        /// The negative of the vector.
        /// </summary>
        public static Vector3 operator -(Vector3 rightSide)
        {
            if (rightSide == null) throw new ArgumentNullException("rightside");
            return -1 * rightSide;
        }
        /// <summary>
        /// The inner- or dot-product of two 3-dimensional vectors.
        /// </summary>
        public static Complex operator *(Vector3 leftSide, Vector3 rightSide)
        {
            if (leftSide == null) throw new ArgumentNullException("leftside");
            if (rightSide == null) throw new ArgumentNullException("rightside");
            return leftSide.vector * rightSide.vector;
        }
        /// <summary>
        /// The product of the vector with a complex (or real) number.
        /// </summary>
        public static Vector3 operator *(Complex leftSide, Vector3 rightSide)
        {
            if (rightSide == null) throw new ArgumentNullException("rightside");
            return new Vector3(leftSide * rightSide.vector);
        }
        /// <summary>
        /// The product of the vector with a complex (or real) number.
        /// </summary>
        public static Vector3 operator *(Vector3 leftSide, Complex rightSide)
        {
            if (leftSide == null) throw new ArgumentNullException("leftside");
            return new Vector3(rightSide * leftSide.vector);
        }
        /// <summary>
        /// The difference of two vectors.
        /// </summary>
        public static Vector3 operator -(Vector3 leftSide, Vector3 rightSide)
        {
            if (leftSide == null) throw new ArgumentNullException("leftside");
            if (rightSide == null) throw new ArgumentNullException("rightside");
            return new Vector3(leftSide.vector - rightSide.vector);
        }
        /// <summary>
        /// The sum of two vectors.
        /// </summary>
        public static Vector3 operator +(Vector3 leftSide, Vector3 rightSide)
        {
            if (leftSide == null) throw new ArgumentNullException("leftside");
            if (rightSide == null) throw new ArgumentNullException("rightside");
            return new Vector3(leftSide.vector + rightSide.vector);
        }
        #endregion

        /// <summary>
        /// Check whether this and another vector have equal coordinates within a certain precision.
        /// </summary>
        public bool Equals(Vector3 other)
        {
            if (other == null) throw new ArgumentNullException("other");
            var difference = vector - other.vector;
            return difference.IsZero();
        }

        /// <summary>
        /// Check whether this and another vector have equal coordinates within a certain precision.<para>
        /// Note that other.Equals(this) returns 'false' because 'this' is not a Vector[Conplex]</para>
        /// </summary>
        public bool Equals(Vector<Complex> other)
        {
            if (other == null) throw new ArgumentNullException("other");
            if (other.Count != 3) return false;
            var difference = vector - other;
            return difference.IsZero();
        }

        /// <summary>
        /// Calculate the outer- or cross-product of two 3-dimensional vectors.
        /// </summary>
        public Vector3 CrossProduct(Vector3 other)
        {
            if (other == null) throw new ArgumentNullException("other");
            Complex[] values = new Complex[3];
            values[0] = this[1] * other[2] - this[2] * other[1];
            values[1] = this[2] * other[0] - this[0] * other[2];
            values[2] = this[0] * other[1] - this[1] * other[0];
            return new Vector3(values);
        }

        /// <summary>
        /// Check whether two vectors are parallel.
        /// </summary>
        public bool IsParallel(Vector3 other)
        {
            return CrossProduct(other).IsZero();
        }

        /// <summary>
        /// Calculate the normalized vector with unit length.<para>
        /// A null value is returned when the vector is zero.</para>
        /// </summary>
        public Vector3 Normalize()
        {
            if (vector.IsZero()) return null;
            return new Vector3(vector.Normalize(2));
        }

        /// <summary>
        /// Calculates the euclidean norm of the vector.<para>
        /// The root of the sum of squares of the absolute value of each coordinate.</para>
        /// </summary>
        public double Norm() { return vector.L2Norm(); }

        /// <summary>
        /// The two vectors are considered to be spatial positions; calcuate their Euclidean distance.
        /// </summary>
        public Complex Distance(Vector3 other)
        {
            return (this - other).Norm();
        }

        /// <summary>
        /// Check whether all coordinates of this vector are zero with respect to a certain precision.
        /// </summary>
        public bool IsZero() { return vector.IsZero(); }

        /// <summary>
        /// Check whether one of the coordinates is Infinity or NaN.
        /// </summary>
        public bool IsValid()
        {
            return vector.Values.IsValid();
        }

        /// <summary>
        /// Check whether all coordinates are valid and real.
        /// </summary>
        public bool IsReal()
        {
            return vector.Values.IsReal();
        }

        /// <summary>
        /// Get a copy of the raw vector.
        /// </summary>
        public Vector<Complex> ToVector() { return vector.Clone(); }

        /// <summary>
        /// Get a copy of the data of this vector.
        /// </summary>
        public Complex[] ToArray() { return vector.ToArray(); }

        /// <summary>
        /// The vector is interpreted as a postion vector and transformed into the corresponding projective point.
        /// </summary>
        public Point3D ToPoint() { return new Point3D(this[0], this[1], this[2]); }

        /// <summary>
        /// The vector is interpreted as normal to a plane and transformed to the corresponding projective plane at a complex "distance" from the origin.<para>
        /// A null value is returned when the vector is zero.</para>
        /// </summary>
        public Plane3D ToPlane(Complex distance)
        {
            if (vector.IsZero()) return null;
            return new Plane3D(-distance * vector.L2Norm(), this[0], this[1], this[2]);
        }

        /// <summary>
        /// A string representation of the type of vector and the values of the vector in the form (a+bi, c+di, ..)
        /// </summary>
        public override string ToString()
        {
            return string.Concat(string.Format("{0} {1}-{2} {3}", GetType().Name, vector.Count, "Complex", Environment.NewLine), vector.ToVectorString());
        }

        #region constants
        /// <summary>
        /// (0 0 0)
        /// </summary>
        public static readonly Vector3 Origin = new Vector3(0, 0, 0);
        /// <summary>
        /// (0 0 0)
        /// </summary>
        public static readonly Vector3 Zero = new Vector3(0, 0, 0);
        /// <summary>
        /// (1 0 0)
        /// </summary>
        public static readonly Vector3 EX = new Vector3(1, 0, 0);
        /// <summary>
        /// (0 1 0)
        /// </summary>
        public static readonly Vector3 EY = new Vector3(0, 1, 0);
        /// <summary>
        /// (0 0 1)
        /// </summary>
        public static readonly Vector3 EZ = new Vector3(0, 0, 1);
        /// <summary>
        /// (1 1 1)
        /// </summary>
        public static readonly Vector3 E = new Vector3(1, 1, 1);
        #endregion
    }
}
