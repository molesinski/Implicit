using System.Collections.Generic;
using System.Linq;
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

            foreach (var userId in Enumerable.Range(0, n).Select(o => o.ToString()))
            {
                var items = recommender.RecommendUser(userId).Take(25).Select(o => o.ItemId);

                Assert.Equal(userId, items.Except(data[userId].Keys).First());
            }
        }

        [Fact]
        public void RecommendItem()
        {
            var n = 50;
            var data = this.CreateCheckerBoard(n);
            var recommender = this.CreateRecommender(data);

            foreach (var itemId in Enumerable.Range(0, n).Select(o => o.ToString()))
            {
                var items = recommender.RecommendItem(itemId).Take(10).Select(o => o.ItemId);

                foreach (var item in items)
                {
                    Assert.Equal(int.Parse(item) % 2, int.Parse(itemId) % 2);
                }
            }
        }

        protected abstract TRecommender CreateRecommender(Dictionary<string, Dictionary<string, float>> data);

        protected Dictionary<string, Dictionary<string, float>> CreateCheckerBoard(int n)
        {
            return Enumerable.Range(0, n)
                .SelectMany(o => Enumerable.Range(0, n), (i, j) => new { i, j })
                .Where(o => o.i % 2 == o.j % 2)
                .Where(o => o.i != o.j)
                .Select(o => new { UserId = o.i.ToString(), ItemId = o.j.ToString(), Confidence = 1.0 })
                .GroupBy(o => o.UserId)
                .ToDictionary(o => o.Key, o => o.ToDictionary(p => p.ItemId, p => (float)p.Confidence));
        }
    }
}
