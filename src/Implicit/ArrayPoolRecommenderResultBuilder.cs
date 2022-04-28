#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Implicit
{
    public sealed class ArrayPoolRecommenderResultBuilder : IResultBuilder<ArrayPoolRecommenderResult>
    {
        private readonly KeyValuePair<string, double>[] storage;
        private readonly int length;
        private readonly ArrayPool<KeyValuePair<string, double>> pool;
        private int count;

        internal ArrayPoolRecommenderResultBuilder(KeyValuePair<string, double>[] storage, ArrayPool<KeyValuePair<string, double>> pool)
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

            this.storage[this.count] = new KeyValuePair<string, double>(key, score);
            this.count++;
        }

        public ArrayPoolRecommenderResult ToResult()
        {
            Array.Sort(this.storage, 0, this.count, DescendingScoreComparer.Instance);

            return new ArrayPoolRecommenderResult(this.storage, this.count, this.pool);
        }

        private class DescendingScoreComparer : IComparer<KeyValuePair<string, double>>
        {
            public static IComparer<KeyValuePair<string, double>> Instance { get; } = new DescendingScoreComparer();

            public int Compare(KeyValuePair<string, double> x, KeyValuePair<string, double> y)
            {
                return y.Value.CompareTo(x.Value);
            }
        }
    }
}

#endif
