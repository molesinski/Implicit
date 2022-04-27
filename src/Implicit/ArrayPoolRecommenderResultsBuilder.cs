#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER

using System;
using System.Buffers;

namespace Implicit
{
    public class ArrayPoolRecommenderResultsBuilder : IResultsBuilder<ArrayPoolRecommenderResults>
    {
        private readonly ArrayPool<RecommenderResultsItem> pool;
        private readonly RecommenderResultsItem[] storage;
        private readonly int length;
        private int count;

        internal ArrayPoolRecommenderResultsBuilder(ArrayPool<RecommenderResultsItem> pool, RecommenderResultsItem[] storage)
        {
            this.pool = pool;
            this.storage = storage;
            this.length = storage.Length;
            this.count = 0;
        }

        public void Add(string key, double score)
        {
            if (this.count == this.length)
            {
                throw new InvalidOperationException("Result builder maximum capacity has been exceeded.");
            }

            this.storage[this.count] = new RecommenderResultsItem(key, score);
            this.count++;
        }

        public ArrayPoolRecommenderResults ToResults()
        {
            Array.Sort(this.storage, 0, this.count, RecommenderResultsItemComparer.Instance);

            return new ArrayPoolRecommenderResults(this.pool, this.storage, this.count);
        }
    }
}

#endif
