part of '../vim_linq_array.dart';

// MIT License - Copyright 2019 (C) VIMaec, LLC.
// MIT License - Copyright (C) Ara 3D, Inc.
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.
//
// LinqArray.cs
// A library for working with pure functional arrays, using LINQ style extension functions.
// Based on code that originally appeared in https://www.codeproject.com/Articles/140138/Immutable-Array-for-NET

/// Represents an immutable array with expected O(1) complexity when
/// retrieving the number of items and random element access.
abstract class IArray<T> {
  T operator [](int n);
  int get count;
}

/// Implements an IArray via a function and a count.
class FunctionalArray<T> implements IArray<T> {
  final int _count;
  T Function(int) function;

  @override
  int get count => _count;
  @override
  T operator [](int n) => function(n);

  FunctionalArray(int count, this.function) : _count = count;
}

/// Implements an IArray from a System.Array.
class ArrayAdapter<T> implements IArray<T> {
  final List<T> array;

  @override
  int get count => array.length;
  @override
  T operator [](int n) => array[n];

  const ArrayAdapter(this.array);
}

extension ListExtensions<T> on List<T> {
  /// Converts any implementation of IList (e.g. Array/List) to an IArray.
  IArray<T> toIArray() => length.select((i) => this[i]);
  IArray<U> toIArrayOf<U>(U Function(T) f) => length.select((i) => f(this[i]));

  /// Converts any implementation of IList (e.g. Array/List) to an IArray.
  IArray<T> toIArrayAdapt() => ArrayAdapter<T>(this);
}

extension IterableExtensions<T> on Iterable<T> {
  /// Converts any implementation of IEnumerable to an IArray
  IArray<T> toIArray() => length.select<T>(elementAt);

  ILookup<TKey, TValue> toLookupOf<TKey, TValue>(
    TKey Function(T) keyFunc,
    TValue Function(T) valueFunc,
  ) {
    return {for (var e in this) keyFunc(e): valueFunc(e)}.toLookup();
  }

  ILookup<TKey, T> toLookup<TKey>(TKey Function(T) keyFunc) {
    return {for (var e in this) keyFunc(e): e}.toLookup();
  }
}

extension IntExtensions on int {
  /// Creates an IArray with the given number of items,
  /// and uses the function to return items.
  IArray<T> select<T>(T Function(int) f) => FunctionalArray<T>(this, f);

  /// Creates an IArray of integers from zero up to one less than the given number.
  IArray<int> range() => select<int>((i) => i);
}

extension TExtensions<T> on T {
  /// Creates an IArray by repeating the given item a number of times.
  IArray<T> repeat(int count) => count.select<T>((i) => this);

  /// Creates an IArray starting with a seed value, and applying a function
  /// to each individual member of the array. This is eagerly evaluated.
  IArray<T> generate(int count, T Function(T) f) {
    T init = this;
    final List<T> r = []; //..length = count;
    for (var i = 0; i < count; ++i) {
      r.add(init);
      init = f(init);
    }
    return r.toIArray();
  }
}

extension BoolArrayExtensions on IArray<bool> {
  /// Converts the array into a predicate (a function that returns true or false) based on the truth of the given value.
  bool Function(int) toPredicate() => toFunction(false);

  /// Counts all elements in an array that are equal to true
  int countWhereTrue() => countWhere((x) => x);
}

extension NumArrayExtension<TNum extends num> on IArray<TNum> {
  IArray<TNum> prefixSums() => scan<num>(0, (a, b) => a + b).cast();
}

extension IntArrayExtension<TNum extends num> on IArray<int> {
  /// Uses the array as indices to select elements from the other array.
  IArray<T> choose<T>(IArray<T> values) => values.selectByIndices(this);

  /// Given indices of sub-arrays groups, this will convert it to arrays of indices (e.g. [0, 2] with a group size of 3 becomes [0, 1, 2, 6, 7, 8])
  IArray<int> groupIndicesToIndices(int groupSize) => groupSize == 1
      ? this
      : (count * groupSize)
          .select((i) => this[i ~/ groupSize] * groupSize + i % groupSize);

  /// Returns an array that is one element shorter that subtracts each element from its previous one.
  IArray<int> adjacentDifferences() => zipEachWithNext((a, b) => b - a);

