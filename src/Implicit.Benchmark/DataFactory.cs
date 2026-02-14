using System.Globalization;

namespace Implicit.Benchmark
{
    public static class DataFactory
    {
        public static IEnumerable<UserItemValue> CreateCheckerBoard(int n)
        {
            var keys = Enumerable.Range(0, n).ToDictionary(x => x, x => x.ToString(CultureInfo.InvariantCulture));

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    if ((i != j) && (i % 2 == j % 2))
                    {
                        yield return new(keys[i], keys[j], 1f);
                    }
                }
            }
        }
    }
}
