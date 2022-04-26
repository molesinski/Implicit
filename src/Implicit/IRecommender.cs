namespace Implicit
{
    public interface IRecommender
    {
        RecommenderResults RecommendUser(string userId);

        RecommenderResults RecommendItem(string itemId);
    }
}
