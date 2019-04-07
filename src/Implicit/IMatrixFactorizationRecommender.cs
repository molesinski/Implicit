using System.Collections.Generic;

namespace Implicit
{
    public interface IMatrixFactorizationRecommender : IRecommender
    {
        IEnumerable<ItemResult> RecommendUser(UserFactors user);

        IEnumerable<TKey> RankUsers<TKey>(string userId, IEnumerable<KeyValuePair<TKey, UserFactors>> users);

        IEnumerable<TKey> RankUsers<TKey>(UserFactors user, IEnumerable<KeyValuePair<TKey, UserFactors>> users);

        UserFactors GetUserFactors(string userId);

        UserFactors ComputeUserFactors(Dictionary<string, double> items);
    }
}
