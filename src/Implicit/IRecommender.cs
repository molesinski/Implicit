using System.Collections.Generic;

namespace Implicit
{
    public interface IRecommender
    {
        IEnumerable<string> RecommendUser(string userId);

        IEnumerable<string> RecommendItem(string itemId);
    }
}
