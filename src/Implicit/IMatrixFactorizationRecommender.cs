using System.Collections.Generic;

namespace Implicit
{
    public interface IMatrixFactorizationRecommender : IRecommender
    {
        RecommenderResults RecommendUser(UserFactors user);

        RecommenderResults RankUsers(string userId, IEnumerable<KeyValuePair<string, UserFactors>> users);

        RecommenderResults RankUsers(UserFactors user, IEnumerable<KeyValuePair<string, UserFactors>> users);

        UserFactors? GetUserFactors(string userId);

        UserFactors ComputeUserFactors(Dictionary<string, double> items);
    }
}
