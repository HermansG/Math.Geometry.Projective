using System;
using System.Linq;
using System.Collections.Generic;

namespace Geometry.Projective
{
    using System.Numerics;
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Complex;

    /// <summary>
    /// A homogeneous vector with 2 complex (or real) homogeneous coordinates, representing a element in 1-dimensional projective space.<para>
    /// When the first coordinate is 0, the element is at infinity.</para><para>
    /// When the first coordinate is 1, the other coordinate is the affine or euclidean coordinate of the element.</para>
    /// </summary>
    public class Element1D : HVector
    {
        #region constructors
        /// <summary>
        /// The vector data are copied into the coordinates of the new element.
        /// </summary>
        public Element1D(HVector hvector)
            : base(hvector)
        {
            if (hvector.Count != 2) throw new ArgumentException("hvector must have 2 coordinates");
        }
        /// <summary>
        /// The vector data are copied into the coordinates of the new element.
        /// </summary>
        public Element1D(Vector<Complex> vector)
            : base(vector)
        {
            if (vector.Count != 2) throw new ArgumentException("vector must have 2 coordinates");
        }
        /// <summary>
        /// The values are copied into the coordinates of the new element.
        /// </summary>
        public Element1D(Complex[] values)
            : base(values)
        {
            if (values.Length != 2) throw new ArgumentException("values must have 2 entries");
        }
        /// <summary>
        /// The values are copied into the data of the new element.
        /// </summary>
        public Element1D(double[] values)
            : base(values)
        {
            if (values.Length != 2) throw new ArgumentException("values must have 2 entries");
        }
        /// <summary>
        /// The affine or Euclidean value (complex or real) is copied as the coordinate of the new element.<para>
        /// The first coordinate will be One.</para>
        /// </summary>
        public Element1D(Complex x) : this(new Complex[] { Complex.One, x }) { }
        /// <summary>
        /// The values (complex or real) are copied into the coordinates of the new element.
        /// </summary>
        public Element1D(Complex x0, Complex x1) : this(new Complex[] { x0, x1 }) { }
        #endregion

        /// <summary>
        /// Create a new element, identical to this one.
        /// </summary>
        public new Element1D Clone() { return new Element1D(this.vector); }

        #region meet and join
        /// <summary>
        /// Check whether a given hvector is incident with this element.
        /// </summary>
        [Obsolete("Incidence for 1-dimensional elements has no specific meaning.")]
        public new bool IsIncident(HVector other) { return base.IsIncident(other); }
        #endregion

        /// <summary>
        /// Interpret the element as an Euclidean element and return its distance from the origin.
        /// </summary>
        public double DistanceOrigin()
        {
            if (this[0].IsZero()) return double.PositiveInfinity;
            else return (this[1] / this[0]).Magnitude;
        }

        /// <summary>
        /// A string representation for the corresponding 1-dimensional affine or euclidean coordinates of the element.<para>
        /// When the element is at infinity, "infinity", is printed.</para>
        /// </summary>
        public string ToAffineString()
        {
            if (vector[0].IsZero())
            {
                return "infinity";
            }
            else
            {
                Complex[] array = new Complex[] { vector[1] / vector[0] };
                return array.ToVectorString();
            }
        }

        #region constants
        /// <summary>
        /// (1 0)
        /// </summary>
        public static readonly Element1D Origin = new Element1D(1, 0);
        /// <summary>
        /// (0 1)
        /// </summary>
        public static readonly Element1D Infinity = new Element1D(0, 1);
        /// <summary>
        /// (1 1)
        /// </summary>
        public static readonly Element1D Unity = new Element1D(1, 1);
        #endregion
    }
}
