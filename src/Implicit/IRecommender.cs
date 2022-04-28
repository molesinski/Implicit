namespace Implicit
{
    public interface IRecommender
    {
        TResult RecommendUser<TResult>(string userId, IResultBuilderFactory<TResult> resultBuilderFactory);

        TResult RecommendItem<TResult>(string itemId, IResultBuilderFactory<TResult> resultBuilderFactory);
    }
}
