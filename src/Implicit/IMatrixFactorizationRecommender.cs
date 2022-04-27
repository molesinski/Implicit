using System.Collections.Generic;

namespace Implicit
{
    public interface IMatrixFactorizationRecommender : IRecommender
    {
        TResults RecommendUser<TResults>(UserFactors user, IResultsBuilderFactory<TResults> resultsBuilderFactory);

        TResults RankUsers<TResults>(string userId, List<KeyValuePair<string, UserFactors>> users, IResultsBuilderFactory<TResults> resultsBuilderFactory);

        TResults RankUsers<TResults>(UserFactors user, List<KeyValuePair<string, UserFactors>> users, IResultsBuilderFactory<TResults> resultsBuilderFactory);

        UserFactors? GetUserFactors(string userId);

        UserFactors ComputeUserFactors(Dictionary<string, double> items);
    }
}
