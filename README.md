# LinqArray

**LinqArray** is a pure functional .NET Standard 2.0 library from **[Ara 3D](https://ara3d.com)** that provides LINQ functionality for immutable arrays, rather than streams, while preserving `O(1)` complexity when retrieving the count or items by index. It is performant, memory efficient, cross-platform, safe, and easy to use.

## What the heck is .NET Standard? Don't you mean .NET Core? 

Class libraries written to target .NET Standard can be used in projects that target .NET Core or .NET Framework (or of course .NET Standard). 

## Overview 

LinqArray is a set of extension methods build on the `System.IReadonlyList<T>` interface:

```
interface IReadonlyList<T> {
    int Count { get; }
    T this[int n] { get; }
}
```

Because the interface does not mutate objects it is safe to use in a multithreaded context. Furthermore, as with LINQ for IEnumerable, evaluation happens on demand (aka lazily). 

## Why? 

Using concrete types like `List` or `Array` versus an interface leadas to code that is more verbose and harder to maintain because it forces users to commit to a specific representation of data type. Library functions then have to be written for each type. This is why interfaces like `IEnumerable` are so prevalent: using extension methods we can easily write libraries that work on any conforming type. The closest thing to an array 

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

This library is based on an article on CodeProject.com called [LINQ for Immutable Arrays](https://www.codeproject.com/Articles/517728/LINQ-for-Immutable-Arrays). While I worked at Autodesk we used a library based on this article in Autodesk 3ds Max 2016 and later, which shows that 

Unlike [LinqFaster](https://github.com/jackmott/LinqFaster) by Jack Mott evaluations of functions happen lazily, and have no side effects. This means that this library can be easily used in a multi-threaded context without inccurring the overhead and complexity of  synchronization. 