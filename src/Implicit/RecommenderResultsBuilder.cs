using System;

namespace Implicit
{
    public class RecommenderResultsBuilder : IResultsBuilder<RecommenderResults>
    {
        private readonly RecommenderResultsItem[] storage;
        private readonly int length;
        private int count;

        internal RecommenderResultsBuilder(RecommenderResultsItem[] storage)
        {
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

        public RecommenderResults ToResults()
        {
            Array.Sort(this.storage, 0, this.count, RecommenderResultsItemComparer.Instance);

            return new RecommenderResults(this.storage, this.count);
        }
    }
}
