using System;
using System.Collections.Generic;
using System.Linq;

namespace Implicit
{
    public static class MatrixFactorizationRecommenderExtensions
    {
        public static RecommenderResults RecommendUser(
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

            var results = recommender.RecommendUser(recommender.ComputeUserFactors(items));

            return results;
        }

        public static RecommenderResults RecommendUser(
            this IMatrixFactorizationRecommender recommender,
            Dictionary<string, double> items)
        {
            if (recommender == null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var results = recommender.RecommendUser(recommender.ComputeUserFactors(items));

            return results;
        }

        public static RecommenderResults RankUsers(
            this IMatrixFactorizationRecommender recommender,
            string userId,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> userItems)
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

            return recommender.RankUsers(userId, userItems.Select(o => new KeyValuePair<string, Dictionary<string, double>>(o.Key, o.Value.ToDictionary(p => p, p => 1.0))));
        }

        public static RecommenderResults RankUsers(
            this IMatrixFactorizationRecommender recommender,
            string userId,
            IEnumerable<KeyValuePair<string, Dictionary<string, double>>> userItems)
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

            return recommender.RankUsers(userId, userItems.Select(o => new KeyValuePair<string, UserFactors>(o.Key, recommender.ComputeUserFactors(o.Value))));
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

            return recommender.ComputeUserFactors(items.ToDictionary(o => o, o => 1.0));
        }
    }
}
