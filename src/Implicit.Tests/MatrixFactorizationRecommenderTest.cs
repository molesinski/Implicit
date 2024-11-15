﻿using System.Globalization;
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
                var user = recommender.ComputeUserFeatures(new[] { itemId });
                var items = recommender.RecommendUser(user!).Take(11);

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
                var user = recommender.ComputeUserFeatures(data[userId].Keys);
                var items1 = recommender.RecommendUser(userId).Take(25);
                var items2 = recommender.RecommendUser(user!).Take(25);

                Assert.True(items1.SequenceEqual(items2));
            }
        }
    }
}
