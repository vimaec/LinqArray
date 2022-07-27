// MIT License - Copyright 2019 (C) VIMaec, LLC.
// MIT License - Copyright 2018 (C) Ara 3D, Inc.
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

part of '../vim_linq_array.dart';

/// Lookup table: mapping from a key to some value.
abstract class ILookup<TKey, TValue> {
  IArray<TKey> get keys;
  IArray<TValue> get values;
  bool contains(TKey key);
  TValue? operator [](TKey key);
}

class EmptyLookup<TKey, TValue> implements ILookup<TKey, TValue> {
  @override
  IArray<TKey> get keys => LinqArray.empty<TKey>();
  @override
  IArray<TValue> get values => LinqArray.empty<TValue>();

  @override
  bool contains(TKey key) => false;

  @override
  TValue? operator [](TKey key) => null;
}

class LookupFromDictionary<TKey, TValue> implements ILookup<TKey, TValue> {
  final TValue? _default;
  final IArray<TKey> _keys;
  final IArray<TValue> _values;
  final Map<TKey, TValue> dictionary;

  LookupFromDictionary(this.dictionary, [TValue? defaultValue])
      : _default = defaultValue,
        _keys = dictionary.keys.toIArray(),
        _values = dictionary.values.toIArray();

  @override
  IArray<TKey> get keys => _keys;
  @override
  IArray<TValue> get values => _values;

  @override
  bool contains(TKey key) => dictionary.containsKey(key);

  @override
  TValue? operator [](TKey key) => contains(key) ? dictionary[key] : _default;
}

class LookupFromArray<TValue> implements ILookup<int, TValue> {
  final IArray<TValue> _array;
  final IArray<int> _keys;
  final IArray<TValue> _values;

  LookupFromArray(IArray<TValue> xs)
      : _array = xs,
        _keys = xs.indices(),
        _values = xs;

  @override
  IArray<int> get keys => _keys;
  @override
  IArray<TValue> get values => _values;

  @override
  bool contains(int key) => key >= 0 && key <= _array.count;

  @override
  TValue operator [](int key) => _array[key];
}

extension DictionaryExtensions<TKey, TValue> on Map<TKey, TValue> {
  ILookup<TKey, TValue> toLookup([TValue? defaultValue]) =>
      LookupFromDictionary(this, defaultValue);
}

extension LookupExtensions<TKey, TValue> on ILookup<TKey, TValue> {
  TValue? getOrDefault(TKey key, [TValue? defaultValue]) =>
      contains(key) ? this[key] : defaultValue;

  Iterable<TValue> getValues() => keys.toIterable().map((k) => this[k]!);
}
