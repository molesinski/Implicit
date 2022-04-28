using System;

namespace Implicit
{
    public static class RecommenderExtensions
    {
        public static RecommenderResult RecommendUser(
            this IRecommender recommender,
            string userId)
        {
            if (recommender == null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            return recommender.RecommendUser(userId, RecommenderResultBuilderFactory.Instance);
        }

        public static RecommenderResult RecommendItem(
            this IRecommender recommender,
            string itemId)
        {
            if (recommender == null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (itemId == null)
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            return recommender.RecommendItem(itemId, RecommenderResultBuilderFactory.Instance);
        }
    }
}
