namespace Implicit
{
    internal readonly struct RecommenderResultsItem
    {
        public RecommenderResultsItem(string key, double score)
        {
            this.Key = key;
            this.Score = score;
        }

        public string Key { get; }

        public double Score { get; }
    }
}
