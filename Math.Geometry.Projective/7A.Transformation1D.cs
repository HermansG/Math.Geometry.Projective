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
    /// A non-singular 1 dimensionsal projective projective mapping of 1-D elements to 1-D elements of the same type.
    /// </summary>
    public class Collineation1D
    {
        #region constructors
        /// <summary>
        /// The values of the non-singular 2x2 matrix determine the projectivity.
        /// </summary>
        public Collineation1D(Matrix matrix)
        {
            if (matrix == null) throw new ArgumentNullException("matrix");
            if (matrix.RowCount != matrix.ColumnCount) throw new ArgumentException("2x2 matrix required");
            if (matrix.RowCount != 2) throw new ArgumentException("2x2 matrix required");

            matrix.CoerceZero();
            if (matrix.Determinant().IsZero())
            {
                throw new ArgumentException("singular matrix");
            }
            this.matrix = DenseMatrix.OfMatrix(matrix);
        }
        /// <summary>
        /// The values of the non-singular 2x2 matrix are calculated so that 3 given elements are mapped to 3 other given elements.
        /// </summary>
        public Collineation1D(IEnumerable<Element1D> preimages, IEnumerable<Element1D> images)
        {
            if (preimages == null) throw new ArgumentNullException("preimages");
            if (images == null) throw new ArgumentNullException("images");
            if (preimages.Count() != 3 || images.Count() != 3) throw new ArgumentException("3 images and 3 pre-images required");

            // matrix A: standard -> preimages
            var A = Extensions.CanonicalTransformation(preimages);

            // matrix B: standard -> images
            var B = Extensions.CanonicalTransformation(images);

            // matrix = B * A.Inverse()
            this.matrix = DenseMatrix.OfMatrix(B * A.Inverse());
        }
        #endregion

        #region matrices
        /// <summary>
        /// Transforms 1-D elements into image 1-D elements.
        /// </summary>
        protected DenseMatrix matrix;
        #endregion

        #region mappings
        /// <summary>
        /// Transform the given element into its image element.
        /// </summary>
        public Element1D Map(Element1D element)
        {
            if (element == null) throw new ArgumentNullException("element");
            var imageelement = element.Multiply(matrix);
            return new Element1D(imageelement);
        }
        /// <summary>
        /// Transform the given elements into their image elements.
        /// </summary>
        public IEnumerable<Element1D> Map(IEnumerable<Element1D> elements)
        {
            foreach (var item in elements)
            {
                yield return Map(item);
            }
        }
        #endregion
    }
}
