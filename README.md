# LinqArray

**LinqArray** is a pure functional .NET Standard 2.0 library from **[Ara 3D](https://ara3d.com)** that provides LINQ functionality for immutable arrays, rather than streams, while preserving `O(1)` complexity when retrieving the count or items by index. It is performant, memory efficient, cross-platform, safe, and easy to use.

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

LinqArray provides many of the same extension methods for `IArray` as LINQ does for objects implementing the `IEnumerable` interface. Some examples include: 

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

Unlike [LinqFaster](https://github.com/jackmott/LinqFaster) by Jack Mott evaluations of functions happen lazily, and have no side effects. This means that this library can be easily used in a multi-threaded context without inccurring the overhead and complexity of  synchronization. 