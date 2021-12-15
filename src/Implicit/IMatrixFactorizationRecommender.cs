using System.Collections.Generic;

namespace Implicit
{
    public interface IMatrixFactorizationRecommender : IRecommender
    {
        List<ItemResult> RecommendUser(UserFactors user);

        List<TKey> RankUsers<TKey>(string userId, IEnumerable<KeyValuePair<TKey, UserFactors>> users);

        List<TKey> RankUsers<TKey>(UserFactors user, IEnumerable<KeyValuePair<TKey, UserFactors>> users);

        UserFactors GetUserFactors(string userId);

        UserFactors ComputeUserFactors(Dictionary<string, double> items);
    }
}