  // Similar to prefix sums, but starts at zero.
  // r[i] = Sum(count[0 to i])
  IArray<int> countsToOffsets() {
    final r = List<int>.filled(count, 0);
    for (var i = 1; i < count; ++i) {
      r[i] = r[i - 1] + this[i - 1];
    }
    return r.toIArray();
  }

  IArray<int> offsetsToCounts(int last) => indices()
      .select((i) => i < count - 1 ? this[i + 1] - this[i] : last - this[i]);
}

extension ArrayComparable<T extends Comparable<T>> on IArray<T> {
  /// Returns the minimum element in the list
  T min() {
    if (count == 0) {
      throw RangeError.value(count, "Range can't be empty");
    }
    return aggregate(this[0], (a, b) => a.compareTo(b) < 0 ? a : b);
  }

  /// Returns the maximum element in the list
  T max() {
    if (count == 0) {
      throw RangeError.value(count, "Range can't be empty");
    }
    return aggregate(this[0], (a, b) => a.compareTo(b) > 0 ? a : b);
  }
}

extension Tuple2Array<T1, T2> on IArray<Tuple2<T1, T2>> {
  /// Splits an array of tuples into a tuple of array
  Tuple2<IArray<T1>, IArray<T2>> unzip() =>
      Tuple2(select((pair) => pair.item1), select((pair) => pair.item2));
}

extension ArrayOfArraysExtensions<T> on IArray<IArray<T>> {
  /// Converts an array of array into a flattened array. Each array is assumed to be of size n.
  IArray<T> flattenOfSize(int n) =>
      (count * n).select((i) => this[i ~/ n][i % n]);

  /// Converts an array of array into a flattened array.
  IArray<T> flatten() {
    IArray<int> counts =
        select((x) => x.count).postAccumulate((x, y) => x + y, 0);
    final r = List<T?>.filled(counts.last(-1), null);
    var i = 0;
    for (var xs in toIterable()) {
      xs.copyTo(r, counts[i++]);
    }
    return r.toIArrayOf((e) => e!);
  }

  /// Returns an array from an array of arrays, where the number of sub-elements is the same for reach array and is known.
  IArray<T> selectManyOfSize(int count) =>
      count.select((i) => this[i ~/ count][i % count]);
}

/// Extension functions for working on any object implementing IArray. This overrides
/// many of the Linq functions providing better performance.
extension IArrayExtensions<T> on IArray<T> {
  /// A helper function to enable IArray to support IEnumerable
  Iterable<T> toIterable() sync* {
    for (var i = 0; i < count; i++) {
      yield this[i];
    }
  }

  IArray<T> forEach(void Function(T) f) {
    for (var i = 0; i < count; ++i) {
      f(this[i]);
    }
    return this;
  }

  /// Creates an IArray by repeating each item in the source a number of times.
  IArray<T> repeatElements(int count) =>
      count.select<T>((i) => this[i ~/ count]);

  /// Returns the first item in the array.
  T first(T defaultValue) => isEmpty() ? defaultValue : this[0];

  /// Returns the last item in the array
  T last(T defaultValue) => isEmpty() ? defaultValue : this[count - 1];

  /// Returns true if and only if the argument is a valid index into the array.
  bool inRange(int n) => n >= 0 && n < count;

  /// A mnemonic for "Any()" that returns false if the count is greater than zero
  bool isEmpty() => !any();

  /// Converts the IArray into a system array.
  List<T> toListCopy() => copyTo<List<T>>(<T>[]);

  /// Converts the IArray into a system List.
  List<T> toList() => List<T>.generate(count, elementAt);

  /// Converts the array into a function that returns values from an integer, returning a default value if out of range.
  T Function(int) toFunction(T defaultValue) =>
      (i) => inRange(i) ? this[i] : defaultValue;

  /// Adds all elements of the array to the target collection.
  U addTo<U extends List<T>>(U other) {
    forEach(other.add);
    return other;
  }

  /// Copies all elements of the array to the target list or array, starting at the provided index.
  U copyTo<U extends List>(U other, [int destIndex = 0]) {
    for (var i = 0; i < count; ++i) {
      other[i + destIndex] = this[i];
    }
    return other;
  }

