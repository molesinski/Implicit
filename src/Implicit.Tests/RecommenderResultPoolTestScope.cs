using Xunit;

namespace Implicit.Tests
{
    // Pool identity assertions must not race with other tests using the shared result pool.
    [CollectionDefinition(nameof(RecommenderResultPoolTestScope), DisableParallelization = true)]
    public sealed class RecommenderResultPoolTestScope
    {
    }
}
