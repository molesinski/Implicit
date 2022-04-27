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

            return recommender.RecommendUser(recommender.ComputeUserFactors(items));
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

            return recommender.RecommendUser(recommender.ComputeUserFactors(items));
        }

        public static RecommenderResults RecommendUser(
            this IMatrixFactorizationRecommender recommender,
            UserFactors user)
        {
            if (recommender == null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return recommender.RecommendUser(user, RecommenderResultsBuilderFactory.Instance);
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

            return recommender.RankUsers(userId, userItems.Select(o => new KeyValuePair<string, UserFactors>(o.Key, recommender.ComputeUserFactors(o.Value))).ToList());
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

            return recommender.RankUsers(userId, userItems.Select(o => new KeyValuePair<string, UserFactors>(o.Key, recommender.ComputeUserFactors(o.Value))).ToList());
        }

        public static RecommenderResults RankUsers(
            this IMatrixFactorizationRecommender recommender,
            string userId,
            List<KeyValuePair<string, UserFactors>> users)
        {
            if (recommender == null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (users == null)
            {
                throw new ArgumentNullException(nameof(users));
            }

            return recommender.RankUsers(userId, users, RecommenderResultsBuilderFactory.Instance);
        }

        public static RecommenderResults RankUsers(
            this IMatrixFactorizationRecommender recommender,
            UserFactors user,
            List<KeyValuePair<string, UserFactors>> users)
        {
            if (recommender == null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (users == null)
            {
                throw new ArgumentNullException(nameof(users));
            }

            return recommender.RankUsers(user, users, RecommenderResultsBuilderFactory.Instance);
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
