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

                var original = string.Join(",", recommender.Recommend(userId).Select(x => x.Key));
                var reconstructed = string.Join(",", recommender.Recommend(user).Select(x => x.Key));

                Assert.Equal(original, reconstructed);
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
                var result = recommender.SimilarUsers(user).Take(n / 2);
                var parity = int.Parse(itemId, CultureInfo.InvariantCulture) % 2;

                var hits = result.Count(x => parity == int.Parse(x.Key, CultureInfo.InvariantCulture) % 2);

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
                var result = features.SimilarFeatures(userFeatures).Take(n / 2);
                var parity = int.Parse(userId, CultureInfo.InvariantCulture) % 2;

                var hits = result.Count(x => parity == int.Parse(x.Key, CultureInfo.InvariantCulture) % 2);

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
                var result = features.SimilarFeatures(itemFeatures).Take(n / 2);
                var parity = int.Parse(itemId, CultureInfo.InvariantCulture) % 2;

                var hits = result.Count(x => parity == int.Parse(x.Key, CultureInfo.InvariantCulture) % 2);

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
