using System;

namespace Implicit
{
    public class RecommenderResultBuilder : IResultBuilder<RecommenderResult>
    {
        private readonly RecommenderResultItem[] storage;
        private readonly int length;
        private int count;

        internal RecommenderResultBuilder(RecommenderResultItem[] storage)
        {
            this.storage = storage;
            this.length = storage.Length;
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

        public RecommenderResult ToResult()
        {
            Array.Sort(this.storage, 0, this.count, RecommenderResultItem.DescendingScoreComparer.Instance);

            return new RecommenderResult(this.storage, this.count);
        }
    }
}
