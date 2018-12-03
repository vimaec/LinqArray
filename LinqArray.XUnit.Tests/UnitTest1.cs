using System.Collections.Generic;
using Xunit;
using Ara3D;
using System.Linq;
using Xunit.Abstractions;

namespace LinqArray.XUnit.Tests
{
    public class UnitTest1
    {
        readonly ITestOutputHelper output;

        public UnitTest1(ITestOutputHelper output)
        {
            this.output = output;
        }

        public static int[] ShortIntSequence = { 1, 1, 2, 3, 5, 8, 13, 21, 35 };
        public static int[] ArrayToTen = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        public static IReadOnlyList<int> RangeToTen = 10.Range();

        public static object[][] Data = {
            new [] { ArrayToTen },
            new [] { RangeToTen },
        };

        [Theory]
        [MemberData(nameof(Data))]
        public void CheckTens(IReadOnlyList<int> tens)
        {
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
                output.WriteLine(x.ToString());
            }
            Assert.Equal(45, sum);
            Assert.Equal(0, tens.First());
            Assert.True(tens.All(x => x < 10));
            Assert.True(tens.Any(x => x < 5));
            Assert.Equal(5, tens.Count(x => x % 2 == 0));
            Assert.Equal(0, tens.Reverse().Last());
            Assert.Equal(0, tens.Reverse().Reverse().First());
        }

        [Fact]
        public void Test1()
        {
            var tens = 10.Range();
            Assert.Equal(0, tens.First());
            Assert.Equal(9, tens.Last());
            Assert.Equal(45, tens.Aggregate((a, b) => a + b));
            Assert.Equal(10, tens.Count);
            Assert.Equal(5, tens[5]);
            Assert.Equal(5, tens.ElementAt(5));
            var tensByTen = tens.Select(x => x * 10);
        }
    }
}
