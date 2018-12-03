using System.Collections.Generic;
using NUnit;
using Ara3D;

namespace LinqArrayTests
{
    [TestClass]
    public class UnitTest1
    {
        public static int[] ArrayToTen = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        public static IReadOnlyList<int> RangeToTen = 10.Range();
        public static IReadOnlyList<int> BuildToTen = LinqArray.Build(0, x => x + 1, x => x < 10);
        
        public void CheckTens(IReadOnlyList<int> tens)
        {
            Assert.True(tens.Equals(ArrayToTen));
            Assert.Equal(0, tens.First());
            Assert.Equal(9, tens.Last());
            Assert.Equal(45, tens.Aggregate((a, b) => a + b));
            Assert.Equal(10, tens.Count);
            Assert.Equal(5, tens[5]);
            Assert.Equal(5, tens.ElementAt(5));
            var sum = 0;
            foreach (var x in tens)
            {
                sum += x;
            }
            foreach (var x in tens)
            {
                console.WriteLine(x.ToString());
            }
            Assert.Equal(45, sum);
            Assert.Equal(0, tens.First());
            Assert.True(tens.All(x => x < 10));
            Assert.True(tens.Any(x => x < 5));
            Assert.Equal(5, tens.Count(x => x % 2 == 0));
            Assert.Equal(5, tens.CountWhere(x => x % 2 == 0));
            Assert.Equal(0, tens.Reverse().Last());
            Assert.Equal(0, tens.Reverse().Reverse().First());
            var split = tens.Split(3, 6);
            Assert.Equal(3, split.Count);
            Assert.Equal(4, split[1].Count);
            var flattened = split.Flatten();
            Assert.True(flattened.Equals(tens));
        }

        [TestMethod]
        public void TestMethod1()
        {
            CheckTens(ArrayToTen);
            CheckTens(RangeToTen);
            CheckTens(BuildToTen);
        }
    }
}
