namespace Implicit
{
    public class RecommenderResultsBuilderFactory : IResultsBuilderFactory<RecommenderResults>
    {
        private RecommenderResultsBuilderFactory()
        {
        }

        public static IResultsBuilderFactory<RecommenderResults> Instance { get; } = new RecommenderResultsBuilderFactory();

        public RecommenderResults CreateEmpty()
        {
            return RecommenderResults.Empty;
        }

        public IResultsBuilder<RecommenderResults> CreateBuilder(int maximumCapacity)
        {
            return new RecommenderResultsBuilder(new RecommenderResultsItem[maximumCapacity]);
        }
    }
}
