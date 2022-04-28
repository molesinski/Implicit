using System;
using System.Collections.Generic;

namespace Implicit
{
    public sealed class RecommenderResultBuilderFactory : IResultBuilderFactory<RecommenderResult>
    {
        private RecommenderResultBuilderFactory()
        {
        }

        public static IResultBuilderFactory<RecommenderResult> Instance { get; } = new RecommenderResultBuilderFactory();

        public RecommenderResult CreateEmpty()
        {
            return new RecommenderResult(Array.Empty<KeyValuePair<string, double>>(), count: 0);
        }

        public IResultBuilder<RecommenderResult> CreateBuilder(int maximumCapacity)
        {
            return new RecommenderResultBuilder(new KeyValuePair<string, double>[maximumCapacity]);
        }
    }
}
