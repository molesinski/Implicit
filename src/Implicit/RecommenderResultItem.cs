using System.Collections.Generic;

namespace Implicit
{
    internal readonly struct RecommenderResultItem
    {
        public RecommenderResultItem(string key, double score)
        {
            this.Key = key;
            this.Score = score;
        }

        public string Key { get; }

        public double Score { get; }

        internal class DescendingScoreComparer : IComparer<RecommenderResultItem>
        {
            public static IComparer<RecommenderResultItem> Instance { get; } = new DescendingScoreComparer();

            public int Compare(RecommenderResultItem x, RecommenderResultItem y)
            {
                return y.Score.CompareTo(x.Score);
            }
        }
    }
}
