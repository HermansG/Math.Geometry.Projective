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
        /// Calculate the inner- or dot-product of two arrays of equal length.
        /// </summary>
        public static Complex DotProduct(this Complex[] values, Complex[] other)
        {
            if (other == null) throw new ArgumentNullException("other");
            if (values.Length != other.Length) throw new ArgumentException("different dimensions");
            return values.Zip(other, (x, y) => x * y).Aggregate((x, y) => x + y);
        }

        /// <summary>
        /// Calculate the complex factor f so that array B = f * array A.<para>
        /// When the arrays A and B are not linear dependent the return value is 'null'.</para><para>
        /// Zero arrays or invalid arrays will also return 'null'.</para>
        /// </summary>
        public static Complex? LinearDependant(this Complex[] A, Complex[] B)
        {
            if (B == null) throw new ArgumentNullException("other");
            if (A.Length != B.Length) throw new ArgumentException("different dimensions");
            if (A.Length == 0) throw new ArgumentException("empty array");
            if (A.AllZero() || B.AllZero()) return null;
            if (!A.IsValid() || !B.IsValid()) return null;
            if (A == B) return 1;

            Complex? factor = null;
            for (var index = 0; index < A.Length; index++)
            {
                if (A[index].IsZero() && B[index].IsZero()) continue;
                if (A[index].IsZero() || B[index].IsZero()) return null;
                if (factor == null)
                {
                    factor = B[index] / A[index];
                }
                else
                {
                    var difference = factor.Value * A[index] - B[index];
                    if (!difference.IsZero()) return null;
                }
            }
            return factor.Value;
        }

        /// <summary>
        /// Calculate the factors a, b so that C = a * A + b * B.<para>
        /// When the Hvectors A, B and C are not linear dependent (i.e. not collinear) the return value is 'null'.</para>
        /// </summary>
        public static Complex[] LinearDependentFactors(HVector A, HVector B, HVector C)
        {
            var a = A.ToVector();
            var b = B.ToVector();
            var c = C.ToVector();

            var matrix = Matrix<Complex>.Build.DenseOfColumnVectors(a, b);

            var factors = matrix.Solve(c).ToArray();

            var vector = factors[0] * a + factors[1] * b;

            if (c.EqualsWithinPrecision(vector))
            {
                return factors.ToArray();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Calculate the factor f so that B = f * A.<para>
        /// When the 3D-vectors A and B are not linear dependent the return value is 'null'.</para>
        /// </summary>
        public static Complex? LinearDependentFactor(Vector3 A, Vector3 B)
        {
            return LinearDependant(A.ToArray(), B.ToArray());
        }

        /// <summary>
        /// Calculate the cross product from 2 vectors with each 3 entries.
        /// </summary>
        public static Complex[] CrossProduct(Complex[] first, Complex[] second)
        {
            Complex[] values = new Complex[3];
            values[0] = first[1] * second[2] - first[2] * second[1];
            values[1] = first[2] * second[0] - first[0] * second[2];
            values[2] = first[0] * second[1] - first[1] * second[0];
            return values;
        }

        /// <summary>
        /// Calculate the Plücker coordinates (p01, p02, p03, p23, p31, p12) from 2 vectors with each 4 entries.
        /// </summary>
        public static Vector<Complex> PlueckerProduct(Vector<Complex> first, Vector<Complex> second)
        {
            if (first == null) throw new ArgumentNullException("first");
            if (second == null) throw new ArgumentNullException("second");
            if (first.Count != 4 || second.Count != 4) throw new ArgumentException("4 entries required");

            Complex[] array = new Complex[6];

            array[0] = first[0] * second[1] - first[1] * second[0];
            array[1] = first[0] * second[2] - first[2] * second[0];
            array[2] = first[0] * second[3] - first[3] * second[0];
            array[3] = first[2] * second[3] - first[3] * second[2];
            array[4] = first[3] * second[1] - first[1] * second[3];
            array[5] = first[1] * second[2] - first[2] * second[1];

            return DenseVector.OfArray(array);
        }

        /// <summary>
        /// Calculate the imaginary HVector that is represented by the projectivity ABC -> BCA.<para>
        /// The return value and its complex conjugate are the invariant imaginary points for this projectivity</para><para>
        /// When the Hvectors A, B and C are not linear dependent (i.e. not collinear) the return value is 'null'.</para>
        /// </summary>
        public static HVector ImaginaryHVector(HVector A, HVector B, HVector C)
        {
            var factors = LinearDependentFactors(A, B, C);

            if (factors != null)
            {
                var vector = new Complex(0.5, 0.5 * Math.Sqrt(3)) * factors[0] * A.ToVector() + factors[1] * B.ToVector();
                return new HVector(vector);
            }
            return null;
        }

        /// <summary>
        /// Calculate the homogeneous matrix that transforms the canonical frame, i.e. the canonical base together with the unit homogeneous vector, into
        /// a given set of independent homogeneous vectors.<para>
        /// Only spatial dimensions 1, 2 and 3 are supported, i.e. 3 homogeneous vectors with each 2 coordinates, 4 homogeneous vectors with each 3 coordinates or 5 homogeneous vectors with each 4 coordinates.</para><para>
        /// An exception is thrown when any smaller subset of the homogeneous vectors is linear dependant.</para>
        /// </summary>
        public static Matrix<Complex> CanonicalTransformation(IEnumerable<HVector> hvectors)
        {
            if (hvectors == null) throw new ArgumentNullException("hvectors");
            if (hvectors.Count() == 0) throw new ArgumentException("hvectors required");
            int dimension = hvectors.First().Count - 1;
            foreach (var hvector in hvectors)
            {
                if (hvector.Count != dimension + 1) throw new ArgumentException("hvectors dimension");
            }
            if (hvectors.Count() != dimension + 2) throw new ArgumentException((dimension + 2) + " homogeneous vectors required");
            if (dimension != 1 && dimension != 2 && dimension != 3) throw new ArgumentException("only spatial dimensions 1, 2 and 3 are supported");

            // the unit vector will be mapped to the fist hvector
            var imageOfUnity = hvectors.First().ToVector();

            // the canonical base will be mapped to the subsequent hvectors
            var matrix = Matrix<Complex>.Build.DenseOfColumnVectors(hvectors.Skip(1).Select(pim => pim.ToVector()));
            if (matrix.Determinant().IsZero())
            {
                throw new ArgumentException("homogeneous vectors not linearly independent");
            }

            // calculate the factors that will map the unit vector to the first hvector
            var factors = matrix.Solve(imageOfUnity);

            // every column may be multipied by an arbitrary factor != 0 without affecting the image point (homogeneous coordinates)
            for (int i = 0; i <= dimension; i++)
            {
                if (!factors[i].IsValid())
                {
                    throw new ArgumentException("homogeneous vectors not linearly independent");
                }
                else if (factors[i].IsZero())
                {
                    throw new ArgumentException("homogeneous vectors not linearly independent");
                }
                else
                {
                    matrix.SetColumn(i, matrix.Column(i) * factors[i]);
                }
            }

            matrix.CoerceZero();
            if (matrix.Determinant().IsZero())
            {
                throw new ArgumentException("homogeneous vectors not linearly independent");
            }

            return matrix;
        }

        /// <summary>
        /// Calculate the cross ration of four hvectors, i.e. 4 points, 4 lines, 4 planes.<para>
        /// The first three elements will be considered as the base, w.r.t. which the cross ration of the fourth element will be calculated.</para><para>
        /// In the case of two or three dimensions an exception is thrown when the elements are not in a pencil.</para>
        /// </summary>
        public static Complex CrossRatio(IEnumerable<HVector> hvectors)
        {
            if (hvectors == null) throw new ArgumentNullException("hvectors");
            if (hvectors.Count() != 4) throw new ArgumentException("4 homogeneous vectors required");

            return CrossRatio(hvectors.First(), hvectors.ElementAt(1), hvectors.ElementAt(2), hvectors.ElementAt(3));
        }

        /// <summary>
        /// Calculate the cross ration of four homogeneous vectors, i.e. 4 points, 4 lines, 4 planes.<para>
        /// The first three elements will be considered as the base, w.r.t. which the cross ration of the fourth element will be calculated.</para><para>
        /// First element = origin, second element = infinity, third element = unity.</para><para>
        /// An exception is thrown when the elements are not in a pencil.</para>
        /// </summary>
        public static Complex CrossRatio(params HVector[] hvectors)
        {
            if (hvectors == null) throw new ArgumentNullException("hvectors");
            int? spatialdimension = null;
            foreach (var hvector in hvectors)
            {
                if (spatialdimension == null) spatialdimension = hvector.Count - 1;
                else if (hvector.Count != spatialdimension + 1) throw new ArgumentException("hvectors dimension");
            }
            if (hvectors.Length != 4) throw new ArgumentException("4 homogeneous vectors required");
            if (spatialdimension != 1 && spatialdimension != 2 && spatialdimension != 3) throw new ArgumentException("only spatial dimensions 1, 2 and 3 are supported");

            // the origin hvector will be mapped to the first hvector
            var origin = hvectors[0].ToVector();
            // the infinty hvector will be mapped to the second hvector
            var infintity = hvectors[1].ToVector();
            // the unit hvector will be mapped to the third hvector
            var unity = hvectors[2].ToVector();
            // the cross ration for the fourth hvector will be calculated w.r.t. the other three
            var tocalculate = hvectors[3].ToVector();

            if (origin.EqualsWithinPrecision(infintity)) throw new ArgumentException("two or more equal homogeneous vectors");
            if (origin.EqualsWithinPrecision(unity)) throw new ArgumentException("two or more equal homogeneous vectors");
            if (infintity.EqualsWithinPrecision(unity)) throw new ArgumentException("two or more equal homogeneous vectors");

            if (spatialdimension == 2)
            {
                var point = new Point2D(origin);
                var line = point.Join(new Point2D(infintity)) as HVector;
                if (!line.IsIncident(hvectors[2])) throw new InvalidOperationException("the homogeneous vectors are not collinear");
                if (!line.IsIncident(hvectors[3])) throw new InvalidOperationException("the homogeneous  vectors are not collinear");
            }

            if (spatialdimension == 3)
            {
                var point1 = new Point3D(origin);
                var line = point1.Join(new Point3D(infintity)) as HVector;
                if (!line.IsIncident(hvectors[2])) throw new InvalidOperationException("the homogeneous vectors are not collinear");
                if (!line.IsIncident(hvectors[3])) throw new InvalidOperationException("the homogeneous vectors are not collinear");
            }

            if (tocalculate.Equals(origin)) return 0;
            if (tocalculate.Equals(unity)) return 1;
            if (tocalculate.Equals(infintity)) return new Complex(double.PositiveInfinity, double.PositiveInfinity);

            // calculate the factors that will normalize the unit vector to be equal to the origin hvector + the infinity hvector
            var matrix = Matrix<Complex>.Build.DenseOfColumnVectors(origin, infintity);
            var factors = matrix.Solve(unity);

            origin *= factors[0];
            infintity *= factors[1];

            // now calculate the factors that will make the fourth vector equal to the origin hvector + the infinity hvector
            matrix = Matrix<Complex>.Build.DenseOfColumnVectors(origin, infintity);
            factors = matrix.Solve(tocalculate);

            return factors[1] / factors[0];
        }

        /// <summary>
        /// Calculate the cross ration of four hvectors, i.e. 4 points, 4 lines, 4 planes.<para>
        /// The first three elements will be considered as the base, w.r.t. which the cross ration of the fourth element will be calculated.</para><para>
        /// In the case of two or three dimensions an exception is thrown when the elements are not in a pencil.</para>
        /// </summary>
        public static Complex CrossRatio(Set<HVector> setof4)
        {
            if (setof4 == null) throw new ArgumentNullException("setof4");
            if (setof4.Count != 4) throw new ArgumentException("only sets of 4 elements are supported");

            // the origin hvector will be mapped to the first hvector
            var origin = setof4[0].ToVector();
            // the infinty hvector will be mapped to the second hvector
            var infintity = setof4[1].ToVector();
            // the unit hvector will be mapped to the third hvector
            var unity = setof4[2].ToVector();
            // the cross ration for the fourth hvector will be calculated w.r.t. the other three
            var tocalculate = setof4[3].ToVector();

            if (origin.Equals(infintity)) throw new ArgumentException("two or more equal homogeneous vectors");
            if (origin.Equals(unity)) throw new ArgumentException("two or more equal homogeneous vectors");
            if (infintity.Equals(unity)) throw new ArgumentException("two or more equal homogeneous vectors");

            if (setof4.Count == 2)
            {
                var point = new Point2D(origin);
                var line = point.Join(new Point2D(infintity)) as HVector;
                if (!line.IsIncident(setof4[2])) throw new InvalidOperationException("the homogeneous vectors are not collinear");
                if (!line.IsIncident(setof4[3])) throw new InvalidOperationException("the homogeneous  vectors are not collinear");
            }

            if (setof4.Count == 3)
            {
                var point = new Point3D(origin);
                //var line = point.Join(new Point(infintity)) as HVector;
                //if (!line.IsIncident(unity)) throw new InvalidOperationException("the homogeneous vectors are not collinear");
                //if (!line.IsIncident(tocalculate)) throw new InvalidOperationException("the homogeneous vectors are not collinear");
            }

            if (tocalculate.Equals(origin)) return 0;
            if (tocalculate.Equals(unity)) return 1;
            if (tocalculate.Equals(infintity)) return new Complex(double.PositiveInfinity, double.PositiveInfinity);

            // calculate the factors that will normalize the unit vector to be equal to the origin hvector + the infinity hvector
            var matrix = Matrix<Complex>.Build.DenseOfColumnVectors(origin, infintity);
            var factors = matrix.Solve(unity);

            origin *= factors[0];
            infintity *= factors[1];

            // now calculate the factors that will make the fourth vector equal to the origin hvector + the infinity hvector
            matrix = Matrix<Complex>.Build.DenseOfColumnVectors(origin, infintity);
            factors = matrix.Solve(tocalculate);

            return factors[1] / factors[0];
        }
    }
}