  /// Returns an array generated by applying a function to each element.
  IArray<U> select<U>(U Function(T) f) => count.select<U>((i) => f(this[i]));

  /// Returns an array generated by applying a function to each element.
  IArray<U> selectByIndex<U>(U Function(T, int) f) =>
      count.select<U>((i) => f(this[i], i));

  /// Returns an array generated by applying a function to each element.
  IArray<U> selectIndices<U>(U Function(int) f) => count.select<U>(f);

  /// Returns an array of tuple where each element of the initial array is paired with its index.
  IArray<ZipValue<T>> zipWithIndex() =>
      selectByIndex<ZipValue<T>>((v, i) => ZipValue<T>(v, i));

  /// Returns an array given a function that generates an IArray from each member. Eager evaluation.
  IArray<U> selectMany<U>(IArray<U> Function(T) func) {
    var xs = <U>[];
    for (var i = 0; i < count; ++i) {
      func(this[i]).addTo(xs);
    }
    return xs.toIArray();
  }

  /// Returns an array given a function that generates an IArray from each member. Eager evaluation.
  IArray<U> selectManyByIndex<U>(IArray<U> Function(T, int) func) {
    var xs = <U>[];
    for (var i = 0; i < count; ++i) {
      func(this[i], i).addTo(xs);
    }
    return xs.toIArray();
  }

  /// Returns an array given a function that generates a tuple from each member. Eager evaluation.
  IArray<U> selectManyTuple2<U>(Tuple2<U, U> Function(T) func) {
    final r = List<U?>.filled(count * 2, null);
    for (var i = 0; i < count; ++i) {
      var tmp = func(this[i]);
      r[i * 2] = tmp.item1;
      r[i * 2 + 1] = tmp.item2;
    }
    return r.toIArrayOf((e) => e!);
  }

  /// Returns an array given a function that generates a tuple from each member. Eager evaluation.
  IArray<U> selectManyTuple3<U>(Tuple3<U, U, U> Function(T) func) {
    final r = List<U?>.filled(count * 3, null);
    for (var i = 0; i < count; ++i) {
      var tmp = func(this[i]);
      r[i * 3] = tmp.item1;
      r[i * 3 + 1] = tmp.item2;
      r[i * 3 + 2] = tmp.item3;
    }
    return r.toIArrayOf((e) => e!);
  }

  /// Returns an array given a function that generates a tuple from each member. Eager evaluation.
  IArray<U> selectManyTuple4<U>(Tuple4<U, U, U, U> Function(T) func) {
    final r = List<U?>.filled(count * 4, null);
    for (var i = 0; i < count; ++i) {
      var tmp = func(this[i]);
      r[i * 4] = tmp.item1;
      r[i * 4 + 1] = tmp.item2;
      r[i * 4 + 2] = tmp.item3;
      r[i * 4 + 3] = tmp.item4;
    }
    return r.toIArrayOf((e) => e!);
  }

  /// Returns an array generated by applying a function to corresponding pairs of elements in both arrays.
  IArray<V> zip<U, V>(IArray<U> other, V Function(T, U) f) =>
      math.min(count, other.count).select<V>((i) => f(this[i], other[i]));

  /// Returns an array generated by applying a function to corresponding pairs of elements in both arrays.
  IArray<V> zipByIndex<U, V>(IArray<U> other, V Function(T, U, int) f) =>
      math.min(count, other.count).select<V>((i) => f(this[i], other[i], i));

  /// Returns an array generated by applying a function to corresponding pairs of elements in both arrays.
  IArray<W> zip2<U, V, W>(
          IArray<U> other, IArray<V> other2, W Function(T, U, V) f) =>
      math
          .min(math.min(count, other.count), other2.count)
          .select<W>((i) => f(this[i], other[i], other2[i]));

  /// Returns an array generated by applying a function to corresponding pairs of elements in both arrays.
  IArray<W> zip2ByIndex<U, V, W>(
          IArray<U> other, IArray<V> other2, W Function(T, U, V, int) f) =>
      math
          .min(math.min(count, other.count), other2.count)
          .select<W>((i) => f(this[i], other[i], other2[i], i));

