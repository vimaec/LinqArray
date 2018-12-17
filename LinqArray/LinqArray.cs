// MIT License - Copyright (C) Ara 3D, Inc.
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.
//
// LinqArray.cs
// A library for working with pure functional arrays, using LINQ style extension functions. 
// Based on code that originally appeared in https://www.codeproject.com/Articles/140138/Immutable-Array-for-NET

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ara3D
{
    /// <summary>
    /// For my own types, I just implement this interface. 
    /// </summary>
    public interface IArray<T>
    {
        T this[int n] { get; }
        int Count { get; }
    }

    /// <summary>
    /// An enumerator implementation for iterating over an arbitrary IReadOnlyList
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ArrayEnumerator<T> : IEnumerator<T>
    {
        public ArrayEnumerator(IReadOnlyList<T> source) { Index = -1; Source = source; }
        public int Index;
        public IReadOnlyList<T> Source;
        public T Current => Source[Index];
        object IEnumerator.Current => Source[Index];
        public void Dispose() {}
        public bool MoveNext() { if (++Index < Source.Count) return true; --Index; return false; }
        public void Reset() { Index = -1; }
    }

    /// <summary>
    /// Implements an IReadOnlyList via a function and a count. 
    /// </summary>
    public struct FunctionalArray<T> : IReadOnlyList<T>
    {
        public Func<int, T> Function { get; }
        public int Count { get; }
        public T this[int n] { get { return Function(n); } }
        public FunctionalArray(int count, Func<int, T> function) { Count = count; Function = function; }
        public IEnumerator<T> GetEnumerator() { return new ArrayEnumerator<T>(this); }        
        IEnumerator IEnumerable.GetEnumerator() {  return new ArrayEnumerator<T>(this); }
    }

    /// <summary>
    /// Implements an IReadOnlyList via a function and a count. 
    /// </summary>
    public struct ReadOnlyListAdapter<T> : IReadOnlyList<T>
    {
        public IArray<T> Source { get; }
        public int Count => Source.Count;
        public T this[int n] { get { return Source[n]; } }
        public ReadOnlyListAdapter(IArray<T> source) { Source = source; }
        public IEnumerator<T> GetEnumerator() { return new ArrayEnumerator<T>(this); }
        IEnumerator IEnumerable.GetEnumerator() { return new ArrayEnumerator<T>(this); }
    }

    /// <summary>
    /// Extension functions for working on any object implementing IReadOnlyList. This overrides 
    /// many of the Linq functions providing better performance. 
    /// </summary>
    public static class LinqArray
    {
        /// <summary>
        /// A helper function to enable IArray to support IEnumerable 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <returns></returns>
        public static ReadOnlyListAdapter<T> ToReadOnlyList<T>(this IArray<T> self)
        {
            return new ReadOnlyListAdapter<T>(self);
        }

        /// <summary>
        /// Creates an IReadOnlyList with the given number of items, that uses the function to return items.
        /// </summary>
        public static IReadOnlyList<T> Select<T>(this int count, Func<int, T> f)
        {
            return new FunctionalArray<T>(count, f);
        }

        /// <summary>
        /// Creates an IReadOnlyList by repeating the given item a number of times.
        /// </summary>
        public static IReadOnlyList<T> Repeat<T>(this T self, int count)
        {
            return Select(count, i => self);
        }

        /// <summary>
        /// Creates an IReadOnlyList of integers from zero up to one less than the given number.
        /// </summary>
        public static IReadOnlyList<int> Range(this int self)
        {
            return Select(self, i => i);
        }
        
        /// <summary>
        /// Returns the first itme in the array. 
        /// </summary>
        public static T First<T>(this IReadOnlyList<T> self)
        {
            return self[0];
        }

        /// <summary>
        /// Returns the last itme in the array. 
        /// </summary>
        public static T Last<T>(this IReadOnlyList<T> self)
        {
            return self[self.Count - 1];
        }

        /// <summary>
        /// Returns true if and only if the argument is a valid index into the array.
        /// </summary>
        public static bool InRange<T>(this IReadOnlyList<T> self, int n)
        {
            return n >= 0 && n < self.Count;
        }
      
        /// <summary>
        /// A mnemonic for "Any()" that returns true if the count is greater than zero 
        /// </summary>
        public static bool IsEmpty<T>(this IReadOnlyList<T> self)
        {
            return self.Any();
        }

        /// <summary>
        /// Returns true if there are any elements in the array.
        /// </summary>
        public static bool Any<T>(this IReadOnlyList<T> self)
        {
            return self.Count == 0;
        }

        /// <summary>
        /// Converts the IReadOnlyList into a system array. 
        /// </summary>
        public static T[] ToArray<T>(this IReadOnlyList<T> self)
        {
            return self.CopyTo(new T[self.Count]);
        }

        /// <summary>
        /// Forces an immediate evaluation of all items in the IReadOnlyList, allocating memory, and storing all of the results. 
        /// </summary>
        public static IReadOnlyList<T> Reify<T>(this IReadOnlyList<T> self)
        {
            return self.ToArray();
        }

        /// <summary>
        /// Converts the array into a function that returns values from an integer, returning a default value if out of range.  
        /// </summary>
        public static Func<int, T> ToFunction<T>(this IReadOnlyList<T> self, T def = default(T))
        {
            return i => self.InRange(i) ? self[i] : def;
        }

        /// <summary>
        /// Converts the array into a predicate (a function that returns true or false) based on the truth of the given value.         
        /// </summary>
        public static Func<int, bool> ToPredicate(this IReadOnlyList<bool> self)
        {
            return self.ToFunction(false);
        }
        
        /// <summary>
        /// Adds all elements of the array to the target collection. 
        /// </summary>
        public static U AddTo<T, U>(this IReadOnlyList<T> self, U other) where U : ICollection<T>
        {
            foreach (var x in self)
                other.Add(x);
            return other;
        }

        /// <summary>
        /// Copies all elements of the array to the target list or array, starting at the provided index. 
        /// </summary>
        public static U CopyTo<T, U>(this IReadOnlyList<T> self, U other, int destIndex = 0) where U : IList<T>
        {
            for (var i = 0; i < self.Count; ++i)
                other[i + destIndex] = self[i];
            return other;
        }

        /// <summary>
        /// Returns an array generated by applying a function to each element.
        /// </summary>
        public static IReadOnlyList<U> Select<T, U>(this IReadOnlyList<T> self, Func<T, U> f)
        {
            return Select(self.Count, i => f(self[i]));
        }

        /// <summary>
        /// Converts an array of array into a flattened array. Each array is assumed to be of size n.
        /// </summary>
        public static IReadOnlyList<T> Flatten<T>(this IReadOnlyList<IReadOnlyList<T>> self, int n)
        {
            return Select(self.Count * n, i => self[i / n][i % n]);
        }

        /// <summary>
        /// Converts an array of array into a flattened array.
        /// </summary>
        public static IReadOnlyList<T> Flatten<T>(this IReadOnlyList<IReadOnlyList<T>> self)
        {
            var counts = self.Select(x => x.Count).PostAccumulate((x,y) => x + y);
            var r = new T[counts.Last()];
            var i = 0;
            foreach (var xs in self)
            {
                xs.CopyTo(r, counts[i++]);
            }
            return r;
        }

        /// <summary>
        /// Returns an array generated by applying a function to each element.
        /// </summary>
        public static IReadOnlyList<U> Select<T, U>(this IReadOnlyList<T> self, Func<T, int, U> f)
        {
            return Select(self.Count, i => f(self[i], i));
        }

        /// <summary>
        /// Returns an array of arrays, where the number of sub-elements is known
        /// </summary>
        public static IReadOnlyList<T> SelectMany<T>(this IReadOnlyList<IReadOnlyList<T>> self, int count)
        {
            return Select(self.Count, i => self[i / count][i % count]);
        }

        /// <summary>
        /// Returns an array generated by applying a function to corresponding pairs of elements in both arrays.
        /// </summary>
        public static IReadOnlyList<V> Zip<T, U, V>(this IReadOnlyList<T> self, IReadOnlyList<U> other, Func<T, U, V> f)
        {
            return Select(Math.Min(self.Count, other.Count), i => f(self[i], other[i]));
        }

        /// <summary>
        /// Returns an array generated by applying a function to corresponding pairs of elements in both arrays.
        /// </summary>
        public static IReadOnlyList<V> Zip<T, U, V>(this IReadOnlyList<T> self, IReadOnlyList<U> other, Func<T, U, int, V> f)
        {
            return Select(Math.Min(self.Count, other.Count), i => f(self[i], other[i], i));
        }

        /// <summary>
        /// Applies a function to each element in the list paired with the next one. 
        /// Used to implement adjacent differences for example.
        /// </summary>
        public static IReadOnlyList<U> ZipEachWithNext<T, U>(this IReadOnlyList<T> self, Func<T, T, U> f)
        {
            return self.Zip(self.Skip(), f);
        }                

        /// <summary>
        /// Returns an IEnumerable containing only elements of the array for which the function returns true on the index.
        /// An IReadOnlyList is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<T> WhereIndices<T>(this IReadOnlyList<T> self, Func<int, bool> f)
        {
            return self.Where((x, i) => f(i));
        }

        /// <summary>
        /// Returns an IEnumerable containing only elements of the array for which the corresponding mask is true.
        /// An IReadOnlyList is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<T> Where<T>(this IReadOnlyList<T> self, IReadOnlyList<bool> mask)
        {
            return self.WhereIndices(mask.ToPredicate());
        }

        /// <summary>
        /// Returns an IEnumerable containing only indices of the array for which the function satisfies a specific predicate.
        /// An IReadOnlyList is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<int> IndicesWhere<T>(this IReadOnlyList<T> self, Func<T, bool> f)
        {
            return self.Indices().Where(i => f(self[i]));
        }

        /// <summary>
        /// Returns an IEnumerable containing only indices of the array for which the function satisfies a specific predicate.
        /// An IReadOnlyList is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<int> IndicesWhere<T>(this IReadOnlyList<T> self, Func<T, int, bool> f)
        {
            return self.IndicesWhere(i => f(self[i], i));
        }

        /// <summary>
        /// Returns an IEnumerable containing only indices of the array for which the function satisfies a specific predicate.
        /// An IReadOnlyList is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<int> IndicesWhere<T>(this IReadOnlyList<T> self, Func<int, bool> f)
        {
            return self.Indices().Where(i => f(i));
        }

        /// <summary>
        /// Returns an IEnumerable containing only indices of the array for which booleans in the mask are true.
        /// An IReadOnlyList is not created automatically because it is an expensive operation that is potentially unneeded.
        /// </summary>
        public static IEnumerable<int> IndicesWhere<T>(this IReadOnlyList<T> self, IReadOnlyList<bool> mask)
        {
            return self.IndicesWhere(mask.ToPredicate());
        }

        /// <summary>
        /// Shortcut for ToEnumerable.Aggregate()
        /// </summary>
        public static U Aggregate<T, U>(this IReadOnlyList<T> self, U init, Func<U, T, U> func)
        {
            for (var i = 0; i < self.Count; ++i)
                init = func(init, self[i]);
            return init;
        }

        /// <summary>
        /// Shortcut for ToEnumerable.Aggregate()
        /// </summary>
        public static U Aggregate<T, U>(this IReadOnlyList<T> self, Func<U, T, U> func)
        {
            return Aggregate(self, default(U), func);
        }

        /// <summary>
        /// Shortcut for ToEnumerable.Aggregate()
        /// </summary>
        public static U Aggregate<T, U>(this IReadOnlyList<T> self, U init, Func<U, T, int, U> func)
        {
            for (var i = 0; i < self.Count; ++i)
                init = func(init, self[i], i);
            return init;
        }

        /// <summary>
        /// Shortcut for ToEnumerable.Aggregate()
        /// </summary>
        public static U Aggregate<T, U>(this IReadOnlyList<T> self, Func<U, T, int, U> func)
        {
            return Aggregate(self, default(U), func);
        }

        /// <summary>
        /// Returns a new array containing the elements in the range of from to to.  
        /// </summary>
        public static IReadOnlyList<T> Slice<T>(this IReadOnlyList<T> self, int from, int to)
        {
            return Select(to - from, i => self[i + from]);
        }

        /// <summary>
        /// Returns n elements of the list starting from a given index.  
        /// </summary>
        public static IReadOnlyList<T> SubArray<T>(this IReadOnlyList<T> self, int from, int count)
        {
            return self.Slice(from, count + from);
        }

        /// <summary>
        /// Returns elements of the array between from and skipping every stride element.  
        /// </summary>
        public static IReadOnlyList<T> Slice<T>(this IReadOnlyList<T> self, int from, int to, int stride)
        {
            return Select(to - from / stride, i => self[i * stride + from]);
        }

        /// <summary>
        /// Returns a new array containing the elements by taking every nth item.  
        /// </summary>
        public static IReadOnlyList<T> Stride<T>(this IReadOnlyList<T> self, int n)
        {
            return Select(self.Count / n, i => self[i * n % self.Count]);
        }

        /// <summary>
        /// Returns a new array containing just the first n items.  
        /// </summary>
        public static IReadOnlyList<T> Take<T>(this IReadOnlyList<T> self, int n)
        {
            return self.Slice(0, n);
        }

        /// <summary>
        /// Returns a new array containing the elements after the first n elements.  
        /// </summary>
        public static IReadOnlyList<T> Skip<T>(this IReadOnlyList<T> self, int n = 1)
        {
            return self.Slice(n, self.Count);
        }

        /// <summary>
        /// Returns a new array containing the last n elements.  
        /// </summary>
        public static IReadOnlyList<T> TakeLast<T>(this IReadOnlyList<T> self, int n = 1)
        {
            return self.Skip(self.Count - n);
        }

        /// <summary>
        /// Returns a new array containing all elements excluding the last n elements.  
        /// </summary>
        public static IReadOnlyList<T> DropLast<T>(this IReadOnlyList<T> self, int n = 1)
        {
            return self.Take(self.Count - n);
        }
        
        /// <summary>
        /// Returns a new array by remapping indices 
        /// </summary>
        public static IReadOnlyList<T> MapIndices<T>(this IReadOnlyList<T> self, Func<int, int> f)
        {
            return self.Count.Select(i => self[f(i)]);
        }

        /// <summary>
        /// Returns a new array that reverses the order of elements 
        /// </summary>
        public static IReadOnlyList<T> Reverse<T>(this IReadOnlyList<T> self)
        {
            return self.MapIndices(i => self.Count - 1 - i);
        }

        /// <summary>
        /// Uses the new array to access elements of the first array.
        /// </summary>
        public static IReadOnlyList<T> SelectByIndex<T>(this IReadOnlyList<T> self, IReadOnlyList<int> indices)
        {
            return indices.Select(i => self[i]);
        }

        /// <summary>
        /// Similiar to take, if count is less than the number of items in the array, otherwise 
        /// uses a modulo operation. 
        /// </summary>
        public static IReadOnlyList<T> Resize<T>(this IReadOnlyList<T> self, int count)
        {
            return Select(count, i => self[i % self.Count]);
        }

        /// <summary>
        /// Returns an array of the same type with no elements. 
        /// </summary>
        public static IReadOnlyList<T> Empty<T>(this IReadOnlyList<T> self)
        {
            return self.Take(0);
        }

        /// <summary>
        /// Returns a sequence of integers from 0 to 1 less than the number of items in the array, representing indicies of the array.
        /// </summary>
        public static IReadOnlyList<int> Indices<T>(this IReadOnlyList<T> self)
        {
            return self.Count.Range();
        }

        /// <summary>
        /// Converts an array of elements into a string representation
        /// </summary>
        public static string Join<T>(this IReadOnlyList<T> self, string sep = " ")
        {
            return self.Aggregate(new StringBuilder(), (sb, x) => sb.Append(x).Append(sep)).ToString();
        }

        /// <summary>
        /// Concatenates the contents of one array with another.
        /// </summary>
        public static IReadOnlyList<T> Concatenate<T>(this IReadOnlyList<T> self, IReadOnlyList<T> other)
        {
            return Select(self.Count + other.Count, i => i < self.Count ? self[i] : other[i - self.Count]);
        }
    
        /// <summary>
        /// Returns the index of the first element matching the given item.
        /// </summary>
        public static int IndexOf<T>(this IReadOnlyList<T> self, T item) where T : IEquatable<T>
        {
            for (var i = 0; i < self.Count; ++i)
                if (self[i].Equals(item))
                    return i;
            return -1;
        }

        /// <summary>
        /// Returns the index of the last element matching the given item.
        /// </summary>
        public static int LastIndexOf<T>(this IReadOnlyList<T> self, T item) where T : IEquatable<T>
        {
            int n = self.Reverse().IndexOf(item);
            return n < 0 ? n : self.Count - 1 - n;
        }

        /// <summary>
        /// Returns an array that is one element shorter that subtracts each element from its previous one. 
        /// </summary>
        public static IReadOnlyList<int> AdjacentDifferences(this IReadOnlyList<int> self)
        {
            return self.ZipEachWithNext((a, b) => b - a);
        }

        /// <summary>
        /// Creates a new array that concatenates a unit item list of one item after it.  
        /// Repeatedly calling Append would result in significant performance degradation.
        /// </summary>
        public static IReadOnlyList<T> Append<T>(this IReadOnlyList<T> self, T x)
        {
            return (self.Count + 1).Select(i => i < self.Count ? self[i] : x);
        }

        /// <summary>
        /// Creates a new array that concatenates a unit item list of one item before it   
        /// Repeatedly calling Prepend would result in significant performance degradation.
        /// </summary>
        public static IReadOnlyList<T> Prepend<T>(this IReadOnlyList<T> self, T x)
        {
            return (self.Count + 1).Select(i => i == 0 ? x : self[i - 1]);
        }

        /// <summary>
        /// Returns the element at the nth position, where n is modulo the number of items in the arrays.
        /// </summary>
        public static T ElementAt<T>(this IReadOnlyList<T> self, int n)
        {
            return self[n];
        }

        /// <summary>
        /// Returns the element at the nth position, where n is modulo the number of items in the arrays.
        /// </summary>
        public static T ElementAtModulo<T>(this IReadOnlyList<T> self, int n)
        {
            return self.ElementAt(n % self.Count);
        }

        /// <summary>
        /// Counts all elements in an array that satisfy a predicate
        /// </summary>
        public static int CountWhere<T>(this IReadOnlyList<T> self, Func<T, bool> p)
        {
            return self.Aggregate(0, (n, x) => n += p(x) ? 1 : 0);
        }

        /// <summary>
        /// Counts all elements in an array that are equal to true
        /// </summary>
        public static int CountWhere(this IReadOnlyList<bool> self)
        {
            return self.CountWhere(x => x);
        }

        /// <summary>
        /// Counts all elements in an array that are equal to a value
        /// </summary>
        public static int CountWhere<T>(this IReadOnlyList<T> self, T val) where T : IEquatable<T>
        {
            return self.CountWhere(x => x.Equals(val));
        }

        /// <summary>
        /// Returns the minimum element in the list 
        /// </summary>
        public static T Min<T>(this IReadOnlyList<T> self) where T : IComparable<T>
        {
            if (self.Count == 0) throw new ArgumentOutOfRangeException();
            return self.Aggregate(self[0], (a, b) => a.CompareTo(b) < 0 ? a : b);
        }

        /// <summary>
        /// Returns the maximum element in the list 
        /// </summary>
        public static T Max<T>(this IReadOnlyList<T> self) where T : IComparable<T>
        {
            if (self.Count == 0) throw new ArgumentOutOfRangeException();
            return self.Aggregate(self[0], (a, b) => a.CompareTo(b) > 0 ? a : b);
        }
        
        /// <summary>
        /// Applies a function (like "+") to each element in the series to create an effect similar to partial sums. 
        /// </summary>
        public static T[] Accumulate<T>(this IReadOnlyList<T> self, Func<T, T, T> f)
        {
            var n = self.Count;
            var r = new T[n];
            if (n == 0) return r;
            var prev = r[0] = self[0];
            for (var i = 1; i < n; ++i)
            {
                prev = r[i] = f(prev, self[i]);
            }
            return r;
        }

        /// <summary>
        /// Applies a function (like "+") to each element in the series to create an effect similar to partial sums.
        /// The first value in the array will be zero.
        /// </summary>
        public static T[] PostAccumulate<T>(this IReadOnlyList<T> self, Func<T, T, T> f, T init = default(T))
        {
            var n = self.Count;
            var r = new T[n+1];
            if (n == 0) return r;
            var prev = r[0] = init;
            for (var i = 0; i < n; ++i)
            {
                prev = r[i+1] = f(prev, self[i]);
            }
            return r;
        }

        /// <summary>
        /// Returns true if the two lists are the same length, and the elements are the same. 
        /// </summary>
        public static bool IsEqual<T>(this IReadOnlyList<T> self, IReadOnlyList<T> other) where T: IEquatable<T>
        {
            return self == other || (self.Count == other.Count && self.Zip(other, (x, y) => x.Equals(y)).All(x => x));
        }

        /// <summary>
        /// Creates a readonly array from a seed value, by applying a function 
        /// </summary>
        public static IReadOnlyList<T> Build<T>(T init, Func<T, T> next, Func<T, bool> hasNext)
        {
            var r = new List<T>();
            while (hasNext(init))
            {
                r.Add(init);
                init = next(init);
            }
            return r;
        }

        /// <summary>
        /// Creates a readonly array from a seed value, by applying a function 
        /// </summary>
        public static IReadOnlyList<T> Build<T>(T init, Func<T, int, T> next, Func<T, int, bool> hasNext)
        {
            var i = 0;
            var r = new List<T>();
            while (hasNext(init, i))
            {
                r.Add(init);
                init = next(init, ++i);
            }
            return r;
        }

        /// <summary>
        /// Creates an array of arrays, split at the given indices
        /// </summary>
        public static IReadOnlyList<IReadOnlyList<T>> Split<T>(this IReadOnlyList<T> self, IReadOnlyList<int> indices)
        {
            return indices.Prepend(0).Zip(indices.Append(self.Count), (x,y) => self.Slice(x, y));
        }

        /// <summary>
        /// Creates an array of arrays, split at the given index.
        /// </summary>
        public static IReadOnlyList<IReadOnlyList<T>> Split<T>(this IReadOnlyList<T> self, int index)
        {
            return new[] { self.Take(index), self.Skip(index) };
        }
    }
}
