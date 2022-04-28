using System.Collections.Generic;

namespace Implicit
{
    public interface IMatrixFactorizationRecommender : IRecommender
    {
        TResult RecommendUser<TResult>(UserFactors user, IResultBuilderFactory<TResult> resultBuilderFactory);

        TResult RankUsers<TResult>(string userId, List<KeyValuePair<string, UserFactors>> users, IResultBuilderFactory<TResult> resultBuilderFactory);

        TResult RankUsers<TResult>(UserFactors user, List<KeyValuePair<string, UserFactors>> users, IResultBuilderFactory<TResult> resultBuilderFactory);

        UserFactors? GetUserFactors(string userId);

        UserFactors ComputeUserFactors(Dictionary<string, double> items);
    }
}
