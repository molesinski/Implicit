using MathNet.Numerics.LinearAlgebra;
using Xunit;

namespace Implicit.Tests
{
    public class AlternatingLeastSquaresRecommenderTest : MatrixFactorizationRecommenderTest<AlternatingLeastSquaresRecommender>
    {
        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void Factorize(bool conjugateGradient, bool negativeConfidence)
        {
            var original = Matrix<float>.Build.DenseOfRowArrays(
                new float[][]
                {
                    new float[] { 1, 1, 0, 1, 0, 0 },
                    new float[] { 0, 1, 1, 1, 0, 0 },
                    new float[] { 1, 0, 1, 0, 0, 0 },
                    new float[] { 1, 1, 0, 0, 0, 0 },
                    new float[] { 0, 0, 1, 1, 0, 1 },
                    new float[] { 0, 1, 0, 0, 0, 1 },
                    new float[] { 0, 0, 0, 0, 1, 1 },
                });

            var training = original;

            if (negativeConfidence)
            {
                training = training.Clone();

                foreach (var (i, j, value) in training.EnumerateIndexed())
                {
                    if (value == 0)
                    {
                        training[i, j] = -1;
                    }
                }
            }

            var recommender =
                AlternatingLeastSquaresRecommender.Fit(
                    UserItemMatrix.Build(ToUserItems(training)),
                    new AlternatingLeastSquaresParameters(
                        factors: 6,
                        regularization: 0,
                        alpha: 2,
                        iterations: 15,
                        useConjugateGradient: conjugateGradient,
                        random: new Random(42)));

            var reconstructed = ToReconstructedMatrix(recommender);

            for (var i = 0; i < original.RowCount; i++)
            {
                for (var j = 0; j < reconstructed.ColumnCount; j++)
                {
                    AssertFinite(reconstructed[i, j]);
                    Assert.InRange(MathF.Abs(original[i, j] - reconstructed[i, j]), 0f, 1e-3f);
                }
            }
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(2, 15)]
        [InlineData(64, 1)]
        [InlineData(64, 15)]
        public void FactorizeHighConfidence(int factors, int iterations)
        {
            var input = UserItemMatrix.Build(new[] { new UserItemValue("user", "item", 1f) });
            var parameters = new AlternatingLeastSquaresParameters(
                factors: factors,
                regularization: 0.01f,
                alpha: 10000f,
                iterations: iterations,
                useConjugateGradient: false,
                random: new Random(42));

            var model = AlternatingLeastSquaresRecommender.Fit(input, parameters);

            Assert.All(model.UserFactors.Enumerate(), AssertFinite);
            Assert.All(model.ItemFactors.Enumerate(), AssertFinite);
            AssertSingleObservationSolution(model.ItemFactors.Row(0), model.UserFactors.Row(0), model.Alpha, model.Regularization);

            using var recommendations = model.Recommend("user");
            var recommendation = Assert.Single(recommendations);
            AssertFinite(recommendation.Value);
            Assert.InRange(MathF.Abs(1f - recommendation.Value), 0f, 1e-4f);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ComputeHighConfidenceFeatures(bool userFeatures)
        {
            var factorValues = new float[] { 41.974094f, 174.81573f };
            var factors = Matrix<float>.Build.DenseOfRowArrays(factorValues);
            var model = new AlternatingLeastSquaresRecommender(
                factors: 2,
                regularization: 0.01f,
                alpha: 10000f,
                users: new Dictionary<string, int> { ["user"] = 0 },
                items: new Dictionary<string, int> { ["item"] = 0 },
                userFactors: factors,
                itemFactors: factors);

            var features = userFeatures
                ? model.ComputeUserFeatures(new[] { new KeyValuePair<string, float>("item", 1f) })
                : model.ComputeItemFeatures(new[] { new KeyValuePair<string, float>("user", 1f) });

            Assert.NotNull(features);
            AssertSingleObservationSolution(features.Factors, factors.Row(0), model.Alpha, model.Regularization);
        }

        [Fact]
        public void SaveLoad()
        {
            var userFactors = new float[][]
            {
                new float[] { 1.25f, -2.5f },
                new float[] { 0.125f, 3.75f },
            };
            var itemFactors = new float[][]
            {
                new float[] { -4f, 0.5f },
                new float[] { 2f, -1f },
                new float[] { 0f, 1.5f },
            };

            // Dictionary iteration order differs from matrix row order; persistence must follow IDs.
            var original = new AlternatingLeastSquaresRecommender(
                factors: 2,
                regularization: 0.25f,
                alpha: 3.5f,
                users: new Dictionary<string, int> { ["żółw"] = 1, ["alice"] = 0 },
                items: new Dictionary<string, int> { ["book-c"] = 2, ["book-a"] = 0, ["book-b"] = 1 },
                userFactors: Matrix<float>.Build.DenseOfRowArrays(userFactors),
                itemFactors: Matrix<float>.Build.DenseOfRowArrays(itemFactors));

            using var stream = new MemoryStream();
            stream.WriteByte(42);
            var start = stream.Position;

            original.Save(stream);

            Assert.True(stream.CanRead);
            Assert.True(stream.CanWrite);
            var end = stream.Position;
            stream.Position = 0;
            Assert.Equal(42, stream.ReadByte());
            stream.Position = start;

            var reconstructed = AlternatingLeastSquaresRecommender.Load(stream);

            Assert.True(stream.CanRead);
            Assert.True(stream.CanWrite);
            Assert.Equal(end, stream.Position);
            Assert.Equal(original.Factors, reconstructed.Factors);
            Assert.Equal(original.Regularization, reconstructed.Regularization);
            Assert.Equal(original.Alpha, reconstructed.Alpha);
            Assert.Equal(original.Users.Count, reconstructed.Users.Count);
            Assert.Equal(original.Items.Count, reconstructed.Items.Count);
            Assert.Equal(original.UserFactors.RowCount, reconstructed.UserFactors.RowCount);
            Assert.Equal(original.UserFactors.ColumnCount, reconstructed.UserFactors.ColumnCount);
            Assert.Equal(original.ItemFactors.RowCount, reconstructed.ItemFactors.RowCount);
            Assert.Equal(original.ItemFactors.ColumnCount, reconstructed.ItemFactors.ColumnCount);

            foreach (var user in original.Users)
            {
                Assert.True(reconstructed.Users.TryGetValue(user.Key, out var row));
                Assert.Equal(original.UserFactors.Row(user.Value).ToArray(), reconstructed.UserFactors.Row(row).ToArray());

                using var expectedRecommendations = original.Recommend(user.Key);
                using var actualRecommendations = reconstructed.Recommend(user.Key);
                Assert.Equal(expectedRecommendations.ToArray(), actualRecommendations.ToArray());

                using var expectedSimilarUsers = original.SimilarUsers(user.Key);
                using var actualSimilarUsers = reconstructed.SimilarUsers(user.Key);
                Assert.Equal(expectedSimilarUsers.ToArray(), actualSimilarUsers.ToArray());
            }

            foreach (var item in original.Items)
            {
                Assert.True(reconstructed.Items.TryGetValue(item.Key, out var row));
                Assert.Equal(original.ItemFactors.Row(item.Value).ToArray(), reconstructed.ItemFactors.Row(row).ToArray());

                using var expectedSimilarItems = original.SimilarItems(item.Key);
                using var actualSimilarItems = reconstructed.SimilarItems(item.Key);
                Assert.Equal(expectedSimilarItems.ToArray(), actualSimilarItems.ToArray());
            }
        }

        protected override AlternatingLeastSquaresRecommender CreateRecommender(Dictionary<string, Dictionary<string, float>> userItems)
        {
            return
                AlternatingLeastSquaresRecommender.Fit(
                    UserItemMatrix.Build(userItems),
                    new AlternatingLeastSquaresParameters(
                        factors: 3,
                        regularization: 0,
                        useConjugateGradient: true,
                        random: new Random(42)));
        }

        private static void AssertFinite(float value)
        {
            Assert.False(float.IsNaN(value));
            Assert.False(float.IsInfinity(value));
        }

        private static void AssertSingleObservationSolution(Vector<float> actual, Vector<float> fixedFactors, float confidence, float regularization)
        {
            // For one positive observation, the minimizer is c * y / (lambda + c * ||y||^2).
            var squaredNorm = fixedFactors.Sum(value => (double)value * value);
            var scale = confidence / (regularization + (confidence * squaredNorm));

            for (var f = 0; f < actual.Count; f++)
            {
                var expected = scale * fixedFactors[f];
                AssertFinite(actual[f]);
                Assert.InRange(Math.Abs(actual[f] - expected), 0d, 1e-6d * Math.Max(1d, Math.Abs(expected)));
            }
        }
    }
}
