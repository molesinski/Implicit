using System.Globalization;
using MathNet.Numerics.LinearAlgebra;
using Xunit;

namespace Implicit.Tests
{
    public abstract class MatrixFactorizationRecommenderTest<TMatrixFactorizationRecommender> : RecommenderTest<TMatrixFactorizationRecommender>
        where TMatrixFactorizationRecommender : MatrixFactorizationRecommender
    {
        [Fact]
        public void RecommendReconstructedUser()
        {
            var n = 50;
            var matrix = CreateCheckerBoard(n);
            var userItems = ToUserItems(matrix);
            var recommender = this.CreateRecommender(userItems);

            foreach (var userId in userItems.Keys)
            {
                var user = recommender.ComputeUserFeatures(userItems[userId])!;

                using var original = recommender.Recommend(userId);
                using var reconstructed = recommender.Recommend(user);

                Assert.Equal(original.Select(x => x.Key), reconstructed.Select(x => x.Key));
            }
        }

        [Fact]
        public void RecommendUserConstructedFromItem()
        {
            var n = 50;
            var matrix = CreateCheckerBoard(n);
            var userItems = ToUserItems(matrix);
            var recommender = this.CreateRecommender(userItems);

            var itemKeys = userItems.SelectMany(x => x.Value).Select(x => x.Key).Distinct().ToList();

            foreach (var itemId in itemKeys)
            {
                var user = recommender.ComputeUserFeatures([new(itemId, 1f)])!;
                using var result = recommender.SimilarUsers(user);
                var parity = int.Parse(itemId, CultureInfo.InvariantCulture) % 2;

                var hits = result.Take(n / 2).Count(x => parity == int.Parse(x.Key, CultureInfo.InvariantCulture) % 2);

                Assert.True(hits > (n / 2) / 2);
            }
        }

        [Fact]
        public void SimilarFeaturesUsers()
        {
            var n = 50;
            var matrix = CreateCheckerBoard(n);
            var userItems = ToUserItems(matrix);
            var recommender = this.CreateRecommender(userItems);

            var userFeatures = userItems.Keys.ToDictionary(x => x, x => recommender.GetUserFeatures(x)!);

            foreach (var userId in userItems.Keys)
            {
                var features = recommender.GetUserFeatures(userId)!;
                using var result = features.SimilarFeatures(userFeatures);
                var parity = int.Parse(userId, CultureInfo.InvariantCulture) % 2;

                var hits = result.Take(n / 2).Count(x => parity == int.Parse(x.Key, CultureInfo.InvariantCulture) % 2);

                Assert.True(hits == n / 2);
            }
        }

        [Fact]
        public void SimilarFeaturesItems()
        {
            var n = 50;
            var matrix = CreateCheckerBoard(n);
            var userItems = ToUserItems(matrix);
            var recommender = this.CreateRecommender(userItems);

            var itemKeys = userItems.SelectMany(x => x.Value).Select(x => x.Key).Distinct().ToList();
            var itemFeatures = itemKeys.ToDictionary(x => x, x => recommender.GetItemFeatures(x)!);

            foreach (var itemId in itemKeys)
            {
                var features = recommender.GetItemFeatures(itemId)!;
                using var result = features.SimilarFeatures(itemFeatures);
                var parity = int.Parse(itemId, CultureInfo.InvariantCulture) % 2;

                var hits = result.Take(n / 2).Count(x => parity == int.Parse(x.Key, CultureInfo.InvariantCulture) % 2);

                Assert.True(hits == n / 2);
            }
        }

        protected static Matrix<float> ToReconstructedMatrix(MatrixFactorizationRecommender recommender)
        {
            if (recommender is null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            var reconstructed = recommender.UserFactors.Multiply(recommender.ItemFactors.Transpose());
            var matrix = Matrix<float>.Build.Dense(recommender.Users.Count, recommender.Items.Count);

            foreach (var user in recommender.Users)
            {
                var i = int.Parse(user.Key, CultureInfo.InvariantCulture);

                foreach (var item in recommender.Items)
                {
                    var j = int.Parse(item.Key, CultureInfo.InvariantCulture);

                    matrix[i, j] = reconstructed[user.Value, item.Value];
                }
            }

            return matrix;
        }
    }
}
