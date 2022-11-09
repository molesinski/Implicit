namespace Implicit
{
    public sealed class RecommenderResultBuilder : IResultBuilder<RecommenderResult>
    {
        private readonly KeyValuePair<string, double>[] storage;
        private readonly int length;
        private int count;

        internal RecommenderResultBuilder(KeyValuePair<string, double>[] storage)
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

            this.storage[this.count] = new KeyValuePair<string, double>(key, score);
            this.count++;
        }

        public RecommenderResult ToResult()
        {
            Array.Sort(this.storage, 0, this.count, DescendingScoreComparer.Instance);

            return new RecommenderResult(this.storage, this.count);
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
