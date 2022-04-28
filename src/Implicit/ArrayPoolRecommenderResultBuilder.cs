#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER

using System;
using System.Buffers;

namespace Implicit
{
    public class ArrayPoolRecommenderResultBuilder : IResultBuilder<ArrayPoolRecommenderResult>
    {
        private readonly RecommenderResultItem[] storage;
        private readonly int length;
        private readonly ArrayPool<RecommenderResultItem> pool;
        private int count;

        internal ArrayPoolRecommenderResultBuilder(RecommenderResultItem[] storage, ArrayPool<RecommenderResultItem> pool)
        {
            this.storage = storage;
            this.length = storage.Length;
            this.pool = pool;
            this.count = 0;
        }

        public void Append(string key, double score)
        {
            if (this.count == this.length)
            {
                throw new InvalidOperationException("Result builder maximum capacity has been exceeded.");
            }

            this.storage[this.count] = new RecommenderResultItem(key, score);
            this.count++;
        }

        public ArrayPoolRecommenderResult ToResult()
        {
            Array.Sort(this.storage, 0, this.count, RecommenderResultItem.DescendingScoreComparer.Instance);

            return new ArrayPoolRecommenderResult(this.storage, this.count, this.pool);
        }
    }
}

#endif
