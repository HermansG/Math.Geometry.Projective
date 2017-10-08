using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Geometry.Projective
{
    using System.Numerics;
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Complex;

    /// <summary>
    /// A homogeneous vector with 6 complex (or real) homogeneous coordinates, representing a line in 3-dimensional projective space but also a special linear complex in 5-dimensional projective space.<para>
    /// It must be specified whether the coordinates have to be interpreted as pointwise (contravariant) or (hyper)planewise (covariant) values.</para>
    /// </summary>
    public class Line3D : LinearComplex
    {
        #region constuctors
        /// <summary>
        /// The vector data are copied into the coordinates of the new line.
        /// </summary>
        public Line3D(HVector hvector, CoordinateType coordinateType) : base(hvector, coordinateType) { initialize(); }
        /// <summary>
        /// The vector data are copied into the coordinates of the new line.
        /// </summary>
        public Line3D(Vector<Complex> vector, CoordinateType coordinateType) : base(vector, coordinateType) { initialize(); }
        /// <summary>
        /// The coordinates of the new line.
        /// </summary>
        public Line3D(Complex x0, Complex x1, Complex x2, Complex x3, Complex x4, Complex x5, CoordinateType coordinateType) : base(x0, x1, x2, x3, x4, x5, coordinateType) { initialize(); }
        /// <summary>
        /// The vector data are copied into the coordinates of the new line.
        /// </summary>
        public Line3D(Complex[] values, CoordinateType coordinateType) : base(values, coordinateType) { initialize(); }
        /// <summary>
        /// The vector data are copied into the coordinates of the new line.
        /// </summary>
        public Line3D(double[] values, CoordinateType coordinateType) : base(values, coordinateType) { initialize(); }
        /// <summary>
        /// The direction and moment vector are considered to be the pointwise coordinates of the new line.
        /// </summary>
        public Line3D(VectorC3 direction, VectorC3 moment) : this(new Complex[] { direction[0], direction[1], direction[2], moment[0], moment[1], moment[2] }, CoordinateType.Pointwise) { }
        /// <summary>
        /// The line connecting two distinct, different points.<para>
        /// When the points are equal, the return value is 'null'.</para>
        /// </summary>
        public static Line3D Create(Point3D p, Point3D q)
        {
            var pluecker = Extensions.PlueckerProduct(p.ToVector(), q.ToVector());
            if (pluecker.IsZero()) return null;
            return new Line3D(pluecker, CoordinateType.Pointwise);
        }
        /// <summary>
        /// The line where two distinct, different planes meet.<para>
        /// When the planes are equal, the return value is 'null'.</para>
        /// </summary>
        public static Line3D Create(Plane3D v, Plane3D w)
        {
            var pluecker = Extensions.PlueckerProduct(v.ToVector(), w.ToVector());
            if (pluecker.IsZero()) return null;
            return new Line3D(pluecker, CoordinateType.Hyperplanewise);
        }
        void initialize()
        {
            if (!IsSpecial) throw new ArgumentException("The coordinates of the vector do not represent a line but a non-special complex");

            // 0, vector[0], vector[1], vector[2] (*-1)
            pointInfinity = new Lazy<Vector<Complex>>(() => Plane3D.Infinity.Multiply(MatrixPlaneToPoint));
            // -vector[0], 0, vector[5], -vector[4] (*-1)
            pointYZ = new Lazy<Vector<Complex>>(() => Plane3D.YZ.Multiply(MatrixPlaneToPoint));
            // -vector[1], -vector[5], 0, vector[3] (*-1)
            pointXZ = new Lazy<Vector<Complex>>(() => Plane3D.XZ.Multiply(MatrixPlaneToPoint));
            // -vector[2], vector[4], -vector[3], 0 (*-1)
            pointXY = new Lazy<Vector<Complex>>(() => Plane3D.XY.Multiply(MatrixPlaneToPoint));

            // 0, vectordual[0], vectordual[1], vectordual[2] ==  0, vector[3], vector[4], vector[5] (*-1)
            planeOrigin = new Lazy<Vector<Complex>>(() => Point3D.Origin.Multiply(MatrixPointToPlane));
            // -vectordual[0], 0, vectordual[5], -vectordual[4] == -vector[3], 0, vector[2], -vector[1] (*-1)
            planeInfinityX = new Lazy<Vector<Complex>>(() => Point3D.InfinityX.Multiply(MatrixPointToPlane));
            // -vectordual[1], -vectordual[5], 0, vectordual[3] == -vector[4], -vector[2], 0, vector[0] (*-1)
            planeInfinityY = new Lazy<Vector<Complex>>(() => Point3D.InfinityY.Multiply(MatrixPointToPlane));
            // -vectordual[2], vectordual[4], -vectordual[3], 0 == -vector[5], vector[1], -vector[0], 0 (*-1)
            planeInfinityZ = new Lazy<Vector<Complex>>(() => Point3D.InfinityZ.Multiply(MatrixPointToPlane));

            conjugate = new Lazy<Line3D>(() => new Line3D(vector.Conjugate(), CoordinateType.Pointwise));
        }
        #endregion

        /// <summary>
        /// Create a new line, identical to this one.
        /// </summary>
        public new Line3D Clone() { return new Line3D(vector, CoordinateType.Pointwise); }

        /// <summary>
        /// The direction vector of this line in space.<para>
        /// The local coordinates in the plane at infinity</para>
        /// </summary>
        public VectorC3 DirectionVector { get { return new VectorC3(vector[0], vector[1], vector[2]); } }
        /// <summary>
        /// The moment vector of this line in space.<para>
        /// The local coordinates in the bundle of lines through the origin.</para>
        /// </summary>
        public VectorC3 MomentVector { get { return new VectorC3(vectordual[0], vectordual[1], vectordual[2]); } }

        #region points and planes
        // at least two vectors are valid points
        Lazy<Vector<Complex>> pointInfinity;
        Lazy<Vector<Complex>> pointYZ;
        Lazy<Vector<Complex>> pointXZ;
        Lazy<Vector<Complex>> pointXY;

        // at least two vectors are valid planes
        Lazy<Vector<Complex>> planeOrigin;
        Lazy<Vector<Complex>> planeInfinityX;
        Lazy<Vector<Complex>> planeInfinityY;
        Lazy<Vector<Complex>> planeInfinityZ;

        List<Point3D> points = new List<Point3D>();
        /// <summary>
        /// A copy of some points on the line, with complex or real coordinates.
        /// </summary>
        public List<Point3D> GetPoints(int count = 5, bool real = true)
        {
            if (points.Count == 0)
            {
                if (pointInfinity.Value != null)
                {
                    points.Add(new Point3D(pointInfinity.Value));
                }
                if (pointXY.Value != null)
                {
                    if (points.All(p => !p.Equals(pointXY.Value)))
                    {
                        points.Add(new Point3D(pointXY.Value));
                    }
                }
                if (pointXZ.Value != null)
                {
                    if (points.All(p => !p.Equals(pointXZ.Value)))
                    {
                        points.Add(new Point3D(pointXZ.Value));
                    }
                }
                if (pointYZ.Value != null)
                {
                    if (points.All(p => !p.Equals(pointYZ.Value)))
                    {
                        points.Add(new Point3D(pointYZ.Value));
                    }
                }
                if (IsImaginaryFirstKind && count > 2)
                {
                    var realpoint = Meet(Conjugate);
                    if (realpoint != null)
                    {
                        if (points.All(p => !realpoint.Equals(p)))
                        {
                            points.Add(realpoint);
                        }
                    }
                    else
                    {
                        throw new AlgorithmException("Real point of imaginary line of the first kind not found");
                    }
                }
            }
            List<Point3D> calculation = points.ToList();
            if (real)
            {
                calculation.RemoveAll(p => !p.IsReal());
                if (IsImaginaryFirstKind)
                {
                    if (calculation.Count != 1)
                    {
                        throw new AlgorithmException("Inconsistent real points for an imaginary line of the first kind");
                    }
                    return calculation;
                }
                if (IsImaginarySecondKind)
                {
                    if (calculation.Count != 0)
                    {
                        throw new AlgorithmException("Inconsistent real points for an imaginary line of the second kind");
                    }
                    return calculation;
                }
            }
            calculation.Shuffle();
            var first = calculation[0];
            var second = calculation[1];
            while (calculation.Count < count)
            {
                double random1Real = Extensions.PickRandom(1, 2 * count) * Extensions.PickRandomSign();
                double random2Real = Extensions.PickRandom(1, 2 * count) * Extensions.PickRandomSign();
                HVector hvector = null;
                if (real)
                {
                    hvector = new HVector(random1Real * first.ToVector() + random2Real * second.ToVector());
                }
                else
                {
                    double random1Imaginary = Extensions.PickRandom(1, 2 * count) * Extensions.PickRandomSign();
                    double random2Imaginary = Extensions.PickRandom(1, 2 * count) * Extensions.PickRandomSign();
                    Complex random1 = new Complex(random1Real, random1Imaginary);
                    Complex random2 = new Complex(random2Real, random2Imaginary);
                    hvector = new HVector(random1 * first.ToVector() + random2 * second.ToVector());
                }
                if (points.All(p => !hvector.Equals(p)))
                {
                    var point = new Point3D(hvector);
                    points.Add(point);
                    calculation.Add(point);
                }
            }
            return calculation;
        }

        List<Plane3D> planes = new List<Plane3D>();
        /// <summary>
        /// A copy of some planes through the line, with complex or real coordinates.
        /// </summary>
        public List<Plane3D> GetPlanes(int count = 5, bool real = true)
        {
            if (planes.Count == 0)
            {
                if (planeOrigin.Value != null)
                {
                    planes.Add(new Plane3D(planeOrigin.Value));
                }
                if (planeInfinityX.Value != null)
                {
                    if (planes.All(p => !p.Equals(planeInfinityX.Value)))
                    {
                        planes.Add(new Plane3D(planeInfinityX.Value));
                    }
                }
                if (planeInfinityY.Value != null)
                {
                    if (planes.All(p => !p.Equals(planeInfinityY.Value)))
                    {
                        planes.Add(new Plane3D(planeInfinityY.Value));
                    }
                }
                if (planeInfinityZ.Value != null)
                {
                    if (planes.All(p => !p.Equals(planeInfinityZ.Value)))
                    {
                        planes.Add(new Plane3D(planeInfinityZ.Value));
                    }
                }
                if (IsImaginaryFirstKind && count > 2)
                {
                    var realplane = Join(Conjugate);
                    if (realplane != null)
                    {
                        if (planes.All(p => !realplane.Equals(p)))
                        {
                            planes.Add(realplane);
                        }
                    }
                    else
                    {
                        throw new AlgorithmException("Real plane of imaginary line of the first kind not found");
                    }
                }
            }
            List<Plane3D> calculation = planes.ToList();
            if (real)
            {
                calculation.RemoveAll(p => !p.IsReal());
                if (IsImaginaryFirstKind)
                {
                    if (calculation.Count != 1)
                    {
                        throw new AlgorithmException("Inconsistent real planes for an imaginary line of the first kind");
                    }
                    return calculation;
                }
                if (IsImaginarySecondKind)
                {
                    if (calculation.Count != 0)
                    {
                        throw new AlgorithmException("Inconsistent real planes for an imaginary line of the second kind");
                    }
                    return calculation;
                }
            }
            calculation.Shuffle();
            var first = calculation[0];
            var second = calculation[1];
            while (calculation.Count < count)
            {
                double random1Real = Extensions.PickRandom(1, 2 * count) * Extensions.PickRandomSign();
                double random2Real = Extensions.PickRandom(1, 2 * count) * Extensions.PickRandomSign();
                HVector hvector = null;
                if (real)
                {
                    hvector = new HVector(random1Real * first.ToVector() + random2Real * second.ToVector());
                }
                else
                {
                    double random1Imaginary = Extensions.PickRandom(1, 2 * count) * Extensions.PickRandomSign();
                    double random2Imaginary = Extensions.PickRandom(1, 2 * count) * Extensions.PickRandomSign();
                    Complex random1 = new Complex(random1Real, random1Imaginary);
                    Complex random2 = new Complex(random2Real, random2Imaginary);
                    hvector = new HVector(random1 * first.ToVector() + random2 * second.ToVector());
                }
                if (planes.All(p => !hvector.Equals(p)))
                {
                    var plane = new Plane3D(hvector);
                    planes.Add(plane);
                    calculation.Add(plane);
                }
            }
            return calculation;
        }
        #endregion

        #region meet and join
        /// <summary>
        /// Calculate whether the point lies on this line.
        /// </summary>
        public bool IsIncident(Point3D point)
        {
            return Join(point) == null;
        }
        /// <summary>
        /// Calculate whether this line lies in the plane.
        /// </summary>
        public bool IsIncident(Plane3D plane)
        {
            return Meet(plane) == null;
        }
        /// <summary>
        /// Calculate whether this line and another line have a common point and plane.
        /// </summary>
        public bool IsIncident(Line3D line)
        {
            if (line == null) throw new ArgumentNullException("line");
            return (vector * line.vectordual).Magnitude <= 6 * Extensions.PrecisionZero;
        }
        /// <summary>
        /// Calculate the plane, spanned by this line and a point.<para>
        /// When the point is on the line, the return value is 'null'.</para>
        /// </summary>
        public Plane3D Join(Point3D point)
        {
            if (point == null) throw new ArgumentNullException("point");
            var plane = MatrixPointToPlane.Multiply(point.ToVector());
            if (plane.IsZero()) return null;
            else return new Plane3D(plane);
        }
        /// <summary>
        /// Calculate the point where this line meets a plane.<para>
        /// When the line is in the plane, the return value is 'null'.</para>
        /// </summary>
        public Point3D Meet(Plane3D plane)
        {
            if (plane == null) throw new ArgumentNullException("plane");
            var point = MatrixPlaneToPoint.Multiply(plane.ToVector()); ;
            if (point.IsZero()) return null;
            else return new Point3D(point);
        }
        /// <summary>
        /// Calculate the plane, spanned by this line and another line.<para>
        /// When the lines are skew, the return value is 'null'.</para>
        /// </summary>
        public Plane3D Join(Line3D line)
        {
            if (!this.IsIncident(line)) return null;
            if (this.Equals(line)) return null;
            foreach (var point in line.GetPoints(2))
            {
                Plane3D plane = Join(point);
                if (plane != null) return plane;
            }
            throw new AlgorithmException("Common plane of two distinct incident lines not found");
        }
        /// <summary>
        /// Calculate the point where this line meets another line.<para>
        /// When the lines are skew, the return value is 'null'.</para>
        /// </summary>
        public Point3D Meet(Line3D line)
        {
            if (!this.IsIncident(line)) return null;
            if (this.Equals(line)) return null;
            foreach (var plane in line.GetPlanes(2))
            {
                Point3D point = Meet(plane);
                if (point != null) return point;
            }
            throw new AlgorithmException("Common point of two distinct incident lines not found");
        }
        #endregion

        /// <summary>
        /// Calculate a random line, incident with this line.<para>
        /// Optionally excluding a list of known HVectors, optionally complex.</para>
        /// </summary>
        public new Line3D GetRandomIncident(bool real = true, IEnumerable<HVector> exclude = null)
        {
            var condition = new Func<HVector, bool>((v) => exclude == null ? true : exclude.All(ex => !ex.Equals(v)));

            // join a random point on this line with a random point not on this line
            List<Point3D> points = GetPoints(2, real);
            Point3D first = points[0];
            Point3D second = points[1];
            double random1Real = Extensions.PickRandom(0, 101) * Extensions.PickRandomSign();
            double random2Real = Extensions.PickRandom(0, 101) * Extensions.PickRandomSign();
            while (random1Real.IsZero() && random2Real.IsZero())
            {
                random1Real = Extensions.PickRandom(0, 101) * Extensions.PickRandomSign();
                random2Real = Extensions.PickRandom(0, 101) * Extensions.PickRandomSign();
            }
            HVector hvector = null;
            if (real)
            {
                hvector = new HVector(random1Real * first.ToVector() + random2Real * second.ToVector());
            }
            else
            {
                double random1Imaginary = Extensions.PickRandom(0, 101) * Extensions.PickRandomSign();
                double random2Imaginary = Extensions.PickRandom(0, 101) * Extensions.PickRandomSign();
                Complex random1 = new Complex(random1Real, random1Imaginary);
                Complex random2 = new Complex(random2Real, random2Imaginary);
                hvector = new HVector(random1 * first.ToVector() + random2 * second.ToVector());
            }
            Point3D pointonline = new Point3D(hvector);

            Point3D random = new Point3D(Extensions.PickRandomHVector(4, real));
            while (IsIncident(random))
            {
                random = new Point3D(Extensions.PickRandomHVector(4, real));
            }

            return pointonline.Join(random);
        }

        Lazy<Line3D> conjugate;
        /// <summary>
        /// The line with complex conjugate coordinates.
        /// </summary>
        public new Line3D Conjugate { get { return conjugate.Value; } }

        /// <summary>
        /// Determine wheter the line is real or imaginary of the first or second kind.<para>
        /// An imaginary line of the first kind has exactly one real point and one real plane.</para>
        /// </summary>
        public bool IsImaginaryFirstKind
        {
            get
            {
                if (IsReal()) return false;
                return IsIncident(Conjugate);
            }
        }

        /// <summary>
        /// Determine wheter the line is real or imaginary of the first or second kind.<para>
        /// An imaginary line of the second kind has no real points or real planes.</para>
        /// </summary>
        public bool IsImaginarySecondKind
        {
            get
            {
                if (IsReal()) return false;
                return !IsIncident(Conjugate);
            }
        }

        #region constants
        /// <summary>
        /// (1 0 0 0 0 0)
        /// </summary>
        public static readonly Line3D Xaxis = Line3D.Create(Point3D.Origin, Point3D.InfinityX);
        /// <summary>
        /// (0 1 0 0 0 0)
        /// </summary>
        public static readonly Line3D Yaxis = Line3D.Create(Point3D.Origin, Point3D.InfinityY);
        /// <summary>
        /// (0 0 1 0 0 0)
        /// </summary>
        public static readonly Line3D Zaxis = Line3D.Create(Point3D.Origin, Point3D.InfinityZ);
        /// <summary>
        /// (0 0 0 1 0 0)
        /// </summary>
        public static readonly Line3D InfinityYZ = Line3D.Create(Plane3D.Infinity, Plane3D.YZ);
        /// <summary>
        /// (0 0 0 0 1 0)
        /// </summary>
        public static readonly Line3D InfinityXZ = Line3D.Create(Plane3D.Infinity, Plane3D.XZ);
        /// <summary>
        /// (0 0 0 0 0 1)
        /// </summary>
        public static readonly Line3D InfinityXY = Line3D.Create(Plane3D.Infinity, Plane3D.XY);
        #endregion
    }
}
