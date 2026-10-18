using MathNet.Numerics.LinearAlgebra;
using Xunit;

namespace Implicit.Tests
{
    public class AlternatingLeastSquaresFeaturesTest
    {
        [Theory]
        [InlineData(false, "empty")]
        [InlineData(true, "empty")]
        [InlineData(false, "unknown")]
        [InlineData(true, "unknown")]
        [InlineData(false, "zero")]
        [InlineData(true, "zero")]
        public void ComputeReturnsNullWithoutKnownNonzeroInteractions(bool computeUser, string history)
        {
            var model = CreateModel();
            var interactions = history switch
            {
                "empty" => Array.Empty<KeyValuePair<string, float>>(),
                "unknown" => new[] { new KeyValuePair<string, float>("unknown", 3f) },
                "zero" => new[] { new KeyValuePair<string, float>(GetId(computeUser, "a"), 0f) },
                _ => throw new ArgumentOutOfRangeException(nameof(history)),
            };

            var features = ComputeFeatures(model, computeUser, interactions);

            Assert.Null(features);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ComputeAggregatesMixedSignDuplicates(bool computeUser)
        {
            var model = CreateModel();
            var duplicates = new[]
            {
                new KeyValuePair<string, float>(GetId(computeUser, "a"), 2f),
                new KeyValuePair<string, float>(GetId(computeUser, "b"), -1f),
                new KeyValuePair<string, float>(GetId(computeUser, "a"), -0.5f),
                new KeyValuePair<string, float>(GetId(computeUser, "b"), -1f),
            };

            var actual = ComputeFeatures(model, computeUser, duplicates);
            var aggregated = ComputeFeatures(model, computeUser, CreateHistoryA(computeUser));

            AssertHistoryA(actual, computeUser);
            AssertHistoryA(aggregated, computeUser);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ComputeIgnoresUnknownAndZeroValues(bool computeUser)
        {
            var model = CreateModel();
            var ignored = new[]
            {
                new KeyValuePair<string, float>("unknown", 100f),
                new KeyValuePair<string, float>(GetId(computeUser, "c"), 0f),
            };

            var features = ComputeFeatures(model, computeUser, CreateHistoryA(computeUser).Concat(ignored));

            AssertHistoryA(features, computeUser);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ComputeDoesNotRetainPreviousHistory(bool computeUser)
        {
            var model = CreateModel();
            var historyA = CreateHistoryA(computeUser);
            var historyB = new[] { new KeyValuePair<string, float>(GetId(computeUser, "c"), 2f) };

            var first = ComputeFeatures(model, computeUser, historyA);
            var second = ComputeFeatures(model, computeUser, historyB);
            var repeated = ComputeFeatures(model, computeUser, historyA);

            // Assert retained results after all calls, also detecting aliasing of returned factor buffers.
            AssertHistoryA(first, computeUser);
            AssertHistoryA(repeated, computeUser);

            // For history B: user A=[[4,1],[1,22]], rhs=[0,8]; item A=[[6,1],[1,6]], rhs=[0,4].
            if (computeUser)
            {
                AssertFeatures(second, -8d / 87d, 32d / 87d);
            }
            else
            {
                AssertFeatures(second, -4d / 35d, 24d / 35d);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ComputeCancelledHistoryReturnsZeroFactors(bool computeUser)
        {
            var model = CreateModel();
            var interactions = new[]
            {
                new KeyValuePair<string, float>(GetId(computeUser, "a"), 1f),
                new KeyValuePair<string, float>(GetId(computeUser, "a"), -1f),
            };

            var features = ComputeFeatures(model, computeUser, interactions);

            // Accepted nonzero records cancel after aggregation; the API still returns a solved vector.
            AssertFeatures(features, 0d, 0d);
        }

        private static AlternatingLeastSquaresRecommender CreateModel()
        {
            var userFactors = new float[][]
            {
                new float[] { 2f, 0f },
                new float[] { 1f, 1f },
                new float[] { 0f, 1f },
            };
            var itemFactors = new float[][]
            {
                new float[] { 1f, 0f },
                new float[] { 1f, 2f },
                new float[] { 0f, 2f },
                new float[] { 1f, -1f },
            };

            return new AlternatingLeastSquaresRecommender(
                factors: 2,
                regularization: 1f,
                alpha: 2f,
                users: new Dictionary<string, int> { ["user-b"] = 1, ["user-c"] = 2, ["user-a"] = 0 },
                items: new Dictionary<string, int> { ["item-c"] = 2, ["item-a"] = 0, ["item-d"] = 3, ["item-b"] = 1 },
                userFactors: Matrix<float>.Build.DenseOfRowArrays(userFactors),
                itemFactors: Matrix<float>.Build.DenseOfRowArrays(itemFactors));
        }

        private static string GetId(bool computeUser, string suffix)
        {
            return (computeUser ? "item-" : "user-") + suffix;
        }

        private static KeyValuePair<string, float>[] CreateHistoryA(bool computeUser)
        {
            return new[]
            {
                new KeyValuePair<string, float>(GetId(computeUser, "a"), 1.5f),
                new KeyValuePair<string, float>(GetId(computeUser, "b"), -2f),
            };
        }

        private static RecommenderFeatures? ComputeFeatures(
            AlternatingLeastSquaresRecommender model,
            bool computeUser,
            IEnumerable<KeyValuePair<string, float>> interactions)
        {
            return computeUser ? model.ComputeUserFeatures(interactions) : model.ComputeItemFeatures(interactions);
        }

        private static void AssertHistoryA(RecommenderFeatures? features, bool computeUser)
        {
            // Minimize the weighted squared error with lambda=1, alpha=2, and missing confidence=1.
            // For history A: user A=[[9,7],[7,22]], rhs=[3,0]; item A=[[17,4],[4,6]], rhs=[6,0].
            if (computeUser)
            {
                AssertFeatures(features, 66d / 149d, -21d / 149d);
            }
            else
            {
                AssertFeatures(features, 18d / 43d, -12d / 43d);
            }
        }

        private static void AssertFeatures(RecommenderFeatures? features, double first, double second)
        {
            Assert.NotNull(features);
            Assert.Equal(2, features.Factors.Count);
            Assert.All(features.Factors, value =>
            {
                Assert.False(float.IsNaN(value));
                Assert.False(float.IsInfinity(value));
            });
            Assert.InRange(Math.Abs(features.Factors[0] - first), 0d, 1e-6d);
            Assert.InRange(Math.Abs(features.Factors[1] - second), 0d, 1e-6d);
        }
    }
}
