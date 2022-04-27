using System.Collections.Generic;

namespace Implicit
{
    internal class RecommenderResultsItemComparer : IComparer<RecommenderResultsItem>
    {
        public static IComparer<RecommenderResultsItem> Instance { get; } = new RecommenderResultsItemComparer();

        public int Compare(RecommenderResultsItem x, RecommenderResultsItem y)
        {
            return y.Score.CompareTo(x.Score);
        }
    }
}
