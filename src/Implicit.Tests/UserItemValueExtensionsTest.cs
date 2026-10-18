using Xunit;

namespace Implicit.Tests
{
    public class UserItemValueExtensionsTest
    {
        [Fact]
        public void ToUserItemsAggregatesValuesAndPreservesZeros()
        {
            var records = new[]
            {
                new UserItemValue("alice", "book", 2f),
                new UserItemValue("bob", "book", 4f),
                new UserItemValue("alice", "other", -2f),
                new UserItemValue("alice", "book", 3f),
                new UserItemValue("alice", "other", 0.5f),
                new UserItemValue("bob", "book", -4f),
                new UserItemValue("zero-user", "zero-item", 0f),
            };

            var result = UserItemValueExtensions.ToUserItems(records);

            Assert.Equal(3, result.Count);
            Assert.Equal(2, result["alice"].Count);
            Assert.Equal(5f, result["alice"]["book"]);
            Assert.Equal(-1.5f, result["alice"]["other"]);
            Assert.Equal(new KeyValuePair<string, float>("book", 0f), Assert.Single(result["bob"]));
            Assert.Equal(new KeyValuePair<string, float>("zero-item", 0f), Assert.Single(result["zero-user"]));
        }
    }
}
