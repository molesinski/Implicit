namespace Implicit
{
    public interface IRecommender
    {
        RecommenderResult RecommendUser(string userId);

        RecommenderResult RecommendItem(string itemId);
    }
}
