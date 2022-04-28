#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER

using System;
using System.Buffers;

namespace Implicit
{
    public class ArrayPoolRecommenderResultBuilderFactory : IResultBuilderFactory<ArrayPoolRecommenderResult>
    {
        private readonly ArrayPool<RecommenderResultItem> pool;

        public ArrayPoolRecommenderResultBuilderFactory()
            : this(ArrayPool<RecommenderResultItem>.Create())
        {
        }

        public ArrayPoolRecommenderResultBuilderFactory(int maxArrayLength, int maxArraysPerBucket)
            : this(ArrayPool<RecommenderResultItem>.Create(maxArrayLength, maxArraysPerBucket))
        {
        }

        private ArrayPoolRecommenderResultBuilderFactory(ArrayPool<RecommenderResultItem> pool)
        {
            if (pool is null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            this.pool = pool;
        }

        public static IResultBuilderFactory<ArrayPoolRecommenderResult> Shared { get; } = new ArrayPoolRecommenderResultBuilderFactory(ArrayPool<RecommenderResultItem>.Shared);

        public ArrayPoolRecommenderResult CreateEmpty()
        {
            return new ArrayPoolRecommenderResult(Array.Empty<RecommenderResultItem>(), count: 0, pool: null);
        }

        public IResultBuilder<ArrayPoolRecommenderResult> CreateBuilder(int maximumCapacity)
        {
            var storage = this.pool.Rent(minimumLength: maximumCapacity);

            return new ArrayPoolRecommenderResultBuilder(storage, this.pool);
        }
    }
}

#endif