  /// Returns an array generated by applying a function to corresponding pairs of elements in both arrays.
  IArray<X> zip3<U, V, W, X>(IArray<U> other, IArray<V> other2,
          IArray<W> other3, X Function(T, U, V, W) f) =>
      math
          .min(math.min(count, other.count),
              math.min(other2.count, other3.count))
          .select<X>((i) => f(this[i], other[i], other2[i], other3[i]));

  /// Returns an array generated by applying a function to corresponding pairs of elements in both arrays.
  IArray<X> zip3ByIndex<U, V, W, X>(IArray<U> other, IArray<V> other2,
          IArray<W> other3, X Function(T, U, V, W, int) f) =>
      math
          .min(math.min(count, other.count),
              math.min(other2.count, other3.count))
          .select<X>((i) => f(this[i], other[i], other2[i], other3[i], i));

  /// Applies a function to each element in the list paired with the next one.
  /// Used to implement adjacent differences for example.
  IArray<U> zipEachWithNext<U>(U Function(T, T) f) => zip(skip(), f);

  /// Returns an IEnumerable containing only elements of the array for which the function returns true on the index.
  /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
  Iterable<T> whereIndices(bool Function(int) f) =>
      whereByIndex((x, i) => f(i));

  /// Returns an IEnumerable containing only elements of the array for which the corresponding mask is true.
  /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
  Iterable<T> whereByMask(IArray<bool> mask) =>
      whereIndices(mask.toPredicate());

  /// Returns an IEnumerable containing only elements of the array for which the corresponding predicate is true.
  //=> toIterable().where(predicate);
  Iterable<T> where(bool Function(T) predicate) sync* {
    for (var i = 0; i < count; i++) {
      final value = this[i];
      if (predicate(value)) {
        yield value;
      }
    }
  }

  /// Returns an IEnumerable containing only elements of the array for which the corresponding predicate is true.
  Iterable<T> whereByIndex(bool Function(T, int) predicate) sync* {
    for (var i = 0; i < count; i++) {
      final value = this[i];
      if (predicate(value, i)) {
        yield value;
      }
    }
  }

  /// Returns an IEnumerable containing only indices of the array for which the function satisfies a specific predicate.
  /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
  Iterable<int> indicesWhere(bool Function(T) f) =>
      indices().where((i) => f(this[i]));

  /// Returns an IEnumerable containing only indices of the array for which the function satisfies a specific predicate.
  /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
  Iterable<int> indicesWhereByIndex(bool Function(T, int) f) =>
      indicesAtWhere((i) => f(this[i], i));

  /// Returns an IEnumerable containing only indices of the array for which the function satisfies a specific predicate.
  /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
  Iterable<int> indicesAtWhere(bool Function(int) f) => indices().where(f);

  /// Returns an IEnumerable containing only indices of the array for which booleans in the mask are true.
  /// An IArray is not created automatically because it is an expensive operation that is potentially unneeded.
  Iterable<int> indicesAtByMask(IArray<bool> mask) =>
      indicesAtWhere(mask.toPredicate());
  U aggregate<U>(U init, U Function(U, T) func) {
    U seed = init;
    for (var i = 0; i < count; ++i) {
      seed = func(seed, this[i]);
    }
    return seed;
  }

  U aggregateByIndex<U>(U init, U Function(U, T, int) func) {
    U seed = init;
    for (var i = 0; i < count; ++i) {
      seed = func(seed, this[i], i);
    }
    return seed;
  }

  /// Returns a new array containing the elements in the range of from to to.
  IArray<T> slice(int from, int to) =>
      (to - from).select<T>((i) => this[i + from]);

  /// Returns an array of SubArrays of size "size"
  /// the last items that cannot fill an arrat if size "size" will be ignored
  IArray<IArray<T>> subArraysFixed(int size) =>
      (count ~/ size).select((i) => subArray(i, size));

  /// Returns an array of SubArrays of size "size" plus extras
  /// The extra array is of size count % size if present
  IArray<IArray<T>> subArrays(int size) => count % size == 0
      ? subArraysFixed(size)
      : subArraysFixed(size).append(takeLast(count % size));

  /// Returns n elements of the list starting from a given index.
  IArray<T> subArray(int from, int count) => slice(from, count + from);

