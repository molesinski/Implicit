using Implicit.Weights;
using Xunit;

namespace Implicit.Tests
{
    public class WeightTest
    {
        private static readonly string[] ExpectedItemKeys = new[] { "a", "b", "unknown" };

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Normalize(bool useBM25)
        {
            var weight = CreateWeight(useBM25);
            var items = new Dictionary<string, float>
            {
                ["a"] = 2f,
                ["b"] = 3f,
                ["unknown"] = 3f,
            };

            weight.Normalize(items);

            Assert.Equal(ExpectedItemKeys, items.Keys.OrderBy(x => x, StringComparer.Ordinal));
            Assert.InRange(Math.Abs(items["a"] - (useBM25 ? 0.61299074f : 2.1972246f)), 0f, 1e-6f);
            Assert.InRange(Math.Abs(items["b"] - (useBM25 ? -0.76623845f : 1.2163953f)), 0f, 1e-6f);
            Assert.Equal(3f, items["unknown"]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NormalizeUsers(bool useBM25)
        {
            var weight = CreateWeight(useBM25);
            var first = new Dictionary<string, float> { ["a"] = 2f, ["b"] = 3f, ["unknown"] = 3f };
            var second = new Dictionary<string, float> { ["a"] = 4f, ["b"] = 1f, ["unknown"] = 3f };
            var userItems = new Dictionary<string, Dictionary<string, float>>
            {
                ["first"] = first,
                ["second"] = second,
            };

            weight.Normalize(userItems);

            Assert.Same(first, userItems["first"]);
            Assert.Same(second, userItems["second"]);
            Assert.InRange(Math.Abs(first["a"] - (useBM25 ? 0.61299074f : 2.1972246f)), 0f, 1e-6f);
            Assert.InRange(Math.Abs(first["b"] - (useBM25 ? -0.76623845f : 1.2163953f)), 0f, 1e-6f);
            Assert.InRange(Math.Abs(second["a"] - (useBM25 ? 0.87570107f : 4.394449f)), 0f, 1e-6f);
            Assert.InRange(Math.Abs(second["b"] - (useBM25 ? -0.38311923f : 0.4054651f)), 0f, 1e-6f);
            Assert.Equal(3f, first["unknown"]);
            Assert.Equal(3f, second["unknown"]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NormalizeEmptyItems(bool useBM25)
        {
            var weight = CreateWeight(useBM25);
            var items = new Dictionary<string, float>();

            weight.Normalize(items);

            Assert.Empty(items);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NormalizeUnknownItems(bool useBM25)
        {
            var weight = CreateWeight(useBM25);
            var items = new Dictionary<string, float> { ["unknown"] = 3f };

            weight.Normalize(items);

            var item = Assert.Single(items);
            Assert.Equal("unknown", item.Key);
            Assert.Equal(3f, item.Value);
        }

        [Theory]
        [InlineData(100f, 1f, false)]
        [InlineData(100f, 1f, true)]
        [InlineData(0f, 0.8f, false)]
        [InlineData(0f, 0.8f, true)]
        [InlineData(0f, 1f, false)]
        [InlineData(0f, 1f, true)]
        public void NormalizeBM25ZeroWeights(float k1, float b, bool normalizeUsers)
        {
            var weight = CreateWeight(useBM25: true, k1: k1, b: b);
            var zeroItems = new Dictionary<string, float> { ["a"] = 0f, ["b"] = 0f, ["unknown"] = 0f };
            var mixedItems = new Dictionary<string, float> { ["a"] = 0f, ["b"] = 3f, ["unknown"] = 0f };

            if (normalizeUsers)
            {
                var userItems = new Dictionary<string, Dictionary<string, float>>
                {
                    ["zero"] = zeroItems,
                    ["mixed"] = mixedItems,
                };

                weight.Normalize(userItems);
            }
            else
            {
                weight.Normalize(zeroItems);
                weight.Normalize(mixedItems);
            }

            Assert.Equal(ExpectedItemKeys, zeroItems.Keys.OrderBy(x => x, StringComparer.Ordinal));
            Assert.All(zeroItems.Values, value => Assert.Equal(0f, value));
            Assert.Equal(ExpectedItemKeys, mixedItems.Keys.OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal(0f, mixedItems["a"]);
            Assert.Equal(0f, mixedItems["unknown"]);

            // b has IDF ln(0.6); with k1=100 and b=1 its term frequency is 303/78.
            var expected = MathF.Log(0.6f) * (k1 == 0f ? 1f : 303f / 78f);
            Assert.InRange(Math.Abs(mixedItems["b"] - expected), 0f, 1e-6f);
        }

        [Fact]
        public void NormalizeBM25ZeroWeightsWithZeroAverageLength()
        {
            var fittingRows = new Dictionary<int, Dictionary<string, float>>
            {
                [0] = new Dictionary<string, float> { ["a"] = 0f },
            };
            var weight = BM25Weight.Fit(fittingRows);
            var items = new Dictionary<string, float> { ["a"] = 0f };

            weight.Normalize(items);

            var item = Assert.Single(items);
            Assert.Equal("a", item.Key);
            Assert.Equal(0f, item.Value);
        }

        private static Weight CreateWeight(bool useBM25, float k1 = 2f, float b = 0.5f)
        {
            // Three fitting rows with average length 4; a occurs once and b twice.
            var userItems = new Dictionary<int, Dictionary<string, float>>
            {
                [0] = new Dictionary<string, float> { ["a"] = 2f, ["b"] = 1f },
                [1] = new Dictionary<string, float> { ["b"] = 3f, ["c"] = 3f },
                [2] = new Dictionary<string, float> { ["c"] = 3f },
            };

            return useBM25 ? BM25Weight.Fit(userItems, k1: k1, b: b) : TFIDFWeight.Fit(userItems);
        }
    }
}
