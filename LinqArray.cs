// MIT License - Copyright (C) Ara 3D, Inc.
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.
//
// LinqArray.cs
// A library for working with pure functional arrays, using LINQ style extension functions. 
// Based on code that originally appeared in https://www.codeproject.com/Articles/140138/Immutable-Array-for-NET

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        T this[int n] { get; }
    }

    /// <summary>
    /// Implements an IArray via a function and a count. 
    /// </summary>
    public struct FunctionalArray<T> : IArray<T>
    {
        public Func<int, T> Function { get; }
        public int Count { get; }
        public T this[int n] { get { return Function(n); } }
        public FunctionalArray(int count, Func<int, T> function) { Count = count; Function = function; }
    }

    /// <summary>
    /// Extension functions for working on any object implementing IArray.
    /// </summary>
    public static class ImmutableArray
    {
        /// <summary>
        /// Returns an IArray from the list of arguments.
        /// </summary>
        public static IArray<T> Create<T>(params T[] args)
        {
            return args.ToIArray();
        }

        /// <summary>
        /// Creates an IArray with the given number of items, that uses the function to return items.
        /// </summary>
        public static IArray<T> Select<T>(this int count, Func<int, T> f)
        {
            return new FunctionalArray<T>(count, f);
        }

        /// <summary>
        /// Creates an IArray from any object implement IList, which includes System.List and System.Array.
        /// </summary>
        public static IArray<T> ToIArray<T>(this IList<T> self)
        {
            return Select(self.Count, i => self[i]);
        }

        /// <summary>
        /// Creates an IArray from any object implementing IEnumerable
        /// </summary>
        public static IArray<T> ToIArray<T>(this IEnumerable<T> self)
        {
            return self.ToList().ToIArray();
        }

        /// <summary>
        /// Creates an IArray by repeating the given item a number of times.
        /// </summary>
        public static IArray<T> Repeat<T>(this T self, int count)
        {
            return Select(count, i => self);
        }

        /// <summary>
        /// Creates an IArray of integers from zero up to one less than the given number.
        /// </summary>
        public static IArray<int> Range(this int self)
        {
            return Select(self, i => i);
        }
        
        /// <summary>
        /// Returns the nth item in the array. Undefined behavior if the array is empty. 
        /// </summary>
        public static T At<T>(this IArray<T> self, int n)
        {
            return self[n];
        }

        /// <summary>
        /// Returns the first itme in the array. Undefined behavior if the array is empty. 
        /// </summary>
        public static T First<T>(this IArray<T> self)
        {
            return self[0];
        }

        /// <summary>
        /// Returns the last itme in the array. Undefined behavior if the array is empty. 
        /// </summary>
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
      
        /// <summary>
        /// A mnemonic for "Any()" that returns true if the count is greater than zero 
        /// </summary>
        public static bool IsEmpty<T>(this IArray<T> self)
        {
            return self.Any();
        }

        /// <summary>
        /// Returns true if there are any elements in the array.
        /// </summary>
        public static bool Any<T>(this IArray<T> self)
        {
            return self.Count == 0;
        }

        /// <summary>
        /// Returns true if there are any elements in the array that satisfy the predicate.
        /// </summary>
        public static bool Any<T>(this IArray<T> self, Func<T, bool> func)
        {
            return self.ToEnumerable().Any(func);
        }

        /// <summary>
        /// Returns true if all elements in the array satisfy the predicate.
        /// </summary>
        public static bool All<T>(this IArray<T> self, Func<T, bool> func)
        {
            return self.ToEnumerable().All(func);
        }

        /// <summary>
        /// Converts the IArray into a system array. 
        /// </summary>
        public static T[] ToArray<T>(this IArray<T> self)
        {
            return self.CopyTo(new T[self.Count]);
        }

        /// <summary>
        /// Converts the IArray into a system list. 
        /// </summary>
        public static List<T> ToList<T>(this IArray<T> self)
        {
            return self.AddTo(new List<T>(self.Count));
        }

        /// <summary>
        /// Forces an immediate evaluation of all items in the IArray, allocating memory, and storing all of the results. 
        /// </summary>
        public static IArray<T> Reify<T>(this IArray<T> self)
        {
            return self.ToArray().ToIArray();
        }

        /// <summary>
        /// Converts an array into an IEnumerable. This is useful for using ForEach for example.
        /// </summary>
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
        
        /// <summary>
        /// Adds all elements of the array to the target collection. 
        /// </summary>
        public static U AddTo<T, U>(this IArray<T> self, U other) where U : ICollection<T>
        {
            foreach (var x in self.ToEnumerable())
                other.Add(x);
            return other;
        }

        /// <summary>
        /// Copies all elements of the array to the target list or array, starting at the provided index. 
        /// </summary>
        public static U CopyTo<T, U>(this IArray<T> self, U other, int destIndex = 0) where U : IList<T>
        {
            for (var i = 0; i < self.Count; ++i)
                other[i + destIndex] = self[i];
            return other;
        }

        /// <summary>
        /// Returns an array generated by applying a function to each element.
        /// </summary>
        public static IArray<U> Select<T, U>(this IArray<T> self, Func<T, U> f)
        {
            return Select(self.Count, i => f(self[i]));
        }

        /// <summary>
        /// Converts an array of array into a flattened array. Each array is assumed to be of size n.
        /// </summary>
        public static IArray<T> Flatten<T>(this IArray<IArray<T>> self, int n)
        {
            return Select(self.Count * n, i => self[i / n][i % n]);
        }

        /// <summary>
        /// Returns an array generated by applying a function to each element.
        /// </summary>
        public static IArray<U> Select<T, U>(this IArray<T> self, Func<T, int, U> f)
        {
            return Select(self.Count, i => f(self[i], i));
        }

        /// <summary>
        /// Converts an array of arrays to an enumerable.
        /// </summary>
        public static IEnumerable<T> SelectMany<T>(this IArray<IArray<T>> self)
        {
            foreach (var xs in self.ToEnumerable())
                foreach (var x in xs.ToEnumerable())
                    yield return x;
        }

        /// <summary>
        /// Returns an array of arrays, where the number of sub-elements is known
        /// </summary>
        public static IArray<T> SelectMany<T>(this IArray<IArray<T>> self, int count)
        {
            return Select(self.Count, i => self[i / count][i % count]);
        }

        /// <summary>
        /// Returns an array generated by applying a function to corresponding pairs of elements in both arrays.
        /// </summary>
        public static IArray<V> Zip<T, U, V>(this IArray<T> self, IArray<U> other, Func<T, U, V> f)
        {
            return Select(Math.Min(self.Count, other.Count), i => f(self[i], other[i]));
        }

        /// <summary>
        /// Returns an array generated by applying a function to corresponding pairs of elements in both arrays.
        /// </summary>
        public static IArray<V> Zip<T, U, V>(this IArray<T> self, IArray<U> other, Func<T, U, int, V> f)
        {
            return Select(Math.Min(self.Count, other.Count), i => f(self[i], other[i], i));
        }

        /// <summary>
        /// Applies a function to each element in the list paired with the next one. 
        /// Used to implement adjacent differences for example.
        /// </summary>
        public static IArray<U> ZipEachWithNext<T, U>(this IArray<T> self, Func<T, T, U> f)
        {
            return self.Zip(self.Skip(), f);
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
            return Select(to - from, i => self[i + from]);
        }

        /// <summary>
        /// Returns n elements of the list starting from a given index.  
        /// </summary>
        public static IArray<T> Subarray<T>(this IArray<T> self, int from, int count)
        {
            return self.Slice(from, count + from);
        }

        /// <summary>
        /// Returns elements of the array between from and skipping every stride element.  
        /// </summary>
        public static IArray<T> Slice<T>(this IArray<T> self, int from, int to, int stride)
        {
            return Select(to - from / stride, i => self[i * stride + from]);
        }

        /// <summary>
        /// Returns a new array containing the elements by taking every nth item.  
        /// </summary>
        public static IArray<T> Stride<T>(this IArray<T> self, int n)
        {
            return Select(self.Count / n, i => self[i * n % self.Count]);
        }

        /// <summary>
        /// Returns a new array containing just the first n items.  
        /// </summary>
        public static IArray<T> Take<T>(this IArray<T> self, int n)
        {
            return self.Slice(0, n);
        }

        /// <summary>
        /// Returns a new array containing the elements after the first n elements.  
        /// </summary>
        public static IArray<T> Skip<T>(this IArray<T> self, int n = 1)
        {
            return self.Slice(n, self.Count);
        }

        /// <summary>
        /// Returns a new array containing the last n elements.  
        /// </summary>
        public static IArray<T> TakeLast<T>(this IArray<T> self, int n = 1)
        {
            return self.Skip(self.Count - n);
        }

        /// <summary>
        /// Returns a new array containing all elements excluding the last n elements.  
        /// </summary>
        public static IArray<T> DropLast<T>(this IArray<T> self, int n = 1)
        {
            return self.Take(self.Count - n);
        }
        
        /// <summary>
        /// Returns a new array by remapping indices 
        /// </summary>
        public static IArray<T> MapIndices<T>(this IArray<T> self, Func<int, int> f)
        {
            return self.Count.Select(i => self[f(i)]);
        }

        /// <summary>
        /// Returns a new array that reverses the order of elements 
        /// </summary>
        public static IArray<T> Reverse<T>(this IArray<T> self)
        {
            return self.MapIndices(i => self.Count - 1 - i);
        }

        /// <summary>
        /// Uses the new array to access elements of the first array.
        /// </summary>
        public static IArray<T> SelectByIndex<T>(this IArray<T> self, IArray<int> indices)
        {
            return indices.Select(i => self[i]);
        }

        /// <summary>
        /// Similiar to take, if count is less than the number of items in the array, otherwise 
        /// uses a modulo operation. 
        /// </summary>
        public static IArray<T> Resize<T>(this IArray<T> self, int count)
        {
            return Select(count, i => self[i % self.Count]);
        }

        /// <summary>
        /// Returns an array of the same type with no elements. 
        /// </summary>
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

        /// <summary>
        /// Converts an array of elements into a string representation
        /// </summary>
        public static string Join<T>(this IArray<T> self, string sep = " ")
        {
            return self.Aggregate(new StringBuilder(), (sb, x) => sb.Append(x).Append(sep)).ToString();
        }

        /// <summary>
        /// Concatenates the contents of one array with another.
        /// </summary>
        public static IArray<T> Concatenate<T>(this IArray<T> self, IArray<T> other)
        {
            return Select(self.Count + other.Count, i => i < self.Count ? self[i] : other[i - self.Count]);
        }
    
        /// <summary>
        /// Returns the index of the first element matching the given item.
        /// </summary>
        public static int IndexOf<T>(this IArray<T> self, T item) where T : IEquatable<T>
        {
            for (var i = 0; i < self.Count; ++i)
                if (self[i].Equals(item))
                    return i;
            return -1;
        }

        /// <summary>
        /// Returns the index of the last element matching the given item.
        /// </summary>
        public static int LastIndexOf<T>(this IArray<T> self, T item) where T : IEquatable<T>
        {
            int n = self.Reverse().IndexOf(item);
            return n < 0 ? n : self.Count - 1 - n;
        }

        /// <summary>
        /// Returns an array that is one element shorter that subtracts each element from its previous one. 
        /// </summary>
        public static IArray<int> AdjacentDifferences(this IArray<int> self)
        {
            return self.ZipEachWithNext((a, b) => b - a);
        }

        /// <summary>
        /// Creates a new array that concatenates a unit item list of one item after it.       
        /// </summary>
        public static IArray<T> Append<T>(this IArray<T> self, T x)
        {
            return self.Concatenate(x.Repeat(1));
        }

        /// <summary>
        /// Creates a new array that concatenates a unit item list of one item before it   
        /// </summary>
        public static IArray<T> Prepend<T>(this IArray<T> self, T x)
        {
            return x.Repeat(1).Concatenate(self);
        }


        /// <summary>
        /// Returns the element at the nth position, where n is modulo the number of items in the arrays.
        /// </summary>
        public static T ElementAt<T>(this IArray<T> self, int n)
        {
            return self[n];
        }

        /// <summary>
        /// Returns the element at the nth position, where n is modulo the number of items in the arrays.
        /// </summary>
        public static T ElementAtModulo<T>(this IArray<T> self, int n)
        {
            return self.ElementAt(n % self.Count);
        }
    }
}
