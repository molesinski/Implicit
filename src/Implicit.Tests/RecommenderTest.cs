using System.Globalization;
using Xunit;

namespace Implicit.Tests
{
    public abstract class RecommenderTest<TRecommender>
        where TRecommender : IRecommender
    {
        [Fact]
        public void RecommendUser()
        {
            var n = 50;
            var data = this.CreateCheckerBoard(n);
            var recommender = this.CreateRecommender(data);

            foreach (var userId in Enumerable.Range(0, n).Select(x => x.ToString(CultureInfo.InvariantCulture)))
            {
                var items = recommender.RecommendUser(userId).Take(25);

                Assert.Equal(userId, items.Select(x => x.Key).Except(data[userId].Keys).First());
            }
        }

        [Fact]
        public void RecommendItem()
        {
            var n = 50;
            var data = this.CreateCheckerBoard(n);
            var recommender = this.CreateRecommender(data);

            foreach (var itemId in Enumerable.Range(0, n).Select(x => x.ToString(CultureInfo.InvariantCulture)))
            {
                var items = recommender.RecommendItem(itemId).Take(10);

                foreach (var item in items)
                {
                    Assert.Equal(int.Parse(item.Key, CultureInfo.InvariantCulture) % 2, int.Parse(itemId, CultureInfo.InvariantCulture) % 2);
                }
            }
        }

        protected abstract TRecommender CreateRecommender(Dictionary<string, Dictionary<string, double>> data);

        protected Dictionary<string, Dictionary<string, double>> CreateCheckerBoard(int n)
        {
            return Enumerable.Range(0, n)
                .SelectMany(x => Enumerable.Range(0, n), (i, j) => new { i, j })
                .Where(x => x.i % 2 == x.j % 2)
                .Where(x => x.i != x.j)
                .Select(x => new { UserId = x.i.ToString(CultureInfo.InvariantCulture), ItemId = x.j.ToString(CultureInfo.InvariantCulture), Confidence = 1.0 })
                .GroupBy(x => x.UserId)
                .ToDictionary(x => x.Key, x => x.ToDictionary(x => x.ItemId, x => x.Confidence));
        }
    }
}
