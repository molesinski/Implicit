using System.Collections.Generic;

namespace Implicit
{
    public interface IRecommender
    {
        List<ItemResult> RecommendUser(string userId);

        List<ItemResult> RecommendItem(string itemId);
    }
}
