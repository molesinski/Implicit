using System.Collections.Generic;

namespace Implicit
{
    public interface IMatrixFactorizationRecommender : IRecommender
    {
        TResult RecommendUser<TResult>(UserFeatures user, IResultBuilderFactory<TResult> resultBuilderFactory);

        TResult RankUsers<TResult>(string userId, List<KeyValuePair<string, UserFeatures>> users, IResultBuilderFactory<TResult> resultBuilderFactory);

        TResult RankUsers<TResult>(UserFeatures user, List<KeyValuePair<string, UserFeatures>> users, IResultBuilderFactory<TResult> resultBuilderFactory);

        UserFeatures? GetUserFeatures(string userId);

        UserFeatures ComputeUserFeatures(Dictionary<string, double> items);
    }
}
