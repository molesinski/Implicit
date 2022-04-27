#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER

using System;
using System.Buffers;

namespace Implicit
{
    public class ArrayPoolRecommenderResultsBuilderFactory : IResultsBuilderFactory<ArrayPoolRecommenderResults>
    {
        private readonly ArrayPool<RecommenderResultsItem> pool;

        public ArrayPoolRecommenderResultsBuilderFactory()
            : this(ArrayPool<RecommenderResultsItem>.Create())
        {
        }

        public ArrayPoolRecommenderResultsBuilderFactory(int maxArrayLength, int maxArraysPerBucket)
            : this(ArrayPool<RecommenderResultsItem>.Create(maxArrayLength, maxArraysPerBucket))
        {
        }

        private ArrayPoolRecommenderResultsBuilderFactory(ArrayPool<RecommenderResultsItem> pool)
        {
            if (pool is null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            this.pool = pool;
        }

        public static IResultsBuilderFactory<ArrayPoolRecommenderResults> Shared { get; } = new ArrayPoolRecommenderResultsBuilderFactory(ArrayPool<RecommenderResultsItem>.Shared);

        public ArrayPoolRecommenderResults CreateEmpty()
        {
            return new ArrayPoolRecommenderResults(pool: null, Array.Empty<RecommenderResultsItem>(), count: 0);
        }

        public IResultsBuilder<ArrayPoolRecommenderResults> CreateBuilder(int maximumCapacity)
        {
            var storage = this.pool.Rent(minimumLength: maximumCapacity);

            return new ArrayPoolRecommenderResultsBuilder(this.pool, storage);
        }
    }
}

#endif
