using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Geometry.Projective
{
    using System.Numerics;
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Complex;

    /// <summary>
    /// A non-singular 3 dimensionsal projective projective mapping of points to points, lines to lines, planes to planes.
    /// </summary>
    public class Collineation
    {
        #region constructors
        /// <summary>
        /// The values of the non-singular 4x4 matrix determine the projectivity.<para>
        /// It must be specified whether the values of the matrix have to be interpreted as pointwise (contravariant) or hyperplanewise (covariant) values.</para><para>
        /// In other words whether the matrix transforms points into points (pointwise, contravariant) or planes into planes (hyperplanewise, covariant).</para>
        /// </summary>
        public Collineation(Matrix matrix, CoordinateType coordinateType)
        {
            if (matrix == null) throw new ArgumentNullException("matrix");
            if (matrix.RowCount != matrix.ColumnCount) throw new ArgumentException("4x4 matrix required");
            if (matrix.RowCount != 4) throw new ArgumentException("4x4 matrix required");

            matrix.CoerceZero();
            if (matrix.Determinant().IsZero())
            {
                throw new ArgumentException("singular matrix");
            }
            var inversetranspose = matrix.Inverse().Transpose();
            switch (coordinateType)
            {
                case CoordinateType.Pointwise:
                    this.matrix = DenseMatrix.OfMatrix(matrix);
                    this.matrixDual = DenseMatrix.OfMatrix(inversetranspose);
                    break;
                case CoordinateType.Hyperplanewise:
                    this.matrix = DenseMatrix.OfMatrix(inversetranspose);
                    this.matrixDual = DenseMatrix.OfMatrix(matrix);
                    break;
                default:
                    throw new ArgumentException("matrix must be a pointwise (contravariant) or (hyper)planewise (covariant) transformation");
            }
        }
        /// <summary>
        /// The values of the non-singular 4x4 matrix are calculated so that 5 given points are mapped to 5 other given points.
        /// </summary>
        public Collineation(IEnumerable<Point3D> preimages, IEnumerable<Point3D> images)
        {
            if (preimages == null) throw new ArgumentNullException("preimages");
            if (images == null) throw new ArgumentNullException("images");
            if (preimages.Count() != 5 || images.Count() != 5) throw new ArgumentException("5 images and 5 pre-images required");

            // matrix A: standard -> preimages
            var A = Extensions.CanonicalTransformation(preimages);

            // matrix B: standard -> images
            var B = Extensions.CanonicalTransformation(images);

            // matrix = B * A.Inverse()
            this.matrix = DenseMatrix.OfMatrix(B * A.Inverse());
            var inversetranspose = matrix.Inverse().Transpose();
            this.matrixDual = DenseMatrix.OfMatrix(inversetranspose);
        }
        /// <summary>
        /// The values of the non-singular 4x4 matrix are calculated so that 5 given planes are mapped to 5 other given planes.
        /// </summary>
        public Collineation(IEnumerable<Plane3D> preimages, IEnumerable<Plane3D> images)
        {
            if (preimages == null) throw new ArgumentNullException("preimages");
            if (images == null) throw new ArgumentNullException("images");
            if (preimages.Count() != 5 || images.Count() != 5) throw new ArgumentException("5 images and 5 pre-images required");

            // matrix A: standard -> preimages
            var A = Extensions.CanonicalTransformation(preimages);

            // matrix B: standard -> images
            var B = Extensions.CanonicalTransformation(images);

            // matrix = B * A.Inverse()
            this.matrixDual = DenseMatrix.OfMatrix(B * A.Inverse());
            var inversetranspose = matrixDual.Inverse().Transpose();
            this.matrix = DenseMatrix.OfMatrix(inversetranspose);
        }
        #endregion

        #region matrices
        /// <summary>
        /// Transforms points into image points.
        /// </summary>
        protected DenseMatrix matrix;
        /// <summary>
        /// Transforms planes into image planes.
        /// </summary>
        protected DenseMatrix matrixDual;
        DenseMatrix _matrixLines;
        /// <summary>
        /// Transforms contravariant line vectors into contravariant image line vectors.
        /// </summary>
        protected DenseMatrix matrixLines
        {
            get
            {
                if (_matrixLines == null)
                {
                    var imageOrigin = Point3D.Origin.Multiply(matrix);
                    var imageInfinityX = Point3D.InfinityX.Multiply(matrix);
                    var imageInfinityY = Point3D.InfinityY.Multiply(matrix);
                    var imageInfinityZ = Point3D.InfinityZ.Multiply(matrix);

                    var col1 = Extensions.PlueckerProduct(imageOrigin, imageInfinityX);
                    var col2 = Extensions.PlueckerProduct(imageOrigin, imageInfinityY);
                    var col3 = Extensions.PlueckerProduct(imageOrigin, imageInfinityZ);
                    var col4 = Extensions.PlueckerProduct(imageInfinityX, imageInfinityY);
                    var col5 = -Extensions.PlueckerProduct(imageInfinityX, imageInfinityZ);
                    var col6 = Extensions.PlueckerProduct(imageInfinityY, imageInfinityX);

                    _matrixLines = DenseMatrix.OfColumnVectors(col1, col2, col3, col4, col5, col6);
                }
                return _matrixLines;
            }
        }
        DenseMatrix _matrixLinesDual;
        /// <summary>
        /// Transforms covariant line vectors into covariant image line vectors.
        /// </summary>
        protected DenseMatrix matrixLinesDual
        {
            get
            {
                if (_matrixLinesDual == null)
                {
                    var inversetranspose = matrixLines.Inverse().Transpose();
                    _matrixLinesDual = DenseMatrix.OfMatrix(inversetranspose);
                }
                return _matrixLinesDual;
            }
        }
        #endregion

        #region mappings
        /// <summary>
        /// Transform the given point into its image point.
        /// </summary>
        public Point3D Map(Point3D point)
        {
            if (point == null) throw new ArgumentNullException("point");
            var imagepoint = point.Multiply(matrix);
            return new Point3D(imagepoint);
        }
        /// <summary>
        /// Transform the given points into their image points.
        /// </summary>
        public IEnumerable<Point3D> Map(IEnumerable<Point3D> points)
        {
            foreach (var item in points)
            {
                yield return Map(item);
            }
        }
        /// <summary>
        /// Transform the given plane into its image plane.
        /// </summary>
        public Plane3D Map(Plane3D plane)
        {
            if (plane == null) throw new ArgumentNullException("plane");
            var imageplane = plane.Multiply(matrix);
            return new Plane3D(imageplane);
        }
        /// <summary>
        /// Transform the given planes into their image planes.
        /// </summary>
        public IEnumerable<Plane3D> Map(IEnumerable<Plane3D> planes)
        {
            foreach (var item in planes)
            {
                yield return Map(item);
            }
        }
        /// <summary>
        /// Transform the given line into its image line.
        /// </summary>
        public Line3D Map(Line3D line)
        {
            if (line == null) throw new ArgumentNullException("line");
            return null;
        }
        /// <summary>
        /// Transform the given lines into their image lines.
        /// </summary>
        public IEnumerable<Line3D> Map(IEnumerable<Line3D> lines)
        {
            foreach (var item in lines)
            {
                yield return Map(item);
            }
        }
        #endregion
    }

    /// <summary>
    /// A non-singular 3 dimensionsal projective mapping of points to planes, lines to lines, planes to points.
    /// </summary>
    public class Correlation
    {
        #region constructors
        /// <summary>
        /// The values of the non-singular 4x4 matrix determine the projectivity.<para>
        /// It must be specified whether the values of the matrix have to be interpreted as pointwise (contravariant) or hyperplanewise (covariant) values.</para><para>
        /// In other words whether the matrix transforms points into planes (pointwise, contravariant) or planes into points (hyperplanewise, covariant).</para>
        /// </summary>
        public Correlation(DenseMatrix matrix, CoordinateType coordinateType)
        {
            if (matrix == null) throw new ArgumentNullException("matrix");
            if (matrix.RowCount != matrix.ColumnCount) throw new ArgumentException("4x4 matrix required");
            if (matrix.RowCount != 4) throw new ArgumentException("4x4 matrix required");

            matrix.CoerceZero();
            if (matrix.Determinant().IsZero())
            {
                throw new ArgumentException("singular matrix");
            }

            var inversetranspose = matrix.Inverse().Transpose();
            switch (coordinateType)
            {
                case CoordinateType.Pointwise:
                    this.matrix = matrix;
                    this.matrixDual = DenseMatrix.OfMatrix(inversetranspose);
                    break;
                case CoordinateType.Hyperplanewise:
                    this.matrixDual = matrix;
                    this.matrix = DenseMatrix.OfMatrix(inversetranspose);
                    break;
                default:
                    throw new ArgumentException("matrix must be a pointwise (contravariant) or (hyper)planewise (covariant) transformation");
            }
        }
        /// <summary>
        /// The values of the non-singular 4x4 matrix are calculated so that 5 given points are mapped to 5 given planes.
        /// </summary>
        public Correlation(IEnumerable<Point3D> preimages, IEnumerable<Plane3D> images)
        {
            if (preimages == null) throw new ArgumentNullException("preimages");
            if (images == null) throw new ArgumentNullException("images");
            if (preimages.Count() != 5 || images.Count() != 5) throw new ArgumentException("5 images and 5 pre-images required");

            // matrix A: standard -> preimages
            var A = Extensions.CanonicalTransformation(preimages);

            // matrix B: standard -> images
            var B = Extensions.CanonicalTransformation(images);

            // matrix = B * A.Inverse()
            this.matrix = DenseMatrix.OfMatrix(B * A.Inverse());
            var inversetranspose = matrix.Inverse().Transpose();
            this.matrixDual = DenseMatrix.OfMatrix(inversetranspose);
        }
        /// <summary>
        /// The values of the non-singular 4x4 matrix are calculated so that 5 given planes are mapped to 5 given points.
        /// </summary>
        public Correlation(IEnumerable<Plane3D> preimages, IEnumerable<Point3D> images)
        {
            if (preimages == null) throw new ArgumentNullException("preimages");
            if (images == null) throw new ArgumentNullException("images");
            if (preimages.Count() != 5 || images.Count() != 5) throw new ArgumentException("5 images and 5 pre-images required");

            // matrix A: standard -> preimages
            var A = Extensions.CanonicalTransformation(preimages);

            // matrix B: standard -> images
            var B = Extensions.CanonicalTransformation(images);

            // matrix = B * A.Inverse()
            this.matrixDual = DenseMatrix.OfMatrix(B * A.Inverse());
            var inversetranspose = matrixDual.Inverse().Transpose();
            this.matrix = DenseMatrix.OfMatrix(inversetranspose);
        }
        #endregion

        #region matrices
        /// <summary>
        /// Transforms points into image planes.
        /// </summary>
        protected DenseMatrix matrix;
        /// <summary>
        /// Transforms planes into image points.
        /// </summary>
        protected DenseMatrix matrixDual;
        DenseMatrix _matrixLines;
        /// <summary>
        /// Transforms contravariant line vectors into covariant image line vectors.
        /// </summary>
        protected DenseMatrix matrixLines
        {
            get
            {
                if (_matrixLines == null)
                {
                    var imageOrigin = Point3D.Origin.Multiply(matrix);
                    var imageInfinityX = Point3D.InfinityX.Multiply(matrix);
                    var imageInfinityY = Point3D.InfinityY.Multiply(matrix);
                    var imageInfinityZ = Point3D.InfinityZ.Multiply(matrix);

                    var col1 = Extensions.PlueckerProduct(imageOrigin, imageInfinityX);
                    var col2 = Extensions.PlueckerProduct(imageOrigin, imageInfinityY);
                    var col3 = Extensions.PlueckerProduct(imageOrigin, imageInfinityZ);
                    var col4 = Extensions.PlueckerProduct(imageInfinityX, imageInfinityY);
                    var col5 = -Extensions.PlueckerProduct(imageInfinityX, imageInfinityZ);
                    var col6 = Extensions.PlueckerProduct(imageInfinityY, imageInfinityX);

                    _matrixLines = DenseMatrix.OfColumnVectors(col1, col2, col3, col4, col5, col6);
                }
                return _matrixLines;
            }
        }
        DenseMatrix _matrixLinesDual;
        /// <summary>
        /// Transforms covariant line vectors into contravariant image line vectors.
        /// </summary>
        protected DenseMatrix matrixLinesDual
        {
            get
            {
                if (_matrixLinesDual == null)
                {
                    var inversetranspose = matrixLines.Inverse().Transpose();
                    _matrixLinesDual = DenseMatrix.OfMatrix(inversetranspose);
                }
                return _matrixLinesDual;
            }
        }
        #endregion

        #region mappings
        /// <summary>
        /// Transform the given point into its image plane.
        /// </summary>
        public Plane3D Map(Point3D point)
        {
            if (point == null) throw new ArgumentNullException("point");
            var imageplane = point.Multiply(matrix);
            return new Plane3D(imageplane);
        }
        /// <summary>
        /// Transform the given points into their image planes.
        /// </summary>
        public IEnumerable<Plane3D> Map(IEnumerable<Point3D> points)
        {
            foreach (var item in points)
            {
                yield return Map(item);
            }
        }
        /// <summary>
        /// Transform the given plane into its image point.
        /// </summary>
        public Point3D Map(Plane3D plane)
        {
            if (plane == null) throw new ArgumentNullException("plane");
            var imagepoint = plane.Multiply(matrix);
            return new Point3D(imagepoint);
        }
        /// <summary>
        /// Transform the given planes into their image points.
        /// </summary>
        public IEnumerable<Point3D> Map(IEnumerable<Plane3D> planes)
        {
            foreach (var item in planes)
            {
                yield return Map(item);
            }
        }
        /// <summary>
        /// Transform the given line into its image line.
        /// </summary>
        public Line3D Map(Line3D line)
        {
            if (line == null) throw new ArgumentNullException("line");

            // TODO
            //switch (line.CoordinateType)
            //{
            //    case CoordinateType.Pointwise:

            //        return new Line(line.Multiply(matrixLines), CoordinateType.Hyperplanewise);

            //    case CoordinateType.Hyperplanewise:

            //        return new Line(line.Multiply(matrixLinesDual), CoordinateType.Pointwise);

            //    default:
            //        throw new ArgumentException("line must be pointwise (contravariant) or (hyper)planewise (covariant)");
            //}
            return null;
        }
        /// <summary>
        /// Transform the given lines into their image lines.
        /// </summary>
        public IEnumerable<Line3D> Map(IEnumerable<Line3D> lines)
        {
            foreach (var item in lines)
            {
                yield return Map(item);
            }
        }
        #endregion

        /// <summary>
        /// The polarity with respect to a (real) sphere, is determined by its (real) center and its (real) radius.
        /// </summary>
        public static Correlation CreatePolaritySphere(Point3D center, double radius)
        {
            if (!center.IsReal()) throw new ArgumentException("Center of sphere must be a real point");

            var pre_images = new List<Point3D>();
            var images = new List<Plane3D>();

            Vector3 center_affine = center.ToAffine();

            Vector3 specialvector = new Vector3(1, 1, 1);
            Point3D specialpoint = new Point3D(center_affine + radius * specialvector.Normalize());
            Plane3D specialplane = new Plane3D(specialvector);
            specialplane = specialplane.Meet(Plane3D.Infinity).Join(specialpoint);
            pre_images.Add(specialpoint);
            images.Add(specialplane);

            pre_images.Add(center);
            images.Add(Plane3D.Infinity);

            Point3D northpole = new Point3D(center_affine + radius * new Vector3(0, 0, 1));
            Point3D southpole = new Point3D(center_affine - radius * new Vector3(0, 0, 1));
            Point3D eastpole = new Point3D(center_affine + radius * new Vector3(0, 1, 0));
            Point3D westpole = new Point3D(center_affine - radius * new Vector3(0, 1, 0));
            Point3D frontpole = new Point3D(center_affine + radius * new Vector3(1, 0, 0));
            Point3D backpole = new Point3D(center_affine - radius * new Vector3(1, 0, 0));

            Plane3D frontplane = eastpole.Join(westpole, northpole);
            Plane3D azimuthplane = backpole.Join(frontpole, northpole);
            Plane3D horizontalplane = backpole.Join(frontpole, westpole);

            pre_images.Add(northpole.Join(southpole).Meet(Plane3D.Infinity));
            images.Add(horizontalplane);

            pre_images.Add(eastpole.Join(westpole).Meet(Plane3D.Infinity));
            images.Add(azimuthplane);

            pre_images.Add(frontpole.Join(backpole).Meet(Plane3D.Infinity));
            images.Add(frontplane);

            var rv = new Correlation(pre_images, images);

            //Line azimuthalinfinity = azimuthplane.Meet(Plane.Infinity);
            //Line frontalinfinity = frontplane.Meet(Plane.Infinity);
            //Line horizontalinfinity = horizontalplane.Meet(Plane.Infinity);

            //System.Diagnostics.Debug.Assert(rv.Map(center).Equals(Plane.Infinity));
            //System.Diagnostics.Debug.Assert(rv.Map(specialpoint).Equals(specialplane));
            //System.Diagnostics.Debug.Assert(rv.Map(northpole).Equals(northpole.Join(horizontalinfinity)));
            //System.Diagnostics.Debug.Assert(rv.Map(southpole).Equals(southpole.Join(horizontalinfinity)));
            //System.Diagnostics.Debug.Assert(rv.Map(eastpole).Equals(eastpole.Join(azimuthalinfinity)));
            //System.Diagnostics.Debug.Assert(rv.Map(westpole).Equals(frontpole.Join(azimuthalinfinity)));
            //System.Diagnostics.Debug.Assert(rv.Map(frontpole).Equals(southpole.Join(frontalinfinity)));
            //System.Diagnostics.Debug.Assert(rv.Map(backpole).Equals(frontpole.Join(frontalinfinity)));

            return rv;
        }
    }
}
