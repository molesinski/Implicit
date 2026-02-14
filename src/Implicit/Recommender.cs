namespace Implicit
{
    public abstract class Recommender
    {
        public abstract RecommenderResult Recommend(string userId);

        public abstract RecommenderResult SimilarUsers(string userId);

        public abstract RecommenderResult SimilarItems(string itemId);

        public abstract void Save(Stream stream);
    }
}
