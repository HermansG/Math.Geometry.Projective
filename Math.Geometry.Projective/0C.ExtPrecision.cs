using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;

namespace Geometry.Projective
{
    using System.Numerics;
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Complex;

    /// <summary>
    /// Static methods, extension methods and constants for general use.
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// Any double value smaller then this value will be treated as zero.<para>
        /// Approximately 1E-12.</para><para>
        /// Note that other choices for this value may generate errors in the calculation strategies for homogeneous coordinates.</para>
        /// </summary>
        public static readonly double PrecisionZero = 1E4 * Precision.DoublePrecision;

        /// <summary>
        /// Any double value with an absolute value greater then this value will be treated as infinity.
        /// </summary>
        public static readonly double PrecisionInfinity = 1 / PrecisionZero;

        /// <summary>
        /// The maximum value used for homogeneous coordinates.<para>
        /// Larger values will be divided by a factor.</para>
        /// </summary>
        public static readonly double MaxHomogeneousValue = 10;

        /// <summary>
        /// Check whether the struct is equal to its default value.
        /// </summary>
        public static bool IsDefault<T>(this T value) where T : struct
        {
            return value.Equals(default(T));
        }

        /// <summary>
        /// Get the values set in an enum with a flag structure.
        /// </summary>
        public static List<Enum> GetFlags(Enum input)
        {
            List<Enum> rv = new List<Enum>();
            foreach (Enum value in Enum.GetValues(input.GetType()))
            {
                if (input.HasFlag(value))
                {
                    rv.Add(value);
                }
            }
            return rv;
        }

        /// <summary>
        /// The en-Us culture.
        /// </summary>
        public static CultureInfo EnUsCulture = CultureInfo.GetCultureInfo("en-US");

        /// <summary>
        /// Converts an IEnumerable of doubles into an IEnumerable of complex numbers.
        /// </summary>
        public static IEnumerable<Complex> ToComplex(this IEnumerable<double> values)
        {
            if (values == null) throw new ArgumentNullException("array");
            return values.Select(i => (Complex)i);
        }

        /// <summary>
        /// Converts an array of doubles into an array of complex numbers.
        /// </summary>
        public static Complex[] ToComplex(this double[] values)
        {
            if (values == null) throw new ArgumentNullException("array");
            return Array.ConvertAll(values, x => (Complex)x);
        }

        /// <summary>
        /// Check whether all values are valid.
        /// </summary>
        public static bool IsValid(this Complex[] values, bool coerce = true)
        {
            foreach (var value in values)
            {
                if (!value.IsValid()) return false;
            }
            return true;
        }

        /// <summary>
        /// Check whether the complex value is valid.
        /// </summary>
        public static bool IsValid(this Complex value)
        {
            if (value.IsNaN()) return false;
            if (value.Norm() >= PrecisionInfinity) return false;
            return true;
        }

        /// <summary>
        /// Check whether all values are real and valid.
        /// </summary>
        public static bool IsReal(this Complex[] values, bool coerce = true)
        {
            if (!values.IsValid(coerce)) return false;

            foreach (var value in values)
            {
                if (!value.Imaginary.IsZero()) return false;
            }
            return true;
        }

        /// <summary>
        /// Check whether two double numbers are equal within a certain precision.
        /// </summary>
        public static bool EqualsWithinPrecision(this double number, double other)
        {
            return Math.Abs(number - other) < PrecisionZero;
        }

        /// <summary>
        /// Check whether two complex numbers are equal within a certain precision.
        /// </summary>
        public static bool EqualsWithinPrecision(this Complex number, Complex other)
        {
            return number.Real.EqualsWithinPrecision(other.Real) &&
                   number.Imaginary.EqualsWithinPrecision(other.Imaginary);
        }

