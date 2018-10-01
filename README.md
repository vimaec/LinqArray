# LinqArray

*LinqArray* is a .NET library that provides LINQ functionality for immutable arrays, rather than streams, while preserving `O(1)` complexity when retrieving the count or items by index. 

It is performant, memory efficient, safe, and easy to use. While it is a general purpose library, it is being used in the development of high performance geometric processing algorithms.

## Overview 

LinqArray is a set of extension methods build on the following `IArray<T>` interface:

```
interface IArray<T> {
    int Count { get; }
    T this[int n] { get; }
}
```

Because the interface does not mutate objects it is safe to use in a multithreaded context. Furthermore, as with LINQ for IEnumerable, evaluation happens on demand (aka lazily). 

## Extension Methods 

LinqArray provides many of the same extension methods for `IArray` as LINQ does for objects implementing the `IEnumerable` interface, including:

* `Aggregate`
* `Select`
* `Zip`
* `Take`
* `Skip` 
* `Reverse` 
* `All`
* `Any`
* `Count`

## Status 

The project is under heavy development but the core functionality is fixed. 

## Similar Work

This library is based on an article on CodeProject.com called [LINQ for Immutable Arrays](https://www.codeproject.com/Articles/517728/LINQ-for-Immutable-Arrays).

Unlike [LinqFaster](https://github.com/jackmott/LinqFaster) by Jack Mott evaluations of functions happen lazily.