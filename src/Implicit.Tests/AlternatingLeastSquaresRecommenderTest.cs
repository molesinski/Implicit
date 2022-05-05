using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Implicit.Tests
{
    public class AlternatingLeastSquaresRecommenderTest : MatrixFactorizationRecommenderTest<AlternatingLeastSquares>
    {
        [Fact]
        public void Factorize()
        {
            var matrix = new double[][]
            {
                new double[] { 1, 1, 0, 1, 0, 0 },
                new double[] { 0, 1, 1, 1, 0, 0 },
                new double[] { 1, 0, 1, 0, 0, 0 },
                new double[] { 1, 1, 0, 0, 0, 0 },
                new double[] { 0, 0, 1, 1, 0, 1 },
                new double[] { 0, 1, 0, 0, 0, 1 },
                new double[] { 0, 0, 0, 0, 1, 1 },
            };

            var data = Enumerable.Range(0, 7)
                .SelectMany(
                    o => Enumerable.Range(0, 6),
                    (o, p) => new { UserId = o.ToString(), ItemId = p.ToString(), Confidence = matrix[o][p] })
                .Where(o => o.Confidence > 0)
                .GroupBy(o => o.UserId)
                .ToDictionary(o => o.Key, o => o.ToDictionary(p => p.ItemId, p => p.Confidence * 2));

            var parametersScenarios = new[]
            {
                new AlternatingLeastSquaresParameters(factors: 6, regularization: 0, iterations: 15, useConjugateGradient: true),
                new AlternatingLeastSquaresParameters(factors: 6, regularization: 0, iterations: 15, useConjugateGradient: false),
            };

            foreach (var parameters in parametersScenarios)
            {
                var recommender = AlternatingLeastSquares.Fit(DataMatrix.Load(data), parameters);

                Assert.True(recommender.Loss < 0.00001);
            }
        }

        [Fact]
        public void SaveLoad()
        {
            var n = 50;
            var data = this.CreateCheckerBoard(n);
            var recommender1 = this.CreateRecommender(data);

            var stream = new MemoryStream();

            recommender1.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);

            var recommender2 = AlternatingLeastSquares.Load(stream);

            foreach (var userId in Enumerable.Range(0, n).Select(o => o.ToString()))
            {
                var items1 = recommender1.RecommendUser(userId).Keys;
                var items2 = recommender2.RecommendUser(userId).Keys;

                Assert.True(items1.SequenceEqual(items2));
            }
        }

        protected override AlternatingLeastSquares CreateRecommender(Dictionary<string, Dictionary<string, double>> data)
        {
            var parameters = new AlternatingLeastSquaresParameters(factors: 3, regularization: 0, iterations: 15, useConjugateGradient: true);
            var recommender = AlternatingLeastSquares.Fit(DataMatrix.Load(data), parameters);

            return recommender;
        }
    }
}