        /// <summary>
        /// Check whether two complex vectors are equal within a certain precision.
        /// </summary>
        public static bool EqualsWithinPrecision(this Vector<Complex> vector, Vector<Complex> other)
        {
            if (vector.Count != other.Count)
            {
                throw new ArgumentException("Two vectors of different length");
            }
            for (int i = 0; i < vector.Count; i++)
            {
                if (!vector[i].EqualsWithinPrecision(other[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// Check whether the matrix has all entries equal to zero within a certain precision.
        /// </summary>
        public static bool IsZero(this Matrix<Complex> matrix)
        {
            for (int i = 0; i < matrix.RowCount; i++)
            {
                for (int j = 0; j < matrix.ColumnCount; j++)
                {
                    if (!matrix[i, j].IsZero()) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check whether the vector has all coordinates equal to zero within a certain precision.
        /// </summary>
        public static bool IsZero(this Vector<Complex> vector)
        {
            for (int i = 0; i < vector.Count; i++)
            {
                if (!vector[i].IsZero()) return false;
            }
            return true;
        }

        /// <summary>
        /// Check whether the collection has all coordinates equal to zero within a certain precision.
        /// </summary>
        public static bool AllZero(this IEnumerable<Complex> array)
        {
            foreach (var item in array)
            {
                if (!item.IsZero()) return false;
            }
            return true;
        }

        /// <summary>
        /// Check whether the collection has all coordinates equal to zero within a certain precision.
        /// </summary>
        public static bool AllZero(this IEnumerable<double> array)
        {
            foreach (var item in array)
            {
                if (!item.IsZero()) return false;
            }
            return true;
        }

        /// <summary>
        /// Check whether the real and imaginary part of a complex number are equal to zero within a certain precision.
        /// </summary>
        public static bool IsZero(this Complex complex)
        {
            return complex.Real.IsZero() && complex.Imaginary.IsZero();
        }

        /// <summary>
        /// Check whether the real number is equal to zero within a certain precision.
        /// </summary>
        public static bool IsZero(this double real)
        {
            return Math.Abs(real) <= PrecisionZero;
        }

        /// <summary>
        /// The sum of the absolute value of the real part and the absolute value of the imaginary part.
        /// </summary>
        public static double AbsoluteSum(this Complex complex)
        {
            return Math.Abs(complex.Real) + Math.Abs(complex.Imaginary);
        }

        /// <summary>
        /// Consider the vector to be a homogeneous vector and simplify it.<para>
        /// When the vector is homogeneous real, its values will be made real.</para><para>
        /// A homogeneous factor will be applied to prevent the coordinates from becoming too large.</para><para>
        /// Values of the vector will be coerced to a whole number, a simple fraction or 0 within a certain precision.</para>
        /// </summary>
        public static void CoerceHomogeneousCoordinates(this Vector<Complex> vector)
        {
            if (vector.IsHomogeneousReal())
            {
                // a real homogeneous vector can be simplified as follows
                if (vector.Select(v => v.Real).Any(d => Math.Abs(d) > PrecisionZero))
                {
                    for (int i = 0; i < vector.Count; i++)
                    {
                        vector[i] = new Complex(vector[i].Real, 0);
                    }
                }
                else
                {
                    for (int i = 0; i < vector.Count; i++)
                    {
                        vector[i] = new Complex(vector[i].Imaginary, 0);
                    }
                }
            }

            double maxreal = vector.Select(c => Math.Abs(c.Real)).Max();
            double maximaginary = vector.Select(c => Math.Abs(c.Imaginary)).Max();

            if (maxreal >= MaxHomogeneousValue || maximaginary >= MaxHomogeneousValue)
            {
                double factor = MaxHomogeneousValue / (3 * Math.Max(maxreal, maximaginary));
                for (int i = 0; i < vector.Count; i++)
                {
                    vector[i] = new Complex(vector[i].Real * factor, vector[i].Imaginary * factor);
                }
            }

            for (int i = 0; i < vector.Count; i++)
            {
                double? roundedReal = null;
                double ceiling = Math.Ceiling(1000 * vector[i].Real) / 1000;
                double floor = Math.Floor(1000 * vector[i].Real) / 1000;
                if (vector[i].Real.EqualsWithinPrecision(ceiling))
                {
                    if (vector[i].Real != ceiling)
                    {
                        roundedReal = ceiling;
                    }
                }
                else if (vector[i].Real.EqualsWithinPrecision(floor))
                {
                    if (vector[i].Real != floor)
                    {
                        roundedReal = floor;
                    }
                }
                double? roundedImaginary = null;
                ceiling = Math.Ceiling(1000 * vector[i].Imaginary) / 1000;
                floor = Math.Floor(1000 * vector[i].Imaginary) / 1000;
                if (vector[i].Imaginary.EqualsWithinPrecision(ceiling))
                {
                    if (vector[i].Imaginary != ceiling)
                    {
                        roundedImaginary = ceiling;
                    }
                }
                else if (vector[i].Imaginary.EqualsWithinPrecision(floor))
                {
                    if (vector[i].Imaginary != floor)
                    {
                        roundedImaginary = floor;
                    }
                }
                if (roundedReal.HasValue || roundedImaginary.HasValue)
                {
                    vector[i] = new Complex(roundedReal.HasValue ? roundedReal.Value : vector[i].Real,
                                            roundedImaginary.HasValue ? roundedImaginary.Value : vector[i].Imaginary);
                }
            }
        }

        /// <summary>
        /// Consider the vector to be a homogeneous vector and check whether it represents a real homogeneous vector.
        /// </summary>
        public static bool IsHomogeneousReal(this Vector<Complex> vector)
        {
            for (int i = 0; i < vector.Count; i++)
            {
                for (int j = i + 1; j < vector.Count; j++)
                {
                    if (!(vector[i] * vector[j].Conjugate()).EqualsWithinPrecision((vector[j] * vector[i].Conjugate())))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Forces small numbers near zero to zero.<para>
        /// The Math.Net library does not coerce e.g. 1e-15 +2*i to 2*i because it looks at the magnitude.</para>
        /// </summary>
        public static Complex CoerceZero(this Complex complex)
        {
            return new Complex(complex.Real.CoerceZero(PrecisionZero), complex.Imaginary.CoerceZero(PrecisionZero));
        }

        /// <summary>
        /// Forces small numbers near zero to zero.
        /// </summary>
        public static void CoerceZero(this Complex[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = array[i].CoerceZero();
            }
        }

        /// <summary>
        /// Forces small numbers near zero to zero.
        /// </summary>
        public static void CoerceZero(this Vector<Complex> vector)
        {
            for (int i = 0; i < vector.Count; i++)
            {
                vector[i] = vector[i].CoerceZero();
            }
        }

        /// <summary>
        /// Forces small numbers near zero to zero.
        /// </summary>
        public static void CoerceZero(this Matrix<Complex> matrix)
        {
            for (int i = 0; i < matrix.RowCount; i++)
            {
                for (int j = 0; j < matrix.ColumnCount; j++)
                {
                    matrix.At(i, j, matrix.At(i, j).CoerceZero());
                }
            }
        }

        /// <summary>
        /// A string representation of an array of complex numbers in the form (a+bi, c+di, ..)
        /// </summary>
        public static string ToVectorString(this Complex[] array)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("(");
            for (int i = 0; i < array.Length; i++)
            {
                builder.Append(array[i].ToString()).Append(i != array.Length - 1 ? ", " : ")");
            }
            return builder.ToString();
        }

        /// <summary>
        /// Give a string representation of a complex number in the form a+bI.
        /// </summary>
        public static string ToComplexString(this Complex value)
        {
            var plus = value.Imaginary < 0 ? " " : " + ";
            return string.Concat(value.Real.ToString("N1", EnUsCulture), plus, value.Imaginary.ToString("N1", EnUsCulture), " I");
        }

        /// <summary>
        /// Check whether a number is between two bounds, inclusivley.
        /// </summary>
        public static bool Between(double bound1, double bound2, double value)
        {
            if (value >= bound1 && value <= bound2)
            {
                return true;
            }
            if (value >= bound2 && value <= bound1)
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// An algorithm failed unexpectedly.
    /// </summary>
    public class AlgorithmException : Exception
    {
        /// <summary>
        /// An algorithm failed unexpectedly.
        /// </summary>
        public AlgorithmException(string message) : base(message) { }
    }
}