  /// Returns elements of the array between from and skipping every stride element.
  IArray<T> sliceByStride(int from, int to, int stride) =>
      (to - from ~/ stride).select((i) => this[i * stride + from]);

  /// Returns a new array containing the elements by taking every nth item.
  IArray<T> stride(int n) => (count ~/ n).select<T>((i) => this[i * n % count]);

  /// Returns a new array containing just the first n items.
  IArray<T> take(int n) => slice(0, n);

  /// Returns a new array containing just at most n items.
  IArray<T> takeAtMost(int n) => count > n ? slice(0, n) : this;

  /// Returns a new array containing the elements after the first n elements.
  IArray<T> skip([int n = 1]) => slice(n, count);

  /// Returns a new array containing the last n elements.
  IArray<T> takeLast([int n = 1]) => skip(count - n);

  /// Returns a new array containing all elements excluding the last n elements.
  IArray<T> dropLast([int n = 1]) =>
      count > n ? take(count - n) : LinqArray.empty<T>();

  /// Returns a new array by remapping indices
  IArray<T> mapIndices(int Function(int) f) => count.select((i) => this[f(i)]);

  /// Returns a new array that reverses the order of elements
  IArray<T> reverse() => mapIndices((i) => count - 1 - i);

  /// Uses the provided indices to select elements from the array.
  IArray<T> selectByIndices(IArray<int> indices) =>
      indices.select((i) => this[i]);

  /// Return the array separated into a series of groups (similar to DictionaryOfLists)
  /// based on keys created by the given keySelector
  Map<TKey, List<T>> groupBy<TKey>(TKey Function(T) keySelector) {
    var map = <TKey, List<T>>{};
    for (var i = 0; i < count; i++) {
      final element = this[i];
      (map[keySelector(element)] ??= []).add(element);
    }
    return map;
  }

  /// Return the array separated into a series of groups (similar to DictionaryOfLists)
  /// based on keys created by the given keySelector and elements chosen by the element selector
  Map<TKey, List<TElem>> groupElementBy<TKey, TElem>(
      TKey Function(T) keySelector, TElem Function(T) elementSelector) {
    var map = <TKey, List<TElem>>{};
    for (var i = 0; i < count; i++) {
      final value = this[i];
      final element = elementSelector(value);
      (map[keySelector(value)] ??= []).add(element);
    }
    return map;
  }

  /// Uses the provided indices to select groups of contiguous elements from the array.
  /// This is equivalent to self.SubArrays(groupSize).SelectByIndex(indices).SelectMany();
  IArray<T> selectGroupsByIndices(int groupSize, IArray<int> indices) =>
      selectByIndices(indices.groupIndicesToIndices(groupSize));

  /// Similar to take, if count is less than the number of items in the array, otherwise uses a modulo operation.
  IArray<T> resize(int count) => count.select((i) => this[i % this.count]);

  /// Returns an array of the same type with no elements.
  IArray<T> empty() => take(0);

  /// Returns a sequence of integers from 0 to 1 less than the number of items in the array, representing indicies of the array.
  IArray<int> indices() => count.range();

  /// Converts an array of elements into a string representation
  String join([String sep = " "]) => toIterable().join(sep);

  /// Concatenates the contents of one array with another.
  IArray<T> concatenate(IArray<T> other) => (count + other.count)
      .select((i) => i < count ? this[i] : other[i - count]);

  /// Returns the index of the first element matching the given item.
  int indexOf(T item) => indexOfByPredicate((x) => x == item);

  /// Returns the index of the first element matching the given item.
  int indexOfByPredicate(bool Function(T) predicate) {
    for (var i = 0; i < count; ++i) {
      if (predicate(this[i])) {
        return i;
      }
    }
    return -1;
  }

  /// Returns the index of the last element matching the given item.
  int lastIndexOf(T item) {
    var n = reverse().indexOf(item);
    return n < 0 ? n : count - 1 - n;
  }

  /// Creates a new array that concatenates a unit item list of one item after it.
  /// Repeatedly calling Append would result in significant performance degradation.
  IArray<T> append(T x) => (count + 1).select((i) => i < count ? this[i] : x);

  /// Creates a new array that concatenates the given items to itself.
  IArray<T> appendList(List<T> x) => concatenate(x.toIArray());

