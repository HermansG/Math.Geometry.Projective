using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geometry.Projective
{
    using System.Numerics;
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Complex;
    using System.Collections.ObjectModel;

    /// <summary>
    /// A set of points, lines or planes or other homogeneous vectors of the same type and dimension.
    /// </summary>
    public class Set<T> where T : HVector
    {
        /// <summary>
        /// The values become the elements of the set.<para>
        /// All elements must be of the same type and dimension.</para>
        /// </summary>
        public Set(params T[] elements)
        {
            if (elements == null) throw new ArgumentNullException("elements");
            init(elements);
        }

        /// <summary>
        /// The values become the elements of the set.<para>
        /// All elements must be of the same type and dimension.</para>
        /// </summary>
        public Set(IEnumerable<T> elements)
        {
            if (elements == null) throw new ArgumentNullException("elements");
            init(elements.ToArray());
        }

        void init(T[] elements)
        {
            if (elements.Length <= 1) throw new ArgumentException("set must contain at least two elements");
            this.elements = new T[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] == null) throw new ArgumentNullException("elements[" + i + "]");
                this.elements[i] = elements[i];
                if (i > 0)
                {
                    if (elements[i].Count != elements[0].Count)
                    {
                        throw new ArgumentException("homogeneous vectors in a set must all have the same number of coordinates");
                    }
                    if (elements[i].GetType() != elements[0].GetType())
                    {
                        throw new ArgumentException("homogeneous vectors in a set must all be of the same type");
                    }
                }
            }
        }

        /// <summary>
        /// The element of the set at position 'index'.
        /// </summary>
        public T this[int index]
        {
            get
            {
                if (index > Count - 1) throw new IndexOutOfRangeException();
                return elements[index];
            }
        }

        T[] elements;

        /// <summary>
        /// The number of elements in the set.
        /// </summary>
        public int Count { get { return elements.Length; } }

        /// <summary>
        /// The number of coordinates of each element in the set.
        /// </summary>
        public int HVectorDimension { get { return elements[0].Count; } }

        /// <summary>
        /// Convert the set to a set of raw homogeneous vectors.
        /// </summary>
        public Set<HVector> ToHVector()
        {
            return new Set<HVector>(elements);
        }
    }

    /// <summary>
    /// A list with Values of type <typeparamref name ="T"/> and a Function: complex t => <typeparamref name ="T"/>(t) so that new elements of type <typeparamref name ="T"/> for the list can be calculated.
    /// </summary>
    /// <typeparam name="T">The element type of the parameter list.</typeparam>
    public class ParameterList<T>
    {
        /// <summary>
        /// Create a new, empty list with Values of type <typeparamref name ="T"/> and a Function: complex t => <typeparamref name ="T"/>(t) so that new elements of type T for the list can be calculated.
        /// </summary>
        public ParameterList(Func<Complex, T> function)
        {
            Function = function;
        }
        /// <summary>
        /// The Function: complex t => <typeparamref name ="T"/>(t) with which new elements of type <typeparamref name ="T"/> for the list can be calculated.
        /// </summary>
        public Func<Complex, T> Function { get; private set; }
        /// <summary>
        /// The number of elements in the list.
        /// </summary>
        public int Count { get { return ValuesAndParameters.Count; } }
        /// <summary>
        /// The list with Values of type <typeparamref name ="T"/>
        /// </summary>
        public List<T> Values { get { return ValuesAndParameters.Select(v => v.Item2).ToList(); } }
        /// <summary>
        /// The list with Values of type <typeparamref name ="T"/> and complex parameters t with which they were calculated.<para>
        /// The parameter t is 'null' when the elements of type <typeparamref name ="T"/> were added without calculation.</para>
        /// </summary>
        public List<Tuple<Complex?, T>> ValuesAndParameters = new List<Tuple<Complex?, T>>();
        /// <summary>
        /// Calculate the element <typeparamref name ="T"/> using t as parameter and when not null, add it to the list.
        /// </summary>
        public T Add(Complex t)
        {
            T newelement = Function(t);
            if (newelement != null)
            {
                ValuesAndParameters.Add(Tuple.Create((Complex?)t, newelement));
            }
            return newelement;
        }
        /// <summary>
        /// Add an element of type <typeparamref name ="T"/> to the list, without calculation, with or without the parameter t.<para>
        /// When t is given, it is not checked that newelement == Function(t).</para>
        /// </summary>
        public void Add(T newelement, Complex? t = null)
        {
            if (newelement != null)
            {
                ValuesAndParameters.Add(Tuple.Create(t, newelement));
            }
        }
        /// <summary>
        /// Add an element of type <typeparamref name ="T"/> to the list, without calculation, with or without the parameter t.<para>
        /// When t is given, it is not checked that newelement == Function(t).</para>
        /// </summary>
        public void Add(Tuple<Complex?, T> newelement)
        {
            if (newelement != null && newelement.Item2 != null)
            {
                ValuesAndParameters.Add(newelement);
            }
        }
        /// <summary>
        /// Add a range of elements of type <typeparamref name ="T"/> to the list, without calculation, without the parameters t.
        /// </summary>
        public void AddRange(IEnumerable<T> newelements)
        {
            foreach (var newelement in newelements)
            {
                Add(newelement);
            }
        }
        /// <summary>
        /// Add a range of elements of type <typeparamref name ="T"/> to the list, with or without calculation, with or without the parameters t.<para>
        /// When some of the parameters t are given, it is not checked that for each newelement == Function(t).</para>
        /// </summary>
        public void AddRange(IEnumerable<Tuple<Complex?, T>> newelements)
        {
            foreach (var newelement in newelements)
            {
                Add(newelement);
            }
        }
        /// <summary>
        /// Create a parameter list of type <typeparamref name ="T"/> as a chain of a parameter list of type <typeparamref name ="U"/> and and a Function: <typeparamref name ="U"/> => <typeparamref name ="T"/> so that new elements of type <typeparamref name ="T"/> for the list can be calculated.
        /// </summary>
        /// <typeparam name="U">The element type of the new parameter list.</typeparam>
        public ParameterList<U> Chain<U>(Func<T, U> function)
        {
            Func<Complex, U> chain = t => function(Function(t));

            var rv = new ParameterList<U>(chain);

            foreach (var item in ValuesAndParameters)
            {
                if (item.Item1 == null)
                {
                    rv.Add(function(item.Item2));
                }
                else
                {
                    var value = function(item.Item2);
                    if (value != null)
                    {
                        rv.Add(Tuple.Create(item.Item1, value));
                    }
                }
            }

            return rv;
        }
    }

    public class ProjectionList
    {
        public ProjectionList(ProjectionType projectiontype)
        {
            Values = new List<Vector2>();
            ProjectionType = projectiontype;
        }

        public ProjectionType ProjectionType;
        public List<Vector2> Values;
    }
}
