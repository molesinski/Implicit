#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Implicit
{
    public sealed class PooledRecommenderResultBuilderFactory : IResultBuilderFactory<PooledRecommenderResult>
    {
        private readonly ArrayPool<KeyValuePair<string, double>> pool;

        public PooledRecommenderResultBuilderFactory()
            : this(ArrayPool<KeyValuePair<string, double>>.Create())
        {
        }

        public PooledRecommenderResultBuilderFactory(int maxArrayLength, int maxArraysPerBucket)
            : this(ArrayPool<KeyValuePair<string, double>>.Create(maxArrayLength, maxArraysPerBucket))
        {
        }

        private PooledRecommenderResultBuilderFactory(ArrayPool<KeyValuePair<string, double>> pool)
        {
            if (pool is null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            this.pool = pool;
        }

        public static IResultBuilderFactory<PooledRecommenderResult> Shared { get; } = new PooledRecommenderResultBuilderFactory(ArrayPool<KeyValuePair<string, double>>.Shared);

        public PooledRecommenderResult CreateEmpty()
        {
            return new PooledRecommenderResult(Array.Empty<KeyValuePair<string, double>>(), count: 0, pool: null);
        }

        public IResultBuilder<PooledRecommenderResult> CreateBuilder(int maximumCapacity)
        {
            var storage = this.pool.Rent(minimumLength: maximumCapacity);

            return new PooledRecommenderResultBuilder(storage, this.pool);
        }
    }
}

#endif
