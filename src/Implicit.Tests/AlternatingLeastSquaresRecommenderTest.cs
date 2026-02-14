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
            var errors = 0;

            for (var i = 0; i < original.RowCount; i++)
            {
                for (var j = 0; j < reconstructed.ColumnCount; j++)
                {
                    if (MathF.Abs(original[i, j] - reconstructed[i, j]) > 1e-3f)
                    {
                        errors++;
                    }
                }
            }

            Assert.Equal(0, errors);
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
    }
}
