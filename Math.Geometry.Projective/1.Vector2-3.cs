﻿using System;
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
    public class VectorC2
    {
        #region constructors
        /// <summary>
        /// The values are copied into the data of the new vector.
        /// </summary>
        public VectorC2(Complex[] values)
        {
            if (values == null) throw new ArgumentNullException("values");
            vector = DenseVector.OfEnumerable(values);
            init();
        }
        /// <summary>
        /// The values are copied into the data of the new vector.
        /// </summary>
        public VectorC2(double[] values)
        {
            if (values == null) throw new ArgumentNullException("values");
            vector = DenseVector.OfEnumerable(values.ToComplex());
            init();
        }
        /// <summary>
        /// The vector data are copied into the data of the new vector.
        /// </summary>
        public VectorC2(DenseVector vector)
        {
            if (vector == null) throw new ArgumentNullException("vector");
            this.vector = DenseVector.OfVector(vector);
            init();
        }
        /// <summary>
        /// The vector data are copied into the data of the new vector.<para>
        /// Operators in MathNet.Numerics.LinearAlgebra.Complex.DenseVector return the abstract type Vector[Complex]</para>
        /// </summary>
        VectorC2(Vector<Complex> vector)
        {
            if (vector == null) throw new ArgumentNullException("vector");
            this.vector = DenseVector.OfVector(vector);
            init();
        }
        /// <summary>
        /// The data are coordinates of the new vector.
        /// </summary>
        public VectorC2(Complex x, Complex y)
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
        public VectorC2 Clone()
        {
            return new VectorC2(vector);
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
        public static VectorC2 operator -(VectorC2 value)
        {
            if (value == null) throw new ArgumentNullException("value");
            return -1 * value;
        }
        /// <summary>
        /// The inner- or dot-product of two 2-dimensional vectors.
        /// </summary>
        public static Complex operator *(VectorC2 left, VectorC2 right)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");
            return left.vector * right.vector;
        }
        /// <summary>
        /// The product of the vector with a complex (or real) number.
        /// </summary>
        public static VectorC2 operator *(Complex left, VectorC2 right)
        {
            if (right == null) throw new ArgumentNullException("right");
            return new VectorC2(left * right.vector);
        }
        /// <summary>
        /// The product of the vector with a complex (or real) number.
        /// </summary>
        public static VectorC2 operator *(VectorC2 left, Complex right)
        {
            if (left == null) throw new ArgumentNullException("left");
            return new VectorC2(right * left.vector);
        }
        /// <summary>
        /// The difference of two vectors.
        /// </summary>
        public static VectorC2 operator -(VectorC2 left, VectorC2 right)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");
            return new VectorC2(left.vector - right.vector);
        }
        /// <summary>
        /// The sum of two vectors.
        /// </summary>
        public static VectorC2 operator +(VectorC2 left, VectorC2 right)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");
            return new VectorC2(left.vector + right.vector);
        }
        #endregion

        /// <summary>
        /// Check whether this and another vector have equal coordinates within a certain precision.
        /// </summary>
        public bool Equals(VectorC2 other)
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
        public Complex CrossProduct(VectorC2 other)
        {
            return this[0] * other[1] - this[1] * other[0];
        }

        /// <summary>
        /// Calculate the normalized vector with unit length.<para>
        /// A null value is returned when the vector is zero.</para>
        /// </summary>
        public VectorC2 Normalize()
        {
            if (vector.IsZero()) return null;
            return new VectorC2(vector.Normalize(2));
        }

        /// <summary>
        /// The vector is interpreted as a directional vector and the perpendicular vector (90 degree turn) is returned.<para>
        /// When the vector is zero a zero vector is returned</para>
        /// </summary>
        public VectorC2 Perpendicular()
        {
            return new VectorC2(-vector[1], vector[0]);
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
        public double Distance(VectorC2 other) { return (this - other).Norm(); }

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
        public static readonly VectorC2 Origin = new VectorC2(0, 0);
        /// <summary>
        /// (1 0)
        /// </summary>
        public static readonly VectorC2 EX = new VectorC2(1, 0);
        /// <summary>
        /// (0 1)
        /// </summary>
        public static readonly VectorC2 EY = new VectorC2(0, 1);
        /// <summary>
        /// (1 1)
        /// </summary>
        public static readonly VectorC2 E = new VectorC2(1, 1);
        #endregion
    }

    /// <summary>
    /// Any non-homogeneous vector with 3 complex (or real) coordinates.<para>
    /// E.g. a free vector, a position vector, a polar or axial vector in 3-dimensional affine or euclidean space.</para>
    /// </summary>
    public class VectorC3
    {
        #region constructors
        /// <summary>
        /// The values are copied into the data of the new vector.
        /// </summary>
        public VectorC3(Complex[] values)
        {
            if (values == null) throw new ArgumentNullException("values");
            vector = DenseVector.OfEnumerable(values);
            init();
        }
        /// <summary>
        /// The values are copied into the data of the new vector.
        /// </summary>
        public VectorC3(double[] values)
        {
            if (values == null) throw new ArgumentNullException("values");
            vector = DenseVector.OfEnumerable(values.ToComplex());
            init();
        }
        /// <summary>
        /// The vector data are copied into the data of the new vector.
        /// </summary>
        public VectorC3(DenseVector vector)
        {
            if (vector == null) throw new ArgumentNullException("vector");
            this.vector = DenseVector.OfVector(vector);
            init();
        }
        /// <summary>
        /// The vector data are copied into the data of the new vector.<para>
        /// Operators in MathNet.Numerics.LinearAlgebra.Complex.DenseVector return the abstract type Vector[Complex]</para>
        /// </summary>
        VectorC3(Vector<Complex> vector)
        {
            if (vector == null) throw new ArgumentNullException("vector");
            this.vector = DenseVector.OfVector(vector);
            init();
        }
        /// <summary>
        /// The data are coordinates of the new vector.
        /// </summary>
        public VectorC3(Complex x, Complex y, Complex z)
        {
            vector = new DenseVector(new Complex[] { x, y, z });
            init();
        }
        /// <summary>
        /// The data are coordinates of the new vector.
        /// </summary>
        public VectorC3(double x, double y, double z)
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
        public VectorC3 Clone()
        {
            return new VectorC3(vector);
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
        public static VectorC3 operator -(VectorC3 rightSide)
        {
            if (rightSide == null) throw new ArgumentNullException("rightside");
            return -1 * rightSide;
        }
        /// <summary>
        /// The inner- or dot-product of two 3-dimensional vectors.
        /// </summary>
        public static Complex operator *(VectorC3 left, VectorC3 right)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");
            return left.vector * right.vector;
        }
        /// <summary>
        /// The product of the vector with a complex (or real) number.
        /// </summary>
        public static VectorC3 operator *(Complex left, VectorC3 right)
        {
            if (right == null) throw new ArgumentNullException("right");
            return new VectorC3(left * right.vector);
        }
        /// <summary>
        /// The product of the vector with a complex (or real) number.
        /// </summary>
        public static VectorC3 operator *(VectorC3 left, Complex right)
        {
            if (left == null) throw new ArgumentNullException("left");
            return new VectorC3(right * left.vector);
        }
        /// <summary>
        /// The difference of two vectors.
        /// </summary>
        public static VectorC3 operator -(VectorC3 left, VectorC3 right)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");
            return new VectorC3(left.vector - right.vector);
        }
        /// <summary>
        /// The sum of two vectors.
        /// </summary>
        public static VectorC3 operator +(VectorC3 left, VectorC3 right)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");
            return new VectorC3(left.vector + right.vector);
        }
        #endregion

        /// <summary>
        /// Check whether this and another vector have equal coordinates within a certain precision.
        /// </summary>
        public bool Equals(VectorC3 other)
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
        public VectorC3 CrossProduct(VectorC3 other)
        {
            if (other == null) throw new ArgumentNullException("other");
            Complex[] values = new Complex[3];
            values[0] = this[1] * other[2] - this[2] * other[1];
            values[1] = this[2] * other[0] - this[0] * other[2];
            values[2] = this[0] * other[1] - this[1] * other[0];
            return new VectorC3(values);
        }

        /// <summary>
        /// Check whether two vectors are parallel.
        /// </summary>
        public bool IsParallel(VectorC3 other)
        {
            return CrossProduct(other).IsZero();
        }

        /// <summary>
        /// Calculate the normalized vector with unit length.<para>
        /// A null value is returned when the vector is zero.</para>
        /// </summary>
        public VectorC3 Normalize()
        {
            if (vector.IsZero()) return null;
            return new VectorC3(vector.Normalize(2));
        }

        /// <summary>
        /// Calculates the euclidean norm of the vector.<para>
        /// The root of the sum of squares of the absolute value of each coordinate.</para>
        /// </summary>
        public double Norm() { return vector.L2Norm(); }

        /// <summary>
        /// The two vectors are considered to be spatial positions; calcuate their Euclidean distance.
        /// </summary>
        public Complex Distance(VectorC3 other)
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
        public static readonly VectorC3 Origin = new VectorC3(0, 0, 0);
        /// <summary>
        /// (0 0 0)
        /// </summary>
        public static readonly VectorC3 Zero = new VectorC3(0, 0, 0);
        /// <summary>
        /// (1 0 0)
        /// </summary>
        public static readonly VectorC3 EX = new VectorC3(1, 0, 0);
        /// <summary>
        /// (0 1 0)
        /// </summary>
        public static readonly VectorC3 EY = new VectorC3(0, 1, 0);
        /// <summary>
        /// (0 0 1)
        /// </summary>
        public static readonly VectorC3 EZ = new VectorC3(0, 0, 1);
        /// <summary>
        /// (1 1 1)
        /// </summary>
        public static readonly VectorC3 E = new VectorC3(1, 1, 1);
        #endregion
    }
}