  /// Creates a new array that concatenates a unit item list of one item before it
  /// Repeatedly calling Prepend would result in significant performance degradation.
  IArray<T> prepend(T x) =>
      (count + 1).select<T>((i) => i == 0 ? x : this[i - 1]);

  /// Returns the element at the nth position, where n is modulo the number of items in the arrays.
  T elementAt(int n) => this[n];

  /// Returns the element at the nth position, where n is modulo the number of items in the arrays.
  T elementAtModulo(int n) => elementAt(n % count);

  /// Returns the Nth element of the array, or a default value if out of range/
  T elementAtOrDefault(int n, T defaultValue) =>
      n >= 0 && n < count ? this[n] : defaultValue;

  /// Counts all elements in an array that satisfy a predicate
  int countWhere(bool Function(T) p) =>
      aggregate(0, (n, x) => n + (p(x) ? 1 : 0));

  /// Counts all elements in an array that are equal to a value
  int countWhereElement(T val) => countWhere((x) => x == val);

  /// Applies a function (like "+") to each element in the series to create an effect similar to partial sums.
  IArray<T> accumulate(T Function(T, T) f) {
    var n = count;
    if (n == 0) {
      return LinqArray.empty<T>();
    }
    var r = List<T?>.filled(n, null);
    var prev = r[0] = this[0];
    for (var i = 1; i < n; ++i) {
      prev = r[i] = f(prev, this[i]);
    }
    return r.toIArrayOf((e) => e!);
  }

  /// Applies a function (like "+") to each element in the series to create an effect similar to partial sums.
  /// The first value in the array will be zero.
  IArray<T> postAccumulate(T Function(T, T) f, T init) {
    final n = count;
    //final List<T> r = []; //..length = n+1;
    final List<T> r = List<T>.filled(n + 1, init);
    T prev = r[0] = init;
    if (n == 0) {
      return r.toIArray();
    }
    for (var i = 0; i < n; ++i) {
      prev = r[i + 1] = f(prev, this[i]);
    }
    return r.toIArray();
  }

  /// Returns true if the two lists are the same length, and the elements are the same.
  //=> this == other || (count == other.count && zip(other, (x, y) => x == y).all((x) => x));
  bool sequenceEquals(IArray<T>? other) {
    if (identical(this, other)) {
      return true;
    }
    if (other == null) {
      return false;
    }
    if (count != other.count) {
      return false;
    }
    for (var i = 0; i < count; i++) {
      if (this[i] != other[i]) {
        return false;
      }
    }
    return true;
  }

  /// Creates an array of arrays, split at the given indices
  IArray<IArray<T>> splitByIndices(IArray<int> indices) =>
      indices.prepend(0).zip(indices.append(count), (x, y) => slice(x, y));

  /// Creates an array of arrays, split at the given index.
  IArray<IArray<T>> split(int index) => [take(index), skip(index)].toIArray();

  /// Returns true if the predicate is true for all of the elements in the array
  bool all(bool Function(T) predicate) {
    for (var i = 0; i < count; i++) {
      if (!predicate(this[i])) {
        return false;
      }
    }
    return true;
  }

  /// Returns true if there are any elements in the array.
  /// Returns true if the predicate is true for any of the elements in the array
  bool any([bool Function(T)? predicate]) =>
      predicate == null ? count != 0 : toIterable().any(predicate);

  /// Sums items in an array using a selector function that returns integers.
  TElem sum<TElem extends num>(TElem Function(T) func) =>
      aggregate<num>(0, (init, x) => init + func(x)) as TElem;

  /// Forces evaluation (aka reification) of the array by creating a copy in memory.
  /// This is useful as a performance optimization, or to force the objects to exist permanently.
  IArray<T> evaluate() =>
      (this is ArrayAdapter<T>) ? this : toList().toIArray();

  // /// Forces evaluation (aka reification) of the array in parallel.
  // IArray<T> evaluateInParallel() =>
  //     (this is ArrayAdapter<T>) ? this : toArrayInParallel().toIArray();

  // /// Converts to a regular array in paralle;
  // /// <typeparam name="T] </typeparam>
  // /// [xs]
  // /// Returns:
  // List<T> toArrayInParallel() {
  //   if (count == 0) {
  //     return <T>[];
  //   }

