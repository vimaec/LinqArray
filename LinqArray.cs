// MIT License - Copyright (C) Ara 3D, Inc.
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.
//
// LinqArray.cs
// A library for working with pure functional arrays, using LINQ style extension functions. 
// Based on code that originally appeared in https://www.codeproject.com/Articles/140138/Immutable-Array-for-NET
// 

using System;
using System.Collections.Generic;
using System.Linq;

namespace Ara3D
{
    /// <summary>
    /// A pure functional interface to arrays. 
    /// </summary>
    public interface IArray<T>
    {
        /// <summary>
        /// Number of items in the array.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Accessor to the nth item in the array.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        T this[int n] { get; }
    }

    /// <summary>
    /// Implements an IArray via a function and a count. 
    /// </summary>
    public class FunctionalArray<T> : IArray<T>
    {
        public Func<int, T> Function { get; }
        public int Count { get; }
        public T this[int n] { get { return Function(n); } }
        public FunctionalArray(int count, Func<int, T> function) { Count = count; Function = function; }
    }

    public static class ImmutableArray
    {
        // Constructor functions 

        public static IArray<T> Create<T>(int count, Func<int, T> f)
        {
            return new FunctionalArray<T>(count, f);
        }        

        public static IArray<T> ToIArray<T>(this IList<T> self)
        {
            return Create(self.Count, i => self[i]);
        }

        public static IArray<T> ToIArray<T>(this IEnumerable<T> self)
        {
            return self.ToList().ToIArray();
        }

      
        public static IArray<T> Repeat<T>(this T self, int count)
        {
            return Create(count, i => self);
        }

        public static IArray<T> Unit<T>(this T self)
        {
            return self.Repeat(1);
        }

        public static IArray<T> Nil<T>(this T self)
        {
            return self.Repeat(0);
        }

        public static IArray<int> Range(this int n)
        {
            return Create(n, i => i);
        }

        // Helper accessors

        public static T First<T>(this IArray<T> self)
        {
            return self[0];
        }

        public static T Last<T>(this IArray<T> self)
        {
            return self[self.Count - 1];
        }

        /// <summary>
        /// Returns true if and only if the argument is a valid index into the array.
        /// </summary>
        public static bool InRange<T>(this IArray<T> self, int n)
        {
            return n >= 0 && n < self.Count;
        }

        // 

        /// <summary>
        /// A mnemonic for "Any()" that returns true if the count is greater than zero 
        /// </summary>
        public static bool IsEmpty<T>(this IArray<T> self)
        {
            return self.Any();
        }

        public static bool Any<T>(this IArray<T> self)
        {
            return self.Count == 0;
        }

        public static bool Any<T>(this IArray<T> self, Func<T, bool> func)
        {
            return self.ToEnumerable().Any(func);
        }

        public static bool All<T>(this IArray<T> self, Func<T, bool> func)
        {
            return self.ToEnumerable().All(func);
        }

        // Converters 

        public static T[] ToArray<T>(this IArray<T> self)
        {
            return self.CopyTo(new T[self.Count]);
        }

        public static List<T> ToList<T>(this IArray<T> self)
        {
            return self.AddTo(new List<T>(self.Count));
        }

        public static IEnumerable<T> ToEnumerable<T>(this IArray<T> self)
        {
            for (var i = 0; i < self.Count; ++i)
                yield return self[i];
        }

        /// <summary>
        /// Converts the array into a function that returns values from an integer, returning a default value if out of range.  
        /// </summary>
        public static Func<int, T> ToFunction<T>(this IArray<T> self, T def = default(T))
        {
            return i => self.InRange(i) ? self[i] : def;
        }

        /// <summary>
        /// Converts the array into a predicate (a function that returns true or false) based on the truth of the given value.         
        /// </summary>
        public static Func<int, bool> ToPredicate(this IArray<bool> self)
        {
            return self.ToFunction(false);
        }

        // Copying

        public static U AddTo<T, U>(this IArray<T> self, U other) where U : ICollection<T>
        {
            foreach (var x in self.ToEnumerable())
                other.Add(x);
            return other;
        }

        public static U CopyTo<T, U>(this IArray<T> self, U other, int destIndex = 0) where U : IList<T>
        {
            for (var i = 0; i < self.Count; ++i)
                other[i + destIndex] = self[i];
            return other;
        }

        // Select (aka Map)

        /// <summary>
        /// Returns an array generated by applying a function to each element.
        /// </summary>
        public static IArray<U> Select<T, U>(this IArray<T> self, Func<T, U> f)
        {
            return Create(self.Count, i => f(self[i]));
        }

        /// <summary>
        /// Returns an array generated by applying a function to each element.
        /// </summary>
        public static IArray<U> Select<T, U>(this IArray<T> self, Func<T, int, U> f)
        {
            return Create(self.Count, i => f(self[i], i));
        }

        /// <summary>
        /// Returns an array generated by applying a function to corresponding pairs of elements in both arrays.
        /// </summary>
        public static IArray<V> Zip<T, U, V>(this IArray<T> self, IArray<U> other, Func<T, U, V> f)
        {
            return Create(Math.Min(self.Count, other.Count), i => f(self[i], other[i]));
        }

