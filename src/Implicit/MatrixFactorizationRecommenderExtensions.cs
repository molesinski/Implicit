﻿namespace Implicit
{
    public static class MatrixFactorizationRecommenderExtensions
    {
        public static RecommenderResult RecommendUser(
            this IMatrixFactorizationRecommender recommender,
            IEnumerable<string> items)
        {
            if (recommender is null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            return recommender.RecommendUser(recommender.ComputeUserFactors(items));
        }

        public static RecommenderResult RecommendUser(
            this IMatrixFactorizationRecommender recommender,
            Dictionary<string, double> items)
        {
            if (recommender is null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            return recommender.RecommendUser(recommender.ComputeUserFeatures(items));
        }

        public static RecommenderResult RankUsers(
            this IMatrixFactorizationRecommender recommender,
            string userId,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> userItems)
        {
            if (recommender is null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (userId is null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (userItems is null)
            {
                throw new ArgumentNullException(nameof(userItems));
            }

            return recommender.RankUsers(userId, userItems.Select(o => new KeyValuePair<string, UserFeatures>(o.Key, recommender.ComputeUserFactors(o.Value))).ToList());
        }

        public static RecommenderResult RankUsers(
            this IMatrixFactorizationRecommender recommender,
            string userId,
            IEnumerable<KeyValuePair<string, Dictionary<string, double>>> userItems)
        {
            if (recommender is null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (userId is null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (userItems is null)
            {
                throw new ArgumentNullException(nameof(userItems));
            }

            return recommender.RankUsers(userId, userItems.Select(o => new KeyValuePair<string, UserFeatures>(o.Key, recommender.ComputeUserFeatures(o.Value))).ToList());
        }

        public static UserFeatures ComputeUserFactors(
            this IMatrixFactorizationRecommender recommender,
            IEnumerable<string> items)
        {
            if (recommender is null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            return recommender.ComputeUserFeatures(items.ToDictionary(o => o, o => 1.0));
        }
    }
}
