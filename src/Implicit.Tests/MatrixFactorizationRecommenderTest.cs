using System.Globalization;
using System.Linq;
using Xunit;

namespace Implicit.Tests
{
    public abstract class MatrixFactorizationRecommenderTest<TMatrixFactorizationRecommender> : RecommenderTest<TMatrixFactorizationRecommender>
        where TMatrixFactorizationRecommender : IMatrixFactorizationRecommender
    {
        [Fact]
        public void RecommendUserForItem()
        {
            var n = 50;
            var data = this.CreateCheckerBoard(n);
            var recommender = this.CreateRecommender(data);

            foreach (var itemId in Enumerable.Range(0, n).Select(o => o.ToString(CultureInfo.InvariantCulture)))
            {
                var user = recommender.ComputeUserFactors(new[] { itemId });
                var items = recommender.RecommendUser(user).Take(11);

                var parity = items
                    .Select(o => int.Parse(o, CultureInfo.InvariantCulture) % 2)
                    .GroupBy(o => o)
                    .OrderByDescending(o => o.Count())
                    .Select(o => o.Key)
                    .First();

                Assert.Equal(parity, int.Parse(itemId, CultureInfo.InvariantCulture) % 2);
            }
        }

        [Fact]
        public void RecommendUserForItems()
        {
            var n = 50;
            var data = this.CreateCheckerBoard(n);
            var recommender = this.CreateRecommender(data);

            foreach (var userId in Enumerable.Range(0, n).Select(o => o.ToString(CultureInfo.InvariantCulture)))
            {
                var user = recommender.ComputeUserFactors(data[userId].Keys);
                var items1 = recommender.RecommendUser(userId).Take(25);
                var items2 = recommender.RecommendUser(user).Take(25);

                Assert.True(items1.SequenceEqual(items2));
            }
        }

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER

        [Fact]
        public void RecommendUserForItemPooled()
        {
            var n = 50;
            var data = this.CreateCheckerBoard(n);
            var recommender = this.CreateRecommender(data);

            foreach (var itemId in Enumerable.Range(0, n).Select(o => o.ToString(CultureInfo.InvariantCulture)))
            {
                var user = recommender.ComputeUserFactors(new[] { itemId });
                using var result = recommender.RecommendUser(user, PooledRecommenderResultBuilderFactory.Shared);
                var items = result.Take(11);

                var parity = items
                    .Select(o => int.Parse(o, CultureInfo.InvariantCulture) % 2)
                    .GroupBy(o => o)
                    .OrderByDescending(o => o.Count())
                    .Select(o => o.Key)
                    .First();

                Assert.Equal(parity, int.Parse(itemId, CultureInfo.InvariantCulture) % 2);
            }
        }

        [Fact]
        public void RecommendUserForItemsPooled()
        {
            var n = 50;
            var data = this.CreateCheckerBoard(n);
            var recommender = this.CreateRecommender(data);

            foreach (var userId in Enumerable.Range(0, n).Select(o => o.ToString(CultureInfo.InvariantCulture)))
            {
                var user = recommender.ComputeUserFactors(data[userId].Keys);
                using var result1 = recommender.RecommendUser(userId, PooledRecommenderResultBuilderFactory.Shared);
                using var result2 = recommender.RecommendUser(user, PooledRecommenderResultBuilderFactory.Shared);
                var items1 = result1.Take(25);
                var items2 = result2.Take(25);

                Assert.True(items1.SequenceEqual(items2));
            }
        }

#endif
    }
}
