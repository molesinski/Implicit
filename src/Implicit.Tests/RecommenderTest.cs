using System.Globalization;
using MathNet.Numerics.LinearAlgebra;
using Xunit;

namespace Implicit.Tests
{
    public abstract class RecommenderTest<TRecommender>
        where TRecommender : Recommender
    {
        [Fact]
        public void Recommend()
        {
            var n = 50;
            var matrix = CreateCheckerBoard(n);
            var userItems = ToUserItems(matrix);
            var recommender = this.CreateRecommender(userItems);

            foreach (var userId in userItems.Keys)
            {
                var items = recommender.Recommend(userId);

                Assert.Equal(userId, items.Select(x => x.Key).Except(userItems[userId].Keys).First());
            }
        }

        [Fact]
        public void SimilarUsers()
        {
            var n = 50;
            var matrix = CreateCheckerBoard(n);
            var userItems = ToUserItems(matrix);
            var recommender = this.CreateRecommender(userItems);

            foreach (var userId in userItems.Keys)
            {
                var result = recommender.SimilarUsers(userId).Take(n / 2);
                var parity = int.Parse(userId, CultureInfo.InvariantCulture) % 2;

                var hits = result.Count(x => parity == int.Parse(x.Key, CultureInfo.InvariantCulture) % 2);

                Assert.True(hits == n / 2);
            }
        }

        [Fact]
        public void SimilarItems()
        {
            var n = 50;
            var matrix = CreateCheckerBoard(n);
            var userItems = ToUserItems(matrix);
            var recommender = this.CreateRecommender(userItems);

            var itemKeys = userItems.SelectMany(x => x.Value).Select(x => x.Key).Distinct().ToList();

            foreach (var itemId in itemKeys)
            {
                var result = recommender.SimilarItems(itemId).Take(n / 2);
                var parity = int.Parse(itemId, CultureInfo.InvariantCulture) % 2;

                var hits = result.Count(x => parity == int.Parse(x.Key, CultureInfo.InvariantCulture) % 2);

                Assert.True(hits == n / 2);
            }
        }

        protected static Matrix<float> CreateCheckerBoard(int n)
        {
            var matrix = Matrix<float>.Build.Dense(n, n);

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    if (i != j)
                    {
                        if (i % 2 == j % 2)
                        {
                            matrix[i, j] = 1f;
                        }
                    }
                }
            }

            return matrix;
        }

        protected static Dictionary<string, Dictionary<string, float>> ToUserItems(Matrix<float> matrix)
        {
            if (matrix is null)
            {
                throw new ArgumentNullException(nameof(matrix));
            }

            var userItems = new Dictionary<string, Dictionary<string, float>>();

            for (var i = 0; i < matrix.RowCount; i++)
            {
                var items = new Dictionary<string, float>();

                for (var j = 0; j < matrix.ColumnCount; j++)
                {
                    var confidence = matrix[i, j];

                    if (confidence != 0)
                    {
                        items.Add(j.ToString(CultureInfo.InvariantCulture), confidence);
                    }
                }

                userItems.Add(i.ToString(CultureInfo.InvariantCulture), items);
            }

            return userItems;
        }

        protected abstract TRecommender CreateRecommender(Dictionary<string, Dictionary<string, float>> userItems);
    }
}
