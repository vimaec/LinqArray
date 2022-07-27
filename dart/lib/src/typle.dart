part of '../vim_linq_array.dart';

class ZipValue<T> {
  final T value;
  final int index;

  const ZipValue(this.value, this.index);
}

class Tuple2<T1, T2> {
  final T1 item1;
  final T2 item2;

  const Tuple2(this.item1, this.item2);
}

class Tuple3<T1, T2, T3> {
  final T1 item1;
  final T2 item2;
  final T3 item3;

  const Tuple3(this.item1, this.item2, this.item3);
}

class Tuple4<T1, T2, T3, T4> {
  final T1 item1;
  final T2 item2;
  final T3 item3;
  final T4 item4;

  const Tuple4(this.item1, this.item2, this.item3, this.item4);
}
