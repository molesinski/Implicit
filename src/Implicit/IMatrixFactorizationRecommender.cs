namespace Implicit
{
    public interface IMatrixFactorizationRecommender : IRecommender
    {
        RecommenderResult RecommendUser(UserFeatures user);

        RecommenderResult RankUsers(string userId, IEnumerable<KeyValuePair<string, UserFeatures>> users);

        RecommenderResult RankUsers(UserFeatures user, IEnumerable<KeyValuePair<string, UserFeatures>> users);

        UserFeatures? GetUserFeatures(string userId);

        UserFeatures ComputeUserFeatures(Dictionary<string, double> items);
    }
}
