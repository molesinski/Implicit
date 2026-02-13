using System.Globalization;
using Xunit;

namespace Implicit.Tests
{
    public class AlternatingLeastSquaresRecommenderTest : MatrixFactorizationRecommenderTest<AlternatingLeastSquares>
    {
        [Fact]
        public void Factorize()
        {
            var matrix = new float[][]
            {
                new float[] { 1, 1, 0, 1, 0, 0 },
                new float[] { 0, 1, 1, 1, 0, 0 },
                new float[] { 1, 0, 1, 0, 0, 0 },
                new float[] { 1, 1, 0, 0, 0, 0 },
                new float[] { 0, 0, 1, 1, 0, 1 },
                new float[] { 0, 1, 0, 0, 0, 1 },
                new float[] { 0, 0, 0, 0, 1, 1 },
            };

            var data = Enumerable.Range(0, 7)
                .SelectMany(
                    x => Enumerable.Range(0, 6),
                    (x, y) => new { UserId = x.ToString(CultureInfo.InvariantCulture), ItemId = y.ToString(CultureInfo.InvariantCulture), Confidence = matrix[x][y] })
                .Where(x => x.Confidence > 0)
                .GroupBy(x => x.UserId)
                .ToDictionary(x => x.Key, x => x.ToDictionary(p => p.ItemId, p => p.Confidence * 2));

            var parametersScenarios = new[]
            {
                new AlternatingLeastSquaresParameters(factors: 6, regularization: 0, iterations: 15, useConjugateGradient: true),
                new AlternatingLeastSquaresParameters(factors: 6, regularization: 0, iterations: 15, useConjugateGradient: false),
            };

            foreach (var parameters in parametersScenarios)
            {
                var recommender = AlternatingLeastSquares.Fit(DataMatrix.Build(data), parameters);

                Assert.True(recommender.Loss < 0.00001f);
            }
        }

        [Fact]
        public void SaveLoad()
        {
            var n = 50;
            var data = this.CreateCheckerBoard(n);
            var recommender1 = this.CreateRecommender(data);

            using var stream = new MemoryStream();

            recommender1.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);

            var recommender2 = AlternatingLeastSquares.Load(stream);

            foreach (var userId in Enumerable.Range(0, n).Select(x => x.ToString(CultureInfo.InvariantCulture)))
            {
                var items1 = recommender1.RecommendUser(userId);
                var items2 = recommender2.RecommendUser(userId);

                Assert.True(items1.SequenceEqual(items2));
            }
        }

        protected override AlternatingLeastSquares CreateRecommender(Dictionary<string, Dictionary<string, float>> data)
        {
            var parameters = new AlternatingLeastSquaresParameters(factors: 3, regularization: 0, iterations: 15, useConjugateGradient: true);
            var recommender = AlternatingLeastSquares.Fit(DataMatrix.Build(data), parameters);

            return recommender;
        }
    }
}
