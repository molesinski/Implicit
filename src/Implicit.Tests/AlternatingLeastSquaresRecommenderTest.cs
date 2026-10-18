using System.Globalization;
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
            var n = 50;
            var matrix = CreateCheckerBoard(n);
            var userItems = ToUserItems(matrix);
            var original = this.CreateRecommender(userItems);

            using var stream = new MemoryStream();

            original.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);

            var reconstructed = AlternatingLeastSquaresRecommender.Load(stream);

            foreach (var userId in Enumerable.Range(0, n).Select(x => x.ToString(CultureInfo.InvariantCulture)))
            {
                var items1 = string.Join(",", original.Recommend(userId).Select(x => x.Key));
                var items2 = string.Join(",", reconstructed.Recommend(userId).Select(x => x.Key));

                Assert.Equal(items1, items2);
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