        /// <summary>
        /// Returns an array generated by applying a function to corresponding pairs of elements in both arrays.
        /// </summary>
        public static IArray<V> Zip<T, U, V>(this IArray<T> self, IArray<U> other, Func<T, U, int, V> f)
        {
            return Create(Math.Min(self.Count, other.Count), i => f(self[i], other[i], i));
        }

        /// <summary>
        /// Returns an IEnumerable containing only elements of the array that satisfy a specific predicate.
        /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<T> Where<T>(this IArray<T> self, Func<T, bool> f)
        {
            return self.ToEnumerable().Where(f);
        }

        /// <summary>
        /// Returns an IEnumerable containing only elements of the array for which the function returns true.
        /// This variant passes an index to the the function. 
        /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<T> Where<T>(this IArray<T> self, Func<T, int, bool> f)
        {
            return self.ToEnumerable().Where(f);
        }

        /// <summary>
        /// Returns an IEnumerable containing only elements of the array for which the function returns true on the index.
        /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<T> WhereIndices<T>(this IArray<T> self, Func<int, bool> f)
        {
            return self.Where((x, i) => f(i));
        }

        /// <summary>
        /// Returns an IEnumerable containing only elements of the array for which the corresponding mask is true.
        /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<T> Where<T>(this IArray<T> self, IArray<bool> mask)
        {
            return self.WhereIndices(mask.ToPredicate());
        }

        /// <summary>
        /// Returns an IEnumerable containing only indices of the array for which the function satisfies a specific predicate.
        /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<int> IndicesWhere<T>(this IArray<T> self, Func<T, bool> f)
        {
            return self.Indices().ToEnumerable().Where(i => f(self[i]));
        }

        /// <summary>
        /// Returns an IEnumerable containing only indices of the array for which the function satisfies a specific predicate.
        /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<int> IndicesWhere<T>(this IArray<T> self, Func<T, int, bool> f)
        {
            return self.IndicesWhere(i => f(self[i], i));
        }

        /// <summary>
        /// Returns an IEnumerable containing only indices of the array for which the function satisfies a specific predicate.
        /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<int> IndicesWhere<T>(this IArray<T> self, Func<int, bool> f)
        {
            return self.Indices().ToEnumerable().Where(i => f(i));
        }

        /// <summary>
        /// Returns an IEnumerable containing only indices of the array for which booleans in the mask are true.
        /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<int> IndicesWhere<T>(this IArray<T> self, IArray<bool> mask)
        {
            return self.IndicesWhere(mask.ToPredicate());
        }

        /// <summary>
        /// Shortcut for ToEnumerable.Aggregate()
        /// </summary>
        public static U Aggregate<T, U>(this IArray<T> self, U init, Func<U, T, U> func)
        {
            return self.ToEnumerable().Aggregate(init, func);
        }

        /// <summary>
        /// Returns a new array containing the elements in the range of from to to.  
        /// </summary>
        public static IArray<T> Slice<T>(this IArray<T> self, int from, int to)
        {
            return Create(to - from, i => self[i + from]);
        }

        public static IArray<T> Slice<T>(this IArray<T> self, int from, int to, int stride)
        {
            return Create(to - from / stride, i => self[i * stride + from]);
        }

        public static IArray<T> Stride<T>(this IArray<T> self, int stride)
        {
            return Create(self.Count / stride, i => self[i * stride % self.Count]);
        }

        public static IArray<T> Take<T>(this IArray<T> self, int n)
        {
            return self.Slice(0, n);
        }

        public static IArray<T> Skip<T>(this IArray<T> self, int n)
        {
            return self.Slice(n, self.Count);
        }

        public static IArray<T> TakeLast<T>(this IArray<T> self, int n)
        {
            return self.Skip(self.Count - n);
        }

        public static IArray<T> DropLast<T>(this IArray<T> self, int n)
        {
            return self.Take(self.Count - n);
        }

        // Remapping of indices 

        public static IArray<T> MapIndices<T>(this IArray<T> self, Func<int, int> f)
        {
            return self.Select((x, i) => self[f(i)]);
        }

        public static IArray<T> Reverse<T>(this IArray<T> self)
        {
            return self.MapIndices(i => self.Count - 1 - i);
        }

        public static IArray<T> SelectByIndex<T>(this IArray<T> self, IArray<int> indices)
        {
            return indices.Select(i => self[i]);
        }

        // Resizing, if bigger than the original uses a modulo operation 

        /// <summary>
        /// Similiar to take, if count is less than the number of items in the array, otherwise 
        /// uses a modulo operation. 
        /// </summary>
        public static IArray<T> Resize<T>(this IArray<T> self, int count)
        {
            return Create(count, i => self[i % self.Count]);
        }

        public static IArray<T> Clear<T>(this IArray<T> self)
        {
            return self.Take(0);
        }

        /// <summary>
        /// Returns a sequence of integers from 0 to 1 less than the number of items in the array, representing indicies of the array.
        /// </summary>
        public static IArray<int> Indices<T>(this IArray<T> self)
        {
            return self.Count.Range();
        }
    }
}
