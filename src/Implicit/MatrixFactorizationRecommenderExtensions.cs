using System;
using System.Collections.Generic;
using System.Linq;

namespace Implicit
{
    public static class MatrixFactorizationRecommenderExtensions
    {
        public static IEnumerable<ItemResult> RecommendUser(
            this IMatrixFactorizationRecommender recommender,
            IEnumerable<string> items,
            bool excludeItems = false)
        {
            if (recommender == null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            return recommender
                .RecommendUser(recommender.ComputeUserFactors(items))
                .Where(o => !excludeItems || !items.Contains(o.ItemId));
        }

        public static IEnumerable<ItemResult> RecommendUser(
            this IMatrixFactorizationRecommender recommender,
            Dictionary<string, double> items,
            bool excludeItems = false)
        {
            if (recommender == null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            return recommender
                .RecommendUser(recommender.ComputeUserFactors(items))
                .Where(o => !excludeItems || !items.ContainsKey(o.ItemId));
        }

        public static IEnumerable<TKey> RankUsers<TKey>(
            this IMatrixFactorizationRecommender recommender,
            string userId,
            IEnumerable<KeyValuePair<TKey, IEnumerable<string>>> userItems)
        {
            if (recommender == null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (userItems == null)
            {
                throw new ArgumentNullException(nameof(userItems));
            }

            return recommender
                .RankUsers(userId, userItems.Select(o => new KeyValuePair<TKey, Dictionary<string, double>>(o.Key, o.Value.ToDictionary(p => p, p => 1.0))));
        }

        public static IEnumerable<TKey> RankUsers<TKey>(
            this IMatrixFactorizationRecommender recommender,
            string userId,
            IEnumerable<KeyValuePair<TKey, Dictionary<string, double>>> userItems)
        {
            if (recommender == null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (userItems == null)
            {
                throw new ArgumentNullException(nameof(userItems));
            }

            return recommender
                .RankUsers(userId, userItems.Select(o => new KeyValuePair<TKey, UserFactors>(o.Key, recommender.ComputeUserFactors(o.Value))));
        }

        public static UserFactors ComputeUserFactors(
            this IMatrixFactorizationRecommender recommender,
            IEnumerable<string> items)
        {
            if (recommender == null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            return recommender
                .ComputeUserFactors(items.ToDictionary(o => o, o => 1.0));
        }
    }
}
