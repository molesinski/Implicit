using System.Collections.Generic;

namespace Implicit
{
    public interface IRecommender
    {
        IEnumerable<ItemResult> RecommendUser(string userId);

        IEnumerable<ItemResult> RecommendItem(string itemId);
    }
}
