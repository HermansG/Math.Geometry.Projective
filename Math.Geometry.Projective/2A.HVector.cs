using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Geometry.Projective
{
    using System.Numerics;
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Complex;

    /// <summary>
    /// A homogeneous vector with at least two homogeneous coordinates (complex or real) using dense storage.<para>
    /// When all coordinates are 0, or one of the coordinates is NaN, the vector is invalid.</para><para>
    /// The coordinates are immutable.</para>
    /// </summary>
    public class HVector
    {
        #region constructors
        /// <summary>
        /// The values are copied into the data of the new vector.<para>
        /// At least two coordinates are required and the zero vector is forbidden.</para>
        /// </summary>
        public HVector(IEnumerable<Complex> values)
        {
            check(values);
            vector = DenseVector.OfEnumerable(values);
            initialize();
        }
        /// <summary>
        /// The values are copied into the data of the new vector.<para>
        /// At least two coordinates are required and the zero vector is forbidden.</para>
        /// </summary>
        public HVector(IEnumerable<double> values)
        {
            check(values.ToComplex());
            vector = DenseVector.OfEnumerable(values.ToComplex());
            initialize();
        }
        /// <summary>
        /// The vector data are copied into the data of the new vector.<para>
        /// Operators in MathNet.Numerics.LinearAlgebra.Complex.DenseVector return the abstract type Vector[Complex]</para><para>
        /// At least two coordinates are required and the zero vector is forbidden.</para>
        /// </summary>
        protected HVector(Vector<Complex> vector)
        {
            check(vector);
            this.vector = DenseVector.OfVector(vector);
            initialize();
        }
        /// <summary>
        /// The vector data are copied into the data of the new vector.<para>
        /// At least two coordinates are required and the zero vector is forbidden.</para>
        /// </summary>
        public HVector(HVector hvector)
        {
            check(hvector.vector);
            vector = DenseVector.OfVector(hvector.vector);
            initialize();
        }
        /// <summary>
        /// The vector data are copied into the data of the new vector.<para>
        /// At least two coordinates are required and the zero vector is forbidden.</para>
        /// </summary>
        public HVector(params Complex[] values)
        {
            check(values);
            vector = DenseVector.OfEnumerable(values);
            initialize();
        }
        void check(IEnumerable<Complex> values)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (!values.Any()) throw new ArgumentException("values is empty");
            if (values.Count() == 1) throw new ArgumentException("at least two coordinates are required");
            if (values.AllZero()) throw new ArgumentException("zero vector is invalid.");
        }
        void initialize()
        {
            conjugate = new Lazy<HVector>(() => new HVector(vector.Conjugate()));
            vector.CoerceHomogeneousCoordinates();
        }
        #endregion

        /// <summary>
        /// Get a copy of the data of this hvector.
        /// </summary>
        public HVector Clone() { return new HVector(this); }

        /// <summary>
        /// The internal vector data.
        /// </summary>
        protected DenseVector vector;
        /// <summary>
        /// The read-only coordinate at position 'index'.
        /// </summary>
        public Complex this[int index] { get { return vector[index]; } }

        #region operators
        // Addition or multiplcation of homogeneous vecotrs is not allowd because the data in the vector are arbitrary up to a complex (nonzero) factor.
        #endregion

        /// <summary>
        /// The number of coordinates of the homogeneous vector.<para>
        /// Note that the spatial dimension is one less than this value.</para>
        /// </summary>
        public int Count { get { return vector.Count; } }

        /// <summary>
        /// Determines whether all coordinates of this homogeneous vector are zero with respect to a certain precision.
        /// </summary>
        public bool IsZero() { return vector.IsZero(); }

        /// <summary>
        /// Get a copy of the raw vector.
        /// </summary>
        public Vector<Complex> ToVector() { return vector.Clone(); }

        /// <summary>
        /// Get a copy of the data of this homogeneous vector.
        /// </summary>
        public Complex[] ToArray() { return vector.ToArray(); }

        /// <summary>
        /// Multiply the homogeneous vector with a homogeneous matrix.<para>
        /// I.e. (matrix) * (vector).</para><para>
        /// The matrix must be a square matrix of the same dimension as the homogeneous vector.</para><para>
        /// In case of a zero matrix the return value will be 'null'.</para>
        /// </summary>
        public Vector<Complex> Multiply(Matrix<Complex> matrix)
        {
            if (matrix.RowCount != matrix.ColumnCount)
            {
                throw new ArgumentException("only square matrices are allowed");
            }
            if (matrix.RowCount != Count)
            {
                throw new ArgumentException("the matrix must be of the same dimension as the homogeneous vector");
            }

            var rv = matrix.Multiply(vector);

            rv.CoerceHomogeneousCoordinates();

            if (rv.IsZero()) return null;
            return rv;
        }

        Lazy<HVector> conjugate;
        /// <summary>
        /// The homogeneous vector with complex conjugate coordinates.
        /// </summary>
        public HVector Conjugate { get { return conjugate.Value; } }

        /// <summary>
        /// Determines whether this homogeneous vector is incident with another - dual - homogeneous vector.
        /// </summary>
        public bool IsIncident(HVector other)
        {
            if (other == null) throw new ArgumentNullException("other");
            if (Count != other.Count) throw new ArgumentException("different dimensions");

            // the two homogeneous vectors should be dual to each other, but it is not enforced
            var product = this.ToVector() * other.ToVector();
            return product.IsZero();
        }

        /// <summary>
        /// Compare two homogeneous vectors on the basis of their homogeneous coordinates.<para>
        /// When one of the two hvectors is invalid the result is false.</para>
        /// </summary>
        public bool Equals(HVector other)
        {
            if (other == null) throw new ArgumentNullException("other");
            return vector.Values.LinearDependant(other.vector.Values) != null;
        }

        /// <summary>
        /// Check whether the homogenous coordinates represent a real homogeneous vector.
        /// </summary>
        public bool IsReal()
        {
            return vector.IsHomogeneousReal();
        }

        /// <summary>
        /// Calculate a random HVector of the same dimension, incident with this Hvector.<para>
        /// Optionally excluding a number of known HVectors, optionally complex.</para><para>
        /// Optionally specify an index that may not be zero.</para><para>
        /// (A 1-dimensional element has exactly one incident object and the list of excluded HVectors is ignored.)</para>
        /// </summary>
        public HVector GetRandomIncident(bool real = true, params HVector[] exclude)
        {
            if (exclude == null)
            {
                return GetRandomIncident(real);
            }
            else
            {
                return GetRandomIncident(real, exclude.ToList());
            }
        }

        /// <summary>
        /// Calculate a random HVector of the same dimension, incident with this Hvector.<para>
        /// Optionally excluding a list of known HVectors, optionally complex.</para><para><para>
        /// Optionally specify an index that may not be zero.</para>
        /// (A 1-dimensional element has exactly one incident object and the list of excluded HVectors is ignored.)</para>
        /// </summary>
        public HVector GetRandomIncident(bool real = true, IEnumerable<HVector> exclude = null)
        {
            var condition = new Func<HVector, bool>((v) => exclude == null ? true : exclude.All(ex => !ex.Equals(v)));

            DenseVector search = new DenseVector(Count);

            var allindices = Enumerable.Range(0, Count).ToList();
            var zerosindices = allindices.Where(i => vector[i].IsZero()).ToList();
            var nonzerosindices = allindices.Except(zerosindices).ToList();

            if (nonzerosindices.Count == 0)
            {
                throw new AlgorithmException("Unexpected and illegal: HVector is zero");
            }

            // a special case (a, b)
            if (Count == 2)
            {
                return new HVector(-vector[1], vector[0]);
            }

            // another special case (0, x, 0, 0) => (a, 0, b, c)
            if (nonzerosindices.Count == 1)
            {
                // note that zerosindices.Count > 0
                int indexNotzero = zerosindices.Count == 1 ? -1 : zerosindices[Extensions.PickRandom(0, zerosindices.Count)];

                foreach (var index in zerosindices)
                {
                    if (real)
                    {
                        if (indexNotzero == index)
                        {
                            search[index] = new Complex(Extensions.Numbersnotincludingzero.PickRandom(), 0);
                        }
                        else
                        {
                            search[index] = new Complex(Extensions.Numbersincludingmanyzeros.PickRandom(), 0);
                        }
                    }
                    else
                    {
                        if (indexNotzero == index)
                        {
                            search[index] = new Complex(Extensions.Numbersnotincludingzero.PickRandom(), Extensions.Numbersincludingmanyzeros.PickRandom());
                        }
                        else
                        {
                            search[index] = new Complex(Extensions.Numbersincludingmanyzeros.PickRandom(), Extensions.Numbersincludingmanyzeros.PickRandom());
                        }
                    }
                }

                HVector result = new HVector(search);

                while (!condition(result))
                {
                    int index = zerosindices[Extensions.PickRandom(0, zerosindices.Count)];
                    result.vector[index] += 1;
                    if (!real)
                    {
                        result.vector[index] += Complex.ImaginaryOne * Extensions.PickRandomSign();
                    }
                    if (result.IsZero())
                    {
                        result.vector[index] += 1;
                        if (!real)
                        {
                            result.vector[index] += Complex.ImaginaryOne * Extensions.PickRandomSign();
                        }
                    }
                }

                foreach (var item in exclude)
                {
                    if (result.Equals(item)) { }
                }

                result.vector.CoerceHomogeneousCoordinates();
                return result;
            }

            // we have at least 2 non-zeros (.. x, .., y ..) and at least 3 coordinates e.g. (0 .. -y, 0 .. , x, 0.. ) would be ok
            else
            {
                int indexExclude = nonzerosindices[Extensions.PickRandom(0, nonzerosindices.Count)];
                var otherindices = allindices.Where(i => i != indexExclude).ToList();
                foreach (var index in otherindices)
                {
                    if (real)
                    {
                        search[index] = new Complex(Extensions.Numbersincludingmanyzeros.PickRandom(), 0);
                    }
                    else
                    {
                        search[index] = new Complex(Extensions.Numbersincludingmanyzeros.PickRandom(), Extensions.Numbersincludingmanyzeros.PickRandom());
                    }
                }
                search[indexExclude] = -(search * this.vector) / this[indexExclude];

                if (search.IsZero())
                {
                    int index = otherindices[Extensions.PickRandom(0, otherindices.Count)];
                    search[index] += 1;
                    if (!real)
                    {
                        search[index] += Complex.ImaginaryOne * Extensions.PickRandomSign();
                    }
                    search[indexExclude] = 0;
                    search[indexExclude] = -(search * this.vector) / this[indexExclude];
                }

                HVector result = new HVector(search);

                while (!condition(result))
                {
                    int index = otherindices[Extensions.PickRandom(0, otherindices.Count)];
                    result.vector[index] += 1;
                    if (!real)
                    {
                        result.vector[index] += Complex.ImaginaryOne * Extensions.PickRandomSign();
                    }
                    result.vector[indexExclude] = 0;
                    result.vector[indexExclude] = -(result.ToVector() * this.ToVector()) / this[indexExclude];

                    if (result.IsZero())
                    {
                        index = otherindices[Extensions.PickRandom(0, otherindices.Count)];
                        result.vector[index] += 1;
                        if (!real)
                        {
                            result.vector[index] += Complex.ImaginaryOne * Extensions.PickRandomSign();
                        }
                        result.vector[indexExclude] = 0;
                        result.vector[indexExclude] = -(result.ToVector() * this.ToVector()) / this[indexExclude];
                    }
                }

                foreach (var item in exclude)
                {
                    if (result.Equals(item)) { }
                }

                result.vector.CoerceHomogeneousCoordinates();
                return result;
            }
        }

        /// <summary>
        /// A string representation of the type and values of the vector in the form (a+bi, c+di, ..)
        /// </summary>
        public override string ToString()
        {
            return string.Concat(string.Format("{0} {1}-{2} {3}", GetType().Name, Count, "Complex", Environment.NewLine), vector.ToVectorString());
        }

        /// <summary>
        /// Possibly a name for the object.
        /// </summary>
        public string Name { get; set; }
    }
}
