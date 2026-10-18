using System.Globalization;
using Xunit;

namespace Implicit.Tests
{
    public class AlternatingLeastSquaresLossTest
    {
        [Theory]
        [InlineData(false, false, 1)]
        [InlineData(false, false, 2)]
        [InlineData(false, true, 1)]
        [InlineData(false, true, 2)]
        [InlineData(true, false, 1)]
        [InlineData(true, false, 2)]
        [InlineData(true, true, 1)]
        [InlineData(true, true, 2)]
        public void ReportedLossMatchesObjective(bool conjugateGradient, bool negativeConfidence, int parallelism)
        {
            var sign = negativeConfidence ? -1f : 1f;
            var interactions = new[]
            {
                new UserItemValue("u0", "i0", 2f),
                new UserItemValue("u0", "i1", sign * 0.25f),
                new UserItemValue("u1", "i1", 3f),
                new UserItemValue("u1", "i2", sign * 1.5f),
                new UserItemValue("u2", "i0", 1f),
                new UserItemValue("u2", "i2", 0.5f),
            };
            var input = UserItemMatrix.Build(interactions);
            var parameters = new AlternatingLeastSquaresParameters(
                factors: 2,
                regularization: 0.2f,
                alpha: 2.5f,
                iterations: 3,
                useConjugateGradient: conjugateGradient,
                calculateLossAtIteration: true,
                parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = parallelism },
                random: new Random(42));
            float? reportedLoss = null;
            parameters.OnIterationCompleted = context => reportedLoss = context.Loss;

            var model = AlternatingLeastSquaresRecommender.Fit(input, parameters);
            var expected = CalculateObjective(input, model);

            Assert.NotNull(reportedLoss);
            Assert.InRange(Math.Abs(expected - reportedLoss.GetValueOrDefault()), 0d, 1e-5d);
        }

