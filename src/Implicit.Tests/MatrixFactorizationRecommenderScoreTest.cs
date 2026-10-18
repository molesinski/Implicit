using MathNet.Numerics.LinearAlgebra;
using Xunit;

namespace Implicit.Tests
{
    public class MatrixFactorizationRecommenderScoreTest
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RecommendReturnsDotProducts(bool useFeatures)
        {
            var model = CreateModel();
            var expected = new Dictionary<string, float>
            {
                ["item-query"] = 24f,
                ["item-negative"] = -8f,
                ["item-vertical"] = 12f,
                ["item-horizontal"] = 6f,
                ["item-zero"] = 0f,
                ["item-tie"] = 24f,
            };

            using var result = useFeatures
                ? model.Recommend(model.GetUserFeatures("user-query")!)
                : model.Recommend("user-query");

            AssertScores(expected, result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SimilarUsersNormalizesOnlyCandidates(bool useFeatures)
        {
            var model = CreateModel();
            var expected = new Dictionary<string, float>
            {
                ["user-query"] = 5f,
                ["user-negative"] = -3f,
                ["user-vertical"] = 4f,
                ["user-horizontal"] = 3f,
                ["user-zero"] = 0f,
                ["user-tie"] = 3f,
            };

            using var result = useFeatures
                ? model.SimilarUsers(model.GetUserFeatures("user-query")!)
                : model.SimilarUsers("user-query");

            AssertScores(expected, result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SimilarItemsNormalizesOnlyCandidates(bool useFeatures)
        {
            var model = CreateModel();
            var expected = new Dictionary<string, float>
            {
                ["item-query"] = 5f,
                ["item-negative"] = -3f,
                ["item-vertical"] = 3f,
                ["item-horizontal"] = 4f,
                ["item-zero"] = 0f,
                ["item-tie"] = 3f,
            };

            using var result = useFeatures
                ? model.SimilarItems(model.GetItemFeatures("item-query")!)
                : model.SimilarItems("item-query");

            AssertScores(expected, result);
        }

        [Fact]
        public void SimilarFeaturesScoresCustomCandidates()
        {
            var model = CreateModel();
            var query = model.GetUserFeatures("user-query")!;
            var candidates = new Dictionary<string, RecommenderFeatures>
            {
                ["negative"] = model.GetItemFeatures("item-negative")!,
                ["zero"] = model.GetUserFeatures("user-zero")!,
                ["query"] = query,
                ["horizontal"] = model.GetItemFeatures("item-horizontal")!,
                ["vertical"] = model.GetUserFeatures("user-vertical")!,
                ["tie"] = model.GetUserFeatures("user-tie")!,
            };
            var expected = new Dictionary<string, float>
            {
                ["negative"] = -4f,
                ["zero"] = 0f,
                ["query"] = 5f,
                ["horizontal"] = 3f,
                ["vertical"] = 4f,
                ["tie"] = 3f,
            };

            using var result = query.SimilarFeatures(candidates);

            AssertScores(expected, result);
        }

        [Theory]
        [InlineData("recommend")]
        [InlineData("users")]
        [InlineData("items")]
        [InlineData("features")]
        public void ZeroQueriesHaveZeroScores(string operation)
        {
            var model = CreateModel();
            var candidates = model.Items.Keys.ToDictionary(id => id, id => model.GetItemFeatures(id)!);
            var ids = operation == "users" ? model.Users.Keys : model.Items.Keys;
            var expected = ids.ToDictionary(id => id, _ => 0f);

            using var result = operation switch
            {
                "recommend" => model.Recommend("user-zero"),
                "users" => model.SimilarUsers("user-zero"),
                "items" => model.SimilarItems("item-zero"),
                "features" => model.GetUserFeatures("user-zero")!.SimilarFeatures(candidates),
                _ => throw new ArgumentOutOfRangeException(nameof(operation)),
            };

            AssertScores(expected, result);
        }

        private static AlternatingLeastSquaresRecommender CreateModel()
        {
            var userFactors = new float[][]
            {
                new float[] { 3f, 4f },
                new float[] { -6f, 0f },
                new float[] { 0f, 2f },
                new float[] { 2f, 0f },
                new float[] { 0f, 0f },
                new float[] { 4f, 0f },
            };
            var itemFactors = new float[][]
            {
                new float[] { 4f, 3f },
                new float[] { 0f, -2f },
                new float[] { 0f, 3f },
                new float[] { 2f, 0f },
                new float[] { 0f, 0f },
                new float[] { 0f, 6f },
            };

            // Candidate enumeration is deliberately different from row order and score order.
            return new AlternatingLeastSquaresRecommender(
                factors: 2,
                regularization: 1f,
                alpha: 2f,
                users: new Dictionary<string, int>
                {
                    ["user-negative"] = 1,
                    ["user-tie"] = 5,
                    ["user-query"] = 0,
                    ["user-zero"] = 4,
                    ["user-horizontal"] = 3,
                    ["user-vertical"] = 2,
                },
                items: new Dictionary<string, int>
                {
                    ["item-zero"] = 4,
                    ["item-negative"] = 1,
                    ["item-vertical"] = 2,
                    ["item-query"] = 0,
                    ["item-horizontal"] = 3,
                    ["item-tie"] = 5,
                },
                userFactors: Matrix<float>.Build.DenseOfRowArrays(userFactors),
                itemFactors: Matrix<float>.Build.DenseOfRowArrays(itemFactors));
        }

        private static void AssertScores(Dictionary<string, float> expected, RecommenderResult actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            Assert.Equal(
                expected.Keys.OrderBy(id => id, StringComparer.Ordinal),
                actual.Select(item => item.Key).OrderBy(id => id, StringComparer.Ordinal));

            for (var i = 0; i < actual.Count; i++)
            {
                var item = actual[i];
                Assert.False(float.IsNaN(item.Value));
                Assert.False(float.IsInfinity(item.Value));
                Assert.InRange(MathF.Abs(item.Value - expected[item.Key]), 0f, 1e-6f);

                if (i > 0)
                {
                    // Equal scores may appear in either order.
                    Assert.True(actual[i - 1].Value >= item.Value);
                }
            }
        }
    }
}
