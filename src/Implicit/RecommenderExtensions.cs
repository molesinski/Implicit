using System;

namespace Implicit
{
    public static class RecommenderExtensions
    {
        public static RecommenderResults RecommendUser(
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

            return recommender.RecommendUser(userId, RecommenderResultsBuilderFactory.Instance);
        }

        public static RecommenderResults RecommendItem(
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

            return recommender.RecommendItem(itemId, RecommenderResultsBuilderFactory.Instance);
        }
    }
}