        [Theory]
        [InlineData(false, 1e-7f)]
        [InlineData(false, -1e-7f)]
        [InlineData(false, 1e-8f)]
        [InlineData(false, -1e-8f)]
        [InlineData(false, 1e-20f)]
        [InlineData(false, -1e-20f)]
        [InlineData(true, 1e-7f)]
        [InlineData(true, -1e-7f)]
        [InlineData(true, 1e-8f)]
        [InlineData(true, -1e-8f)]
        [InlineData(true, 1e-20f)]
        [InlineData(true, -1e-20f)]
        public void ReportedLossHandlesSmallConfidence(bool conjugateGradient, float confidence)
        {
            // With no missing pairs, the denominator must retain even confidence below double epsilon at one.
            var interactions = new[] { new UserItemValue("user", "item", confidence * 4f) };
            var input = UserItemMatrix.Build(interactions);
            var parameters = new AlternatingLeastSquaresParameters(
                factors: 1,
                regularization: 0.1f,
                alpha: 0.25f,
                iterations: 1,
                useConjugateGradient: conjugateGradient,
                calculateLossAtIteration: true,
                random: new Random(42));
            float? reportedLoss = null;
            parameters.OnIterationCompleted = context => reportedLoss = context.Loss;

            var model = AlternatingLeastSquaresRecommender.Fit(input, parameters);
            var expected = CalculateObjective(input, model);

            Assert.NotNull(reportedLoss);
            var actual = reportedLoss.GetValueOrDefault();
            Assert.False(float.IsNaN(actual));
            Assert.False(float.IsInfinity(actual));
            Assert.InRange(Math.Abs(expected - actual), 0d, 1e-6d * Math.Max(1d, Math.Abs(expected)));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NegativeOnlyObservationsHaveZeroLoss(bool conjugateGradient)
        {
            var interactions = new[] { new UserItemValue("user", "item", -2f) };
            var parameters = new AlternatingLeastSquaresParameters(
                factors: 1,
                regularization: 0.1f,
                alpha: 3f,
                iterations: 1,
                useConjugateGradient: conjugateGradient,
                calculateLossAtIteration: true,
                random: new Random(42));
            float? reportedLoss = null;
            parameters.OnIterationCompleted = context => reportedLoss = context.Loss;

            AlternatingLeastSquaresRecommender.Fit(UserItemMatrix.Build(interactions), parameters);

            Assert.NotNull(reportedLoss);
            Assert.InRange(Math.Abs(reportedLoss.GetValueOrDefault()), 0f, 1e-6f);
        }

        [Theory]
        [InlineData(46341)]
        [InlineData(65536)]
        public void ReportedLossHandlesLargePairCounts(int dimension)
        {
            // A sparse diagonal keeps storage linear while the number of pairs exceeds Int32.MaxValue.
            var interactions = Enumerable.Range(0, dimension).Select(index =>
            {
                var id = index.ToString(CultureInfo.InvariantCulture);
                return new UserItemValue(id, id, 1f);
            });
            var parameters = new AlternatingLeastSquaresParameters(
                factors: 1,
                regularization: 0.1f,
                iterations: 1,
                useConjugateGradient: false,
                calculateLossAtIteration: true,
                parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = 1 },
                random: new Random(42));
            float? reportedLoss = null;
            parameters.OnIterationCompleted = context => reportedLoss = context.Loss;

            var model = AlternatingLeastSquaresRecommender.Fit(UserItemMatrix.Build(interactions), parameters);
            var userSquaredNorm = 0d;
            var itemSquaredNorm = 0d;
            var observedSquaredError = 0d;
            var observedSquaredPredictions = 0d;

            for (var index = 0; index < dimension; index++)
            {
                var userFactor = (double)model.UserFactors[index, 0];
                var itemFactor = (double)model.ItemFactors[index, 0];
                var prediction = userFactor * itemFactor;
                var error = 1d - prediction;

                userSquaredNorm += userFactor * userFactor;
                itemSquaredNorm += itemFactor * itemFactor;
                observedSquaredError += error * error;
                observedSquaredPredictions += prediction * prediction;
            }

            // With one factor, the product of squared norms sums squared predictions over all pairs.
            var missingSquaredError = (userSquaredNorm * itemSquaredNorm) - observedSquaredPredictions;
            var penalty = parameters.Regularization * (userSquaredNorm + itemSquaredNorm);
            var pairCount = (long)dimension * dimension;
            var expected = (observedSquaredError + missingSquaredError + penalty) / pairCount;

            Assert.True(pairCount > int.MaxValue);
            Assert.NotNull(reportedLoss);
            Assert.True(reportedLoss.GetValueOrDefault() > 0f);
            Assert.InRange(Math.Abs((reportedLoss.GetValueOrDefault() / expected) - 1d), 0d, 1e-4d);
        }

        private static double CalculateObjective(UserItemMatrix input, AlternatingLeastSquaresRecommender model)
        {
            // Evaluate every pair directly, including missing observations, without Gram matrices.
            var squaredError = 0d;
            var totalConfidence = 0d;

            for (var u = 0; u < input.Matrix.RowCount; u++)
            {
                for (var i = 0; i < input.Matrix.ColumnCount; i++)
                {
                    var value = (double)input.Matrix[u, i] * model.Alpha;
                    var confidence = value == 0d ? 1d : Math.Abs(value);
                    var preference = value > 0d ? 1d : 0d;
                    var prediction = 0d;

                    for (var f = 0; f < model.Factors; f++)
                    {
                        prediction += (double)model.UserFactors[u, f] * model.ItemFactors[i, f];
                    }

                    var error = preference - prediction;
                    squaredError += confidence * error * error;
                    totalConfidence += confidence;
                }
            }

            var squaredNorms = 0d;

            foreach (var value in model.UserFactors.Enumerate())
            {
                squaredNorms += (double)value * value;
            }

            foreach (var value in model.ItemFactors.Enumerate())
            {
                squaredNorms += (double)value * value;
            }

            return (squaredError + (model.Regularization * squaredNorms)) / totalConfidence;
        }
    }
}
