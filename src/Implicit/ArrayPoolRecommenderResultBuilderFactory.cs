#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Implicit
{
    public sealed class ArrayPoolRecommenderResultBuilderFactory : IResultBuilderFactory<ArrayPoolRecommenderResult>
    {
        private readonly ArrayPool<KeyValuePair<string, double>> pool;

        public ArrayPoolRecommenderResultBuilderFactory()
            : this(ArrayPool<KeyValuePair<string, double>>.Create())
        {
        }

        public ArrayPoolRecommenderResultBuilderFactory(int maxArrayLength, int maxArraysPerBucket)
            : this(ArrayPool<KeyValuePair<string, double>>.Create(maxArrayLength, maxArraysPerBucket))
        {
        }

        private ArrayPoolRecommenderResultBuilderFactory(ArrayPool<KeyValuePair<string, double>> pool)
        {
            if (pool is null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            this.pool = pool;
        }

        public static IResultBuilderFactory<ArrayPoolRecommenderResult> Shared { get; } = new ArrayPoolRecommenderResultBuilderFactory(ArrayPool<KeyValuePair<string, double>>.Shared);

        public ArrayPoolRecommenderResult CreateEmpty()
        {
            return new ArrayPoolRecommenderResult(Array.Empty<KeyValuePair<string, double>>(), count: 0, pool: null);
        }

        public IResultBuilder<ArrayPoolRecommenderResult> CreateBuilder(int maximumCapacity)
        {
            var storage = this.pool.Rent(minimumLength: maximumCapacity);

            return new ArrayPoolRecommenderResultBuilder(storage, this.pool);
        }
    }
}

#endif
