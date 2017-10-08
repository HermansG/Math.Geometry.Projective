using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Geometry.Projective
{
    using System.Numerics;
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Complex;

    /// <summary>
    /// A homogeneous vector with 6 complex (or real) homogeneous coordinates, representing a linear complex in 5- but also in 3-dimensional projective space.<para>
    /// It must be specified whether the coordinates have to be interpreted as pointwise (contravariant) or hyperplanewise (covariant) values.</para>
    /// </summary>
    public class LinearComplex : HVector
    {
        #region constructors
        /// <summary>
        /// The vector data are copied into the coordinates of the new linear complex.
        /// </summary>
        public LinearComplex(HVector hvector, CoordinateType coordinateType) : base(hvector) { initialize(coordinateType); }
        /// <summary>
        /// The vector data are copied into the coordinates of the new linear complex.
        /// </summary>
        public LinearComplex(Vector<Complex> vector, CoordinateType coordinateType) : base(vector) { initialize(coordinateType); }
        /// <summary>
        /// The vector data are copied into the coordinates of the new linear complex.
        /// </summary>
        public LinearComplex(Complex[] values, CoordinateType coordinateType) : base(values) { initialize(coordinateType); }
        /// <summary>
        /// The vector data are copied into the coordinates of the new linear complex.
        /// </summary>
        public LinearComplex(double[] values, CoordinateType coordinateType) : base(values) { initialize(coordinateType); }
        /// <summary>
        /// The coordinates of the new linear complex.
        /// </summary>
        public LinearComplex(Complex x0, Complex x1, Complex x2, Complex x3, Complex x4, Complex x5, CoordinateType coordinateType)
            : this(new Complex[] { x0, x1, x2, x3, x4, x5 }, coordinateType) { }
        void initialize(CoordinateType coordinateType)
        {
            if (vector.Count != 6) throw new ArgumentException("6 coordinates required");

            switch (coordinateType)
            {
                case CoordinateType.Pointwise:
                    vectordual = dualize(vector);
                    break;
                case CoordinateType.Hyperplanewise:
                    vectordual = vector;
                    vector = dualize(vector);
                    break;
                default:
                    throw new ArgumentException("coordinateType");
            }

            matrixPlaneToPoint = new Lazy<DenseMatrix>(() => createMatrix(vector.Values));
            matrixPointToPlane = new Lazy<DenseMatrix>(() => createMatrix(vectordual.Values));
            nullPolarity = new Lazy<Correlation>(() => IsSpecial ? null : new Correlation(MatrixPlaneToPoint, CoordinateType.Pointwise));
            conjugate = new Lazy<LinearComplex>(() => new LinearComplex(vector.Conjugate(), CoordinateType.Pointwise));
        }
        #endregion

        /// <summary>
        /// The dual internal vector data, i.e. the (hyper)planewise coordinates.<para>
        /// Densevector 'vector' contains the pointwise coordinates.</para>
        /// </summary>
        protected DenseVector vectordual;

        /// <summary>
        /// Dualize linear complex-coordinates, i.e. interchange line- and plane-coordinates of a linear complex.
        /// </summary>
        protected Complex[] dualize(DenseVector vector)
        {
            Complex[] rv = new Complex[6];
            for (int i = 0; i < 6; i++)
            {
                rv[i] = vector[dual(i)];
            }
            return rv;
        }

        /// <summary>
        /// Dualize the co- and contravariant indices.
        /// </summary>
        protected static int dual(int index)
        {
            switch (index)
            {
                case 0: return 3;
                case 1: return 4;
                case 2: return 5;

                case 3: return 0;
                case 4: return 1;
                case 5: return 2;

                default:
                    throw new ArgumentException("index");
            }
        }

        /// <summary>
        /// Create a new linear complex, identical to this one.
        /// </summary>
        public new LinearComplex Clone() { return new LinearComplex(this.vector, CoordinateType.Pointwise); }

        /// <summary>
        /// The pointwise (contravariant) coordinates of the linear complex.
        /// </summary>
        public Complex[] ValuesPointwise { get { return vector.ToArray(); } }

        /// <summary>
        /// The (hyper)planewise (covariant) coordinates of the linear complex.
        /// </summary>
        public Complex[] ValuesPlanewise { get { return vectordual.ToArray(); } }

        Lazy<DenseMatrix> matrixPlaneToPoint;
        /// <summary>
        /// The matrix of the null-polarity that transforms planes into points.<para>
        /// For lines the resulting point is the point where the line meets the plane.</para>
        /// </summary>
        public DenseMatrix MatrixPlaneToPoint { get { return matrixPlaneToPoint.Value; } }

        Lazy<DenseMatrix> matrixPointToPlane;
        /// <summary>
        /// The matrix of the null-polarity that transforms points into planes.<para>
        /// For lines the resulting plane is the plane that is spanned by the point and the line.</para>
        /// </summary>
        public DenseMatrix MatrixPointToPlane { get { return matrixPointToPlane.Value; } }

        DenseMatrix createMatrix(Complex[] values)
        {
            var matrix = new DenseMatrix(4);

            // anti-symmetrical: all [i, i] values remain zero

            matrix[0, 1] = values[0];
            matrix[0, 2] = values[1];
            matrix[0, 3] = values[2];

            matrix[1, 2] = values[5];
            matrix[1, 3] = -values[4];

            matrix[2, 3] = values[3];

            for (int i = 1; i <= 3; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    matrix[i, j] = -matrix[j, i];
                }
            }

            return matrix;
        }

        Lazy<Correlation> nullPolarity;
        /// <summary>
        /// The null polarity corresponding to this linear complex.
        /// </summary>
        public Correlation NullPolarity { get { return nullPolarity.Value; } }

        /// <summary>
        /// Calculate the inner product between the point- and planewise coordinates of the linear complex.
        /// </summary>
        Complex scalar { get { return (vector[0] * vectordual[0] + vector[1] * vectordual[1] + vector[2] * vectordual[2]).CoerceZero(); } }
        /// <summary>
        /// Check whether this linear complex is special, i.e. it is a line and its null polarity is degenerated.
        /// </summary>
        public bool IsSpecial { get { return scalar.Magnitude <= 5 * Extensions.PrecisionZero; } }
        Line3D line;
        /// <summary>
        /// Convert the linear complex to a line when possible, otherwise return null.
        /// </summary>
        public Line3D ToLine()
        {
            if (IsSpecial)
            {
                if (line == null)
                {
                    line = new Line3D(vector, CoordinateType.Pointwise);
                }
                return line;
            }
            else return null;
        }

        #region lines of the linear complex
        /// <summary>
        /// Check whether a line belongs to this linear complex of lines.<para>
        /// For special linear complexes this is equal to the incidence relationship.</para>
        /// </summary>
        public bool Contains(Line3D line)
        {
            if (line == null) throw new ArgumentNullException("line");
            return (vector * line.vectordual).Magnitude < 6 * Extensions.PrecisionZero;
        }

        /// <summary>
        /// The pitch of the helical motion associated with the linear complex.
        /// </summary>
        public Complex Pitch
        {
            get
            {
                if (IsSpecial) return 0;
                else
                {
                    return scalar / (vector[0].Square() + vector[1].Square() + vector[2].Square());
                }
            }
        }

        Line3D axis;
        /// <summary>
        /// The axis of the helical motion associated with the linear complex.
        /// </summary>
        public Line3D Axis
        {
            get
            {
                if (axis == null)
                {
                    if (IsSpecial)
                    {
                        axis = ToLine();
                    }
                    else
                    {
                        VectorC3 direction = new VectorC3(vector[0], vector[1], vector[2]);
                        VectorC3 moment = new VectorC3(vectordual[0], vectordual[1], vectordual[2]);
                        axis = new Line3D(direction, moment - Pitch * direction);
                    }
                }
                return axis;
            }
        }

        List<Line3D> lines = new List<Line3D>();
        List<Line3D> GetLines(int count = 5, bool real = true, Plane3D plane = null, Point3D point = null)
        {
            if (lines.Count == 0)
            {
                lines.Add(Axis);

                Point3D infinitePoint = Axis.Meet(Plane3D.Infinity);
                Line3D orthogonalAxis = null;
                if (infinitePoint == null)
                {
                    Plane3D normal = Axis.Join(Point3D.Origin);
                    orthogonalAxis = new Line3D(normal.NormalVector, VectorC3.Zero);
                }
                else
                {
                    Plane3D normal = new Plane3D(infinitePoint.ToAffine());
                    orthogonalAxis = normal.Meet(Plane3D.Infinity);
                }
                lines.Add(orthogonalAxis);
            }
            List<Line3D> calculation = lines.ToList();
            if (plane != null)
            {
                calculation.RemoveAll(l => !l.IsIncident(plane));
            }
            if (point != null)
            {
                calculation.RemoveAll(l => !l.IsIncident(point));
            }
            if (real)
            {
                calculation.RemoveAll(l => !l.IsReal());
            }
            if (plane == null && point == null)
            {
                while (calculation.Count < count)
                {
                    var incident = this.GetRandomIncident(real, lines);
                    if (lines.All(l => !incident.Equals(l)))
                    {
                        var line = new Line3D(incident, CoordinateType.Pointwise);
                        lines.Add(line);
                        calculation.Add(line);
                    }
                }
                return calculation;
            }
            else if (plane == null)
            {
                plane = new Plane3D(MatrixPointToPlane.Multiply(point.ToVector()));
            }
            else if (point == null)
            {
                point = new Point3D(MatrixPlaneToPoint.Multiply(plane.ToVector()));
            }
            var exclude = new List<Point3D> { point };
            while (calculation.Count < count)
            {
                var randomPoint = new Point3D(plane.GetRandomIncident(real, exclude));
                exclude.Add(randomPoint);
                var randomline = point.Join(randomPoint);
                calculation.Add(randomline);
            }
            return calculation;
        }
        /// <summary>
        /// A copy of some lines in the linear complex, with complex or real coordinates.
        /// </summary>
        public List<Line3D> GetLines(int count = 5, bool real = true)
        {
            return GetLines(count, real, null, null);
        }
        /// <summary>
        /// A copy of some lines in the linear complex, with complex or real coordinates.<para>
        /// Optionally incident with a given plane.</para>
        /// </summary>
        public List<Line3D> GetLines(int count = 2, bool real = true, Plane3D plane = null)
        {
            return GetLines(count, real, plane, null);
        }
        /// <summary>
        /// A copy of some lines in the linear complex, with complex or real coordinates.<para>
        /// Optionally incident with a given point.</para>
        /// </summary>
        public List<Line3D> GetLines(int count = 2, bool real = true, Point3D point = null)
        {
            return GetLines(count, real, null, point);
        }
        #endregion

        Lazy<LinearComplex> conjugate;
        /// <summary>
        /// The linear complex with complex conjugate coordinates.
        /// </summary>
        public new LinearComplex Conjugate { get { return conjugate.Value; } }
    }

    /// <summary>
    /// Indicates whether the coordinates belong to:<para>
    ///   - a pointwise (contravariant) vector</para><para>
    ///   - a hyperplanewise (covariant) vector</para><para>
    ///   - or: undetermined.</para>
    /// </summary>
    public enum CoordinateType
    {
        /// <summary>
        /// Point coordinates or derived from point coordinates.<para>
        /// Also: 0-dimensional subpsace (point), contravariant.</para>
        /// </summary>
        Pointwise = 0,
        /// <summary>
        /// Hyperplane coordinates or derived from hyperplane coordinates.<para>
        /// Also: n-1-dimensional subpsace (line in 2 dimensions, plane in 3 dimensions), covariant.</para>
        /// </summary>        
        Hyperplanewise = 1
    }
}
