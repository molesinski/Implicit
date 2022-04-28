using System;

namespace Implicit
{
    public class RecommenderResultBuilderFactory : IResultBuilderFactory<RecommenderResult>
    {
        private RecommenderResultBuilderFactory()
        {
        }

        public static IResultBuilderFactory<RecommenderResult> Instance { get; } = new RecommenderResultBuilderFactory();

        public RecommenderResult CreateEmpty()
        {
            return new RecommenderResult(Array.Empty<RecommenderResultItem>(), count: 0);
        }

        public IResultBuilder<RecommenderResult> CreateBuilder(int maximumCapacity)
        {
            return new RecommenderResultBuilder(new RecommenderResultItem[maximumCapacity]);
        }
    }
}
