namespace Implicit
{
    public interface IRecommender
    {
        TResults RecommendUser<TResults>(string userId, IResultsBuilderFactory<TResults> resultsBuilderFactory);

        TResults RecommendItem<TResults>(string itemId, IResultsBuilderFactory<TResults> resultsBuilderFactory);
    }
}
