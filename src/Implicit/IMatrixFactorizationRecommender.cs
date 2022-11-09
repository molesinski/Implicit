namespace Implicit
{
    public interface IMatrixFactorizationRecommender : IRecommender
    {
        TResult RecommendUser<TResult>(UserFeatures user, IResultBuilderFactory<TResult> resultBuilderFactory);

        TResult RankUsers<TResult>(string userId, IEnumerable<KeyValuePair<string, UserFeatures>> users, IResultBuilderFactory<TResult> resultBuilderFactory);

        TResult RankUsers<TResult>(UserFeatures user, IEnumerable<KeyValuePair<string, UserFeatures>> users, IResultBuilderFactory<TResult> resultBuilderFactory);

        UserFeatures? GetUserFeatures(string userId);

        UserFeatures ComputeUserFeatures(Dictionary<string, double> items);
    }
}
