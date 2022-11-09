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

            foreach (var userId in Enumerable.Range(0, n).Select(o => o.ToString(CultureInfo.InvariantCulture)))
            {
                var items = recommender.RecommendUser(userId).Take(25);

                Assert.Equal(userId, items.Except(data[userId].Keys).First());
            }
        }

        [Fact]
        public void RecommendItem()
        {
            var n = 50;
            var data = this.CreateCheckerBoard(n);
            var recommender = this.CreateRecommender(data);

            foreach (var itemId in Enumerable.Range(0, n).Select(o => o.ToString(CultureInfo.InvariantCulture)))
            {
                var items = recommender.RecommendItem(itemId).Take(10);

                foreach (var item in items)
                {
                    Assert.Equal(int.Parse(item, CultureInfo.InvariantCulture) % 2, int.Parse(itemId, CultureInfo.InvariantCulture) % 2);
                }
            }
        }

        protected abstract TRecommender CreateRecommender(Dictionary<string, Dictionary<string, double>> data);

        protected Dictionary<string, Dictionary<string, double>> CreateCheckerBoard(int n)
        {
            return Enumerable.Range(0, n)
                .SelectMany(o => Enumerable.Range(0, n), (i, j) => new { i, j })
                .Where(o => o.i % 2 == o.j % 2)
                .Where(o => o.i != o.j)
                .Select(o => new { UserId = o.i.ToString(CultureInfo.InvariantCulture), ItemId = o.j.ToString(CultureInfo.InvariantCulture), Confidence = 1.0 })
                .GroupBy(o => o.UserId)
                .ToDictionary(o => o.Key, o => o.ToDictionary(p => p.ItemId, p => p.Confidence));
        }
    }
}
