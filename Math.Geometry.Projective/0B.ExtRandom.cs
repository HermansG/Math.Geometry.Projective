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
        /// Pick a random item out of a IEnumerable collection.
        /// </summary>
        public static T PickRandom<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (source.Count() == 0) return default(T);
            return source.ElementAt(ThreadSafeRandom.ThisThreadsRandom.Next(source.Count()));
        }

        /// <summary>
        /// Pick a random number between min (inclusive) and max (exclusive).<para>
        /// The value of 'max' must be greater than or equal to the value of 'min'.</para>
        /// </summary>
        public static int PickRandom(int min, int max)
        {
            return ThreadSafeRandom.ThisThreadsRandom.Next(min, max);
        }

        /// <summary>
        /// Pick a random number between min (inclusive) and max (exclusive) excluding some numbers.<para>
        /// The value of 'max' must be greater than or equal to the value of 'min'.</para><para>
        /// The range between 'min' and 'max' without the excluded numbers may not be empty.</para>
        /// </summary>
        public static int PickRandom(int min, int max, params int[] exclude)
        {
            var range = Enumerable.Range(min, max + 1 - min).Where(i => !exclude.Contains(i)).ToList();
            if (range.Count == 0) throw new ArgumentOutOfRangeException("The range between 'min' and 'max' without the excluded numbers is empty");
            int index = PickRandom(0, range.Count);
            return range[index];
        }

        /// <summary>
        /// Pick +1 or -1 randomly.
        /// </summary>
        public static int PickRandomSign()
        {
            return (ThreadSafeRandom.ThisThreadsRandom.NextDouble() >= 0.5) ? +1 : -1;
        }

        /// <summary>
        /// Get all combinations of k elements out of an IEnumerable with Count elements (i.e. Count over k).
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Combinations<T>(this IEnumerable<T> elements, int k)
        {
            return k == 0 ? new[] { new T[0] } :
                elements.SelectMany((e, i) =>
                elements.Skip(i + 1).Combinations(k - 1)
                        .Select(c => (new[] { e }).Concat(c)));
        }

        /// <summary>
        /// Get all combinations of k elements out of an List with Count elements (i.e. Count over k).
        /// </summary>
        public static List<List<T>> Combinations<T>(this List<T> elements, int k)
        {
            var enumerable = Combinations(elements as IEnumerable<T>, k);
            var rv = new List<List<T>>();
            foreach (var item in enumerable)
            {
                var sublist = new List<T>();
                foreach (var combination in item)
                {
                    sublist.Add(combination);
                }
                rv.Add(sublist);
            }
            return rv;
        }

        /// <summary>
        /// Shuffle the items in a list to get a random order.
        /// </summary>
        public static void Shuffle<T>(this List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Returns a random line, optionally complex.
        /// </summary>
        public static Line3D PickRandomLine(bool real = true)
        {
            Point3D random = new Point3D(Extensions.PickRandomHVector(4, real));
            return random.GetLine(real);
        }

        /// <summary>
        /// Returns a random HVector with 'count' coordinates, optionally complex.
        /// </summary>
        public static HVector PickRandomHVector(int count, bool real = true)
        {
            DenseVector search = new DenseVector(count);

            for (int i = 0; i < count; i++)
            {
                if (real)
                {
                    search[i] = new Complex(Numbersincludingmanyzeros.PickRandom(), 0);
                }
                else
                {
                    search[i] = new Complex(Numbersincludingmanyzeros.PickRandom(), Numbersincludingmanyzeros.PickRandom());
                }
            }

            if (search.IsZero())
            {
                int index = Extensions.PickRandom(0, search.Count);
                search[index] += 1;
                if (!real)
                {
                    search[index] += Extensions.PickRandomSign() * Complex.ImaginaryOne;
                }
            }

            return new HVector(search);
        }

        /// <summary>
        /// Numbers -10 to +10 with 10 extra zero's.
        /// </summary>
        public static readonly List<int> Numbersincludingmanyzeros = Enumerable.Range(-10, 21).Concat(Enumerable.Repeat(0, 10)).ToList();

        /// <summary>
        /// Numbers -10 to +10 excluding 0.
        /// </summary>
        public static readonly List<int> Numbersnotincludingzero = Enumerable.Range(1, 10).Concat(Enumerable.Range(-10, 10)).ToList();
    }

    /// <summary>
    /// Thread safe random generator.
    /// </summary>
    public static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random Local;

        /// <summary>
        /// Thread safe random generator.
        /// </summary>
        public static Random ThisThreadsRandom
        {
            get
            {
                if (Local == null)
                {
                    Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId));
                }
                return Local;
            }
        }
    }
}