  //   if (xs.count < Environment.processorCount) {
  //     return xs.toArray<T>();
  //   }

  //   var r = List<T>();
  //   var partitioner =
  //       Partitioner.create(0, xs.count, xs.count / Environment.processorCount);

  //   Parallel.forEach<Tuple2<int, int>>(partitioner, (range, state) {
  //     for (var i = range.item1; i < range.item2; ++i) {
  //       r[i] = xs[i];
  //     }
  //   });
  //   return r;
  // }

  /// Maps pairs of elements to a new array.
  IArray<U> selectPairs<U>(U Function(T, T) f) =>
      (count ~/ 2).select<U>((i) => f(this[i * 2], this[i * 2 + 1]));

  /// Maps every 3 elements to a new array.
  IArray<U> selectTriplets<U>(U Function(T, T, T) f) => (count ~/ 3)
      .select<U>((i) => f(this[i * 3], this[i * 3 + 1], this[i * 3 + 2]));

  /// Maps every 4 elements to a new array.
  IArray<U> selectQuartets<U>(U Function(T, T, T, T) f) =>
      (count ~/ 4).select<U>((i) =>
          f(this[i * 4], this[i * 4 + 1], this[i * 4 + 2], this[i * 4 + 3]));

  /// Returns the number of unique instances of elements in the array.
  int countUnique() => toIterable().toSet().length;

  /// Returns elements in order.
  IArray<T> sort([int Function(T a, T b)? compare]) =>
      (toList()..sort(compare)).toIArray();

  /// Given an array of elements of type T casts them to a U
  IArray<U> cast<U>() => select((x) => x as U);

  /// Returns true if the value is present in the array.
  bool contains(T value) => any((x) => x == value);

  ILookup<TKey, TValue> toLookupOf<TKey, TValue>(
    TKey Function(T) keyFunc,
    TValue Function(T) valueFunc,
  ) =>
      {for (var e in toIterable()) keyFunc(e): valueFunc(e)}.toLookup();

  ILookup<TKey, T> toLookup<TKey>(TKey Function(T) keyFunc) =>
      {for (var e in toIterable()) keyFunc(e): e}.toLookup();

  T firstOrDefault(T defaultValue) => count > 0 ? this[0] : defaultValue;
  T firstOrDefaultWhere(bool Function(T) predicate, T defaultValue) {
    for (var i = 0; i < count; i++) {
      final value = this[i];
      if (predicate(value)) {
        return value;
      }
    }
    return defaultValue;
  }

  IArray<U> scan<U>(U init, U Function(U, T) scanFunc) {
    if (count == 0) {
      return LinqArray.empty<U>();
    }
    final r = List<U?>.filled(count, null);
    for (var i = 0; i < count; ++i) {
      init = r[i] = scanFunc(init, this[i]);
    }
    return r.toIArrayOf<U>((e) => e!);
  }

  IArray<T> setElementAt(int index, T value) =>
      selectIndices((i) => i == index ? value : this[i]);
  IArray<T> setFirstElementWhere(bool Function(T) predicate, T value) {
    final index = indexOfByPredicate(predicate);
    return index < 0 ? this : setElementAt(index, value);
  }
}


class LinqArray {
  /// Helper function for creating an IArray from the arguments.
  static IArray<T> create<T>(List<T> self) => self.toIArray();

  /// Returns an array of the same type with no elements.
  static IArray<T> empty<T>() => List<T>.empty().toIArray();

  /// Creates a readonly array from a seed value, by applying a function
  static IArray<T> build<T>(
      T init, T Function(T) next, bool Function(T) hasNext) {
    final r = <T>[];
    while (hasNext(init)) {
      r.add(init);
      init = next(init);
    }
    return r.toIArray();
  }
  
  /// Creates a readonly array from a seed value, by applying a function
  static IArray<T> buildByIndex<T>(
      T init, T Function(T, int) next, bool Function(T, int) hasNext) {
    var i = 0;
    var r = <T>[];
    while (hasNext(init, i)) {
      r.add(init);
      init = next(init, ++i);
    }
    return r.toIArray();
  }
}
