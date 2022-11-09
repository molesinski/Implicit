namespace Implicit
{
    public static class RecommenderExtensions
    {
        public static RecommenderResult RecommendUser(
            this IRecommender recommender,
            string userId)
        {
            if (recommender is null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (userId is null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            return recommender.RecommendUser(userId, RecommenderResultBuilderFactory.Instance);
        }

        public static RecommenderResult RecommendItem(
            this IRecommender recommender,
            string itemId)
        {
            if (recommender is null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (itemId is null)
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            return recommender.RecommendItem(itemId, RecommenderResultBuilderFactory.Instance);
        }
    }
}
