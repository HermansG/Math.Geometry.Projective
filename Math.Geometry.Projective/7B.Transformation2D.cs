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
    /// A non-singular 2 dimensionsal projective projective mapping of points to points, lines to lines.
    /// </summary>
    public class Collineation2D
    {
        #region constructors
        /// <summary>
        /// The values of the non-singular 3x3 matrix determine the projectivity.<para>
        /// It must be specified whether the values of the matrix have to be interpreted as pointwise (contravariant) or hyperplanewise (covariant) values.</para><para>
        /// In other words whether the matrix transforms points into points (pointwise, contravariant) or lines into lines (hyperplanewise, covariant).</para>
        /// </summary>
        public Collineation2D(Matrix matrix, CoordinateType coordinateType)
        {
            if (matrix == null) throw new ArgumentNullException("matrix");
            if (matrix.RowCount != matrix.ColumnCount) throw new ArgumentException("3x3 matrix required");
            if (matrix.RowCount != 3) throw new ArgumentException("3x3 matrix required");

            matrix.CoerceZero();
            if (matrix.Determinant().IsZero())
            {
                throw new ArgumentException("singular matrix");
            }
            var inversetranspose = matrix.Inverse().Transpose();
            switch (coordinateType)
            {
                case CoordinateType.Pointwise:
                    this.Matrix = DenseMatrix.OfMatrix(matrix);
                    this.MatrixDual = DenseMatrix.OfMatrix(inversetranspose);
                    break;
                case CoordinateType.Hyperplanewise:
                    this.Matrix = DenseMatrix.OfMatrix(inversetranspose);
                    this.MatrixDual = DenseMatrix.OfMatrix(matrix);
                    break;
                default:
                    throw new ArgumentException("matrix must be a pointwise (contravariant) or (hyper)planewise (covariant) transformation");
            }
        }
        /// <summary>
        /// The values of the non-singular 3x3 matrix are calculated so that 4 given points are mapped to 4 other given points.
        /// </summary>
        public Collineation2D(IEnumerable<Point2D> preimages, IEnumerable<Point2D> images)
        {
            if (preimages == null) throw new ArgumentNullException("preimages");
            if (images == null) throw new ArgumentNullException("images");
            if (preimages.Count() != 4 || images.Count() != 4) throw new ArgumentException("4 images and 4 pre-images required");

            // matrix A: standard -> preimages
            var A = Extensions.CanonicalTransformation(preimages);

            // matrix B: standard -> images
            var B = Extensions.CanonicalTransformation(images);

            // matrix = B * A.Inverse()
            Matrix = DenseMatrix.OfMatrix(B * A.Inverse());
            Matrix.CoerceZero();
            var inversetranspose = Matrix.Inverse().Transpose();
            MatrixDual = DenseMatrix.OfMatrix(inversetranspose);
            MatrixDual.CoerceZero();
        }
        /// <summary>
        /// The values of the non-singular 4x4 matrix are calculated so that 5 given planes are mapped to 5 other given planes.
        /// </summary>
        public Collineation2D(IEnumerable<Line2D> preimages, IEnumerable<Line2D> images)
        {
            if (preimages == null) throw new ArgumentNullException("preimages");
            if (images == null) throw new ArgumentNullException("images");
            if (preimages.Count() != 4 || images.Count() != 4) throw new ArgumentException("4 images and 4 pre-images required");

            // matrix A: standard -> preimages
            var A = Extensions.CanonicalTransformation(preimages);

            // matrix B: standard -> images
            var B = Extensions.CanonicalTransformation(images);

            // matrix = B * A.Inverse()
            this.MatrixDual = DenseMatrix.OfMatrix(B * A.Inverse());
            var inversetranspose = MatrixDual.Inverse().Transpose();
            this.Matrix = DenseMatrix.OfMatrix(inversetranspose);
        }
        #endregion

        #region matrices
        /// <summary>
        /// Transforms points into image points.
        /// </summary>
        public DenseMatrix Matrix;
        /// <summary>
        /// Transforms lines into image lines.
        /// </summary>
        public DenseMatrix MatrixDual;
        #endregion

        #region mappings
        /// <summary>
        /// Transform the given point into its image point.
        /// </summary>
        public Point2D Map(Point2D point)
        {
            if (point == null) throw new ArgumentNullException("point");
            var imagepoint = point.Multiply(Matrix);
            return new Point2D(imagepoint);
        }
        /// <summary>
        /// Transform the given points into their image points.
        /// </summary>
        public IEnumerable<Point2D> Map(IEnumerable<Point2D> points)
        {
            foreach (var item in points)
            {
                yield return Map(item);
            }
        }
        /// <summary>
        /// Transform the given line into its image line.
        /// </summary>
        public Line2D Map(Line2D line)
        {
            if (line == null) throw new ArgumentNullException("line");
            var imageline = line.Multiply(MatrixDual);
            return new Line2D(imageline);
        }
        /// <summary>
        /// Transform the given lines into their image lines.
        /// </summary>
        public IEnumerable<Line2D> Map(IEnumerable<Line2D> lines)
        {
            foreach (var item in lines)
            {
                yield return Map(item);
            }
        }
        #endregion

        /// <summary>
        /// The central collineation with respect to a center point, an axis and a factor for the cross ratio.<para>
        /// In the case of an elation, the factor is additative.</para>
        /// </summary>
        public static Collineation2D CreateCentralCollineation(Point2D center, Line2D axis, double factor)
        {
            if (Math.Abs(factor) < 1000 * Extensions.PrecisionZero) throw new ArgumentException("The cross ratio factor for the central collineation is too small (almost zero)");

            var pre_images = new List<Point2D>();
            var images = new List<Point2D>();

            var point1 = new Point2D(axis.GetRandomIncident(real: true, exclude: new List<HVector> { center }));
            var point2 = new Point2D(axis.GetRandomIncident(real: true, exclude: new List<HVector> { center, point1 }));

            pre_images.Add(point1);
            pre_images.Add(point2);
            images.Add(point1);
            images.Add(point2);

            bool elation = axis.IsIncident(center);
            if (!elation)
            {
                pre_images.Add(center);
                images.Add(center);
            }

            var linestoexclude = new List<HVector> { axis };
            if (!elation)
            {
                linestoexclude.Add(center.Join(point1));
                linestoexclude.Add(center.Join(point2));
            }
            var line = new Line2D(center.GetRandomIncident(real: true, exclude: linestoexclude));

            Point2D meetingpoint = elation ? center : line.Meet(axis);

            var extra_pre_image1 = new Point2D(line.GetRandomIncident(real: true, exclude: new List<Point2D> { center, meetingpoint }));

            // calculate the factors a, b so that extrapoint1 = a * center + b * meetingpoint
            var matrix = Matrix<Complex>.Build.DenseOfColumnVectors(center.ToVector(), meetingpoint.ToVector());
            var factors = matrix.Solve(extra_pre_image1.ToVector()).ToArray();

            var extra_image1 = new Point2D(factors[0] * center.ToVector() + (factor * factors[1]) * meetingpoint.ToVector());

            pre_images.Add(extra_pre_image1);
            images.Add(extra_image1);

            if (elation)
            {
                line = new Line2D(center.GetRandomIncident(real: true, exclude: new List<HVector> { axis, line }));

                meetingpoint = center;

                var extrapoint2 = new Point2D(line.GetRandomIncident(real: true, exclude: new List<Point2D> { center, meetingpoint }));

                var extra_pre_image2 = new Point2D(line.GetRandomIncident(real: true, exclude: new List<Point2D> { center, meetingpoint }));

                // calculate the factors a, b so that extrapoint1 = a * center + b * meetingpoint
                matrix = Matrix<Complex>.Build.DenseOfColumnVectors(center.ToVector(), meetingpoint.ToVector());
                factors = matrix.Solve(extra_pre_image2.ToVector()).ToArray();

                var extra_image2 = new Point2D(factors[0] * center.ToVector() + (factor * factors[1]) * meetingpoint.ToVector());

                pre_images.Add(extra_pre_image2);
                images.Add(extra_image2);
            }

            var rv = new Collineation2D(pre_images, images);

            return rv;
        }

        /// <summary>
        /// The central collineation with respect to a center point, an axis and an extra pre_image and image point.
        /// </summary>
        public static Collineation2D CreateCentralCollineation(Point2D center, Line2D axis, Point2D pre_image, Point2D image)
        {
            var pre_images = new List<Point2D>();
            var images = new List<Point2D>();

            if (pre_image.Equals(center)) throw new ArgumentException("The pre-image point may not be equal to the center of the central collineation");
            if (image.Equals(center)) throw new ArgumentException("The image point may not be equal to the center of the central collineation");
            if (axis.IsIncident(pre_image)) throw new ArgumentException("The pre-image point may not lie on the axis of the central collineation");
            if (axis.IsIncident(image)) throw new ArgumentException("The image point may not lie on the axis of the central collineation");

            var line = pre_image.Join(image);
            if (line == null)
            {
                return new Collineation2D(DenseMatrix.CreateIdentity(3), CoordinateType.Pointwise);
            }
            else
            {
                if (!center.IsIncident(line)) throw new ArgumentException("The pre-image point, the image point and the center of the central collineation must be collinear");
            }

            var exclude = line.Meet(axis);

            pre_images.Add(pre_image);
            images.Add(image);

            var point1 = new Point2D(axis.GetRandomIncident(real: true, exclude: new List<HVector> { center, exclude }));
            var point2 = new Point2D(axis.GetRandomIncident(real: true, exclude: new List<HVector> { center, exclude, point1 }));

            pre_images.Add(point1);
            pre_images.Add(point2);
            images.Add(point1);
            images.Add(point2);

            bool elation = axis.IsIncident(center);

            if (!elation)
            {
                pre_images.Add(center);
                images.Add(center);
            }

            else
            {
                var extraline = new Line2D(center.GetRandomIncident(real: true, exclude: new List<HVector> { axis, line }));

                var extra_pre_image = new Point2D(extraline.GetRandomIncident(real: true, exclude: new List<Point2D> { center }));

                var constructionpoint = pre_image.Join(extra_pre_image).Meet(axis);
                var extra_image = constructionpoint.Join(image).Meet(extraline);

                pre_images.Add(extra_pre_image);
                images.Add(extra_image);
            }

            var rv = new Collineation2D(pre_images, images);

            return rv;
        }

        /// <summary>
        /// The matrix representation of the pointwise matrix of the collineation.
        /// </summary>
        public override string ToString()
        {
            return Matrix.ToMatrixString();
        }
    }

    /// <summary>
    /// A non-singular 2 dimensionsal projective mapping of points to lines, lines to points.
    /// </summary>
    public class Correlation2D
    {
        #region constructors
        /// <summary>
        /// The values of the non-singular 3x3 matrix determine the projectivity.<para>
        /// It must be specified whether the values of the matrix have to be interpreted as pointwise (contravariant) or hyperplanewise (covariant) values.</para><para>
        /// In other words whether the matrix transforms points into lines (pointwise, contravariant) or lines into points (hyperplanewise, covariant).</para>
        /// </summary>
        public Correlation2D(DenseMatrix matrix, CoordinateType coordinateType)
        {
            if (matrix == null) throw new ArgumentNullException("matrix");
            if (matrix.RowCount != matrix.ColumnCount) throw new ArgumentException("3x3 matrix required");
            if (matrix.RowCount != 3) throw new ArgumentException("3x3 matrix required");

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
        /// The values of the non-singular 3x3 matrix are calculated so that 4 given points are mapped to 4 given lines.
        /// </summary>
        public Correlation2D(IEnumerable<Point2D> preimages, IEnumerable<Line2D> images)
        {
            if (preimages == null) throw new ArgumentNullException("preimages");
            if (images == null) throw new ArgumentNullException("images");
            if (preimages.Count() != 4 || images.Count() != 4) throw new ArgumentException("4 images and 4 pre-images required");

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
        public Correlation2D(IEnumerable<Line2D> preimages, IEnumerable<Point2D> images)
        {
            if (preimages == null) throw new ArgumentNullException("preimages");
            if (images == null) throw new ArgumentNullException("images");
            if (preimages.Count() != 4 || images.Count() != 4) throw new ArgumentException("4 images and 4 pre-images required");

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
        /// Transforms points into image lines.
        /// </summary>
        protected DenseMatrix matrix;
        /// <summary>
        /// Transforms lines into image points.
        /// </summary>
        protected DenseMatrix matrixDual;
        #endregion

        #region mappings
        /// <summary>
        /// Transform the given point into its image line.
        /// </summary>
        public Line2D Map(Point2D point)
        {
            if (point == null) throw new ArgumentNullException("point");
            var imageline = point.Multiply(matrix);
            return new Line2D(imageline);
        }
        /// <summary>
        /// Transform the given points into their image lines.
        /// </summary>
        public IEnumerable<Line2D> Map(IEnumerable<Point2D> points)
        {
            foreach (var item in points)
            {
                yield return Map(item);
            }
        }
        /// <summary>
        /// Transform the given line into its image point.
        /// </summary>
        public Point2D Map(Line2D line)
        {
            if (line == null) throw new ArgumentNullException("line");
            var imagepoint = line.Multiply(matrix);
            return new Point2D(imagepoint);
        }
        /// <summary>
        /// Transform the given lines into their image points.
        /// </summary>
        public IEnumerable<Point2D> Map(IEnumerable<Line2D> lines)
        {
            foreach (var item in lines)
            {
                yield return Map(item);
            }
        }
        #endregion
    }
}
