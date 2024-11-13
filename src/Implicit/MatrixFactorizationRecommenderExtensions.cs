namespace Implicit
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

            var features = recommender.ComputeUserFeatures(items);

            if (features is null)
            {
                return new RecommenderResult(SharedObjectPools.KeyValueLists.Lease());
            }

            return recommender.RecommendUser(features);
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

            var features = recommender.ComputeUserFeatures(items);

            if (features is null)
            {
                return new RecommenderResult(SharedObjectPools.KeyValueLists.Lease());
            }

            return recommender.RecommendUser(features);
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

            var users = default(List<KeyValuePair<string, UserFeatures>>);

            foreach (var user in userItems)
            {
                var userFeatures = recommender.ComputeUserFeatures(user.Value);

                if (userFeatures is not null)
                {
                    users ??= new List<KeyValuePair<string, UserFeatures>>();
                    users.Add(new KeyValuePair<string, UserFeatures>(user.Key, userFeatures));
                }
            }

            if (users is null)
            {
                return new RecommenderResult(SharedObjectPools.KeyValueLists.Lease());
            }

            return recommender.RankUsers(userId, users);
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

            var users = default(List<KeyValuePair<string, UserFeatures>>);

            foreach (var user in userItems)
            {
                var userFeatures = recommender.ComputeUserFeatures(user.Value);

                if (userFeatures is not null)
                {
                    users ??= new List<KeyValuePair<string, UserFeatures>>();
                    users.Add(new KeyValuePair<string, UserFeatures>(user.Key, userFeatures));
                }
            }

            if (users is null)
            {
                return new RecommenderResult(SharedObjectPools.KeyValueLists.Lease());
            }

            return recommender.RankUsers(userId, users);
        }

        public static UserFeatures? ComputeUserFeatures(
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
