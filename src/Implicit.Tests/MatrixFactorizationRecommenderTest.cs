using System.Globalization;
using MathNet.Numerics.LinearAlgebra;
using Xunit;

namespace Implicit.Tests
{
    public abstract class MatrixFactorizationRecommenderTest<TMatrixFactorizationRecommender> : RecommenderTest<TMatrixFactorizationRecommender>
        where TMatrixFactorizationRecommender : MatrixFactorizationRecommender
    {
        [Fact]
        public void RecommendConstructedFromItem()
        {
            var n = 50;
            var matrix = CreateCheckerBoard(n);
            var userItems = ToUserItems(matrix);
            var recommender = this.CreateRecommender(userItems);

            var itemKeys = userItems.SelectMany(x => x.Value).Select(x => x.Key).ToHashSet();

            foreach (var itemId in itemKeys)
            {
                var user = recommender.ComputeUserFeatures([new(itemId, 1f)]);
                var items = recommender.Recommend(user!);

                var parity = int.Parse(itemId, CultureInfo.InvariantCulture) % 2;

                var topQuarter = items
                    .Take(n / 4)
                    .Where(x => parity == int.Parse(x.Key, CultureInfo.InvariantCulture) % 2)
                    .Count() * 1.0 / (n / 4);

                var topHalf = items
                    .Take(n / 2)
                    .Where(x => parity == int.Parse(x.Key, CultureInfo.InvariantCulture) % 2)
                    .Count() * 1.0 / (n / 2);

                Assert.InRange(topQuarter, 0.66, 1);
                Assert.InRange(topHalf, 0.66, 1);
            }
        }

        [Fact]
        public void RecommendReconstructedUser()
        {
            var n = 50;
            var matrix = CreateCheckerBoard(n);
            var userItems = ToUserItems(matrix);
            var recommender = this.CreateRecommender(userItems);

            foreach (var userId in userItems.Keys)
            {
                var items = userItems[userId].Keys.ToDictionary(x => x, x => 1f);

                var user = recommender.ComputeUserFeatures(items)!;

                var original = string.Join(",", recommender.Recommend(userId).Select(x => x.Key));
                var reconstructed = string.Join(",", recommender.Recommend(user).Select(x => x.Key));

                Assert.Equal(original, reconstructed);
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

                foreach (var item in result)
                {
                    Assert.Equal(parity, int.Parse(item.Key, CultureInfo.InvariantCulture) % 2);
                }
            }
        }

        [Fact]
        public void SimilarFeaturesItems()
        {
            var n = 50;
            var matrix = CreateCheckerBoard(n);
            var userItems = ToUserItems(matrix);
            var recommender = this.CreateRecommender(userItems);

            var itemKeys = userItems.SelectMany(x => x.Value).Select(x => x.Key).ToHashSet();
            var itemFeatures = itemKeys.ToDictionary(x => x, x => recommender.GetItemFeatures(x)!);

            foreach (var itemId in itemKeys)
            {
                var features = recommender.GetItemFeatures(itemId)!;
                var result = features.SimilarFeatures(itemFeatures).Take(n / 2);
                var parity = int.Parse(itemId, CultureInfo.InvariantCulture) % 2;

                foreach (var item in result)
                {
                    Assert.Equal(parity, int.Parse(item.Key, CultureInfo.InvariantCulture) % 2);
                }
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
