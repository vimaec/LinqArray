import 'package:flutter_test/flutter_test.dart';

import 'package:vim_linq_array/vim_linq_array.dart';

void main() {
  group('LinqArrayTests', () {
    const List<int> arrayToTen = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
    final rangeToTen = 10.range();
    final buildToTen = LinqArray.build<int>(0, (x) => x + 1, (x) => x < 10);
    final tensData = <IArray<int>>[
      arrayToTen.toIArray(),
      rangeToTen,
      buildToTen
    ];

    test('CheckTens', () {
      for (var tens in tensData) {
        expect(tens.sequenceEquals(arrayToTen.toIArray()), true);
        expect(tens.first(-1), 0);
        expect(tens.last(-1), 9);
        expect(tens.aggregate(0, (a, b) => a + b), 45);
        expect(tens.count, 10);
        expect(tens[5], 5);
        expect(tens.elementAt(5), 5);

        var ones = 1.repeat(9);
        var diffs = tens.zipEachWithNext((x, y) => y - x);
        expect(ones.sequenceEquals(diffs), true);
        expect(ones.sequenceEquals(tens), false);

        var indices = tens.indices();
        expect(tens.sequenceEquals(indices), true);
        expect(tens.sequenceEquals(tens.selectByIndices(indices)), true);
        expect(
            tens
                .reverse()
                .sequenceEquals(tens.selectByIndices(indices.reverse())),
            true);

        var sum = 0;
        for (var x in tens.toIterable()) {
          sum += x;
        }
        for (var x in tens.toIterable()) {
          print(x.toString());
        }
        expect(sum, 45);
        expect(tens.first(-1), 0);
        expect(tens.all((x) => x < 10), true);
        expect(tens.any((x) => x < 5), true);
        expect(tens.countWhere((x) => x % 2 == 0), 5);
        expect(tens.reverse().last(-1), 0);
        expect(tens.reverse().reverse().first(-1), 0);
        var split = tens.splitByIndices(LinqArray.create([3, 6]));
        expect(3, split.count);

        var batch = tens.subArrays(3);
        expect(4, batch.count);
        expect(batch[0].sequenceEquals(LinqArray.create([0, 1, 2])), true);
        expect(batch[3].sequenceEquals(LinqArray.create([9])), true);

        var batch2 = tens.take(9).subArrays(3);
        expect(3, batch2.count);

        var counts = split.select((x) => x.count);
        expect(counts.sequenceEquals(LinqArray.create([3, 3, 4])), true);
        var indices2 = counts.accumulate((x, y) => x + y);
        expect(indices2.sequenceEquals(LinqArray.create([3, 6, 10])), true);
        var indices3 = counts.postAccumulate((x, y) => x + y, 0);
        expect(indices3.sequenceEquals(LinqArray.create([0, 3, 6, 10])), true);
        var flattened = split.flatten();
        expect(flattened.sequenceEquals(tens), true);
      }
    });
  });
}
