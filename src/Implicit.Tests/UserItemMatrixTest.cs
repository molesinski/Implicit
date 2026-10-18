using Xunit;

namespace Implicit.Tests
{
    public class UserItemMatrixTest
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void BuildPreservesValuesAndMappings(bool fromDictionary)
        {
            var records = new[]
            {
                new UserItemValue("zoe", "book-c", 2.5f),
                new UserItemValue("amy", "book-a", -3f),
                new UserItemValue("zoe", "book-b", -1f),
                new UserItemValue("amy", "book-c", 4f),
            };
            var userItems = new Dictionary<string, Dictionary<string, float>>
            {
                ["zoe"] = new Dictionary<string, float> { ["book-c"] = 2.5f, ["book-b"] = -1f },
                ["amy"] = new Dictionary<string, float> { ["book-a"] = -3f, ["book-c"] = 4f },
            };

            var result = fromDictionary ? UserItemMatrix.Build(userItems) : UserItemMatrix.Build(records);

            Assert.Equal(2, result.Users.Count);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(2, result.Matrix.RowCount);
            Assert.Equal(3, result.Matrix.ColumnCount);

            // Compare by ID: grouping records into dictionaries can change the order of columns.
            var zoe = result.Users["zoe"];
            var amy = result.Users["amy"];
            var a = result.Items["book-a"];
            var b = result.Items["book-b"];
            var c = result.Items["book-c"];
            Assert.Equal(0f, result.Matrix[zoe, a]);
            Assert.Equal(-1f, result.Matrix[zoe, b]);
            Assert.Equal(2.5f, result.Matrix[zoe, c]);
            Assert.Equal(-3f, result.Matrix[amy, a]);
            Assert.Equal(0f, result.Matrix[amy, b]);
            Assert.Equal(4f, result.Matrix[amy, c]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void BuildSkipsZeroOnlyUsersAndItems(bool fromDictionary)
        {
            var records = new[]
            {
                new UserItemValue("zero-user", "zero-item", 0f),
                new UserItemValue("user", "zero-item", 0f),
                new UserItemValue("user", "item", 2f),
            };
            var userItems = new Dictionary<string, Dictionary<string, float>>
            {
                ["zero-user"] = new Dictionary<string, float> { ["zero-item"] = 0f },
                ["empty-user"] = new Dictionary<string, float>(),
                ["user"] = new Dictionary<string, float> { ["zero-item"] = 0f, ["item"] = 2f },
            };

            var result = fromDictionary ? UserItemMatrix.Build(userItems) : UserItemMatrix.Build(records);

            Assert.Equal("user", Assert.Single(result.Users).Key);
            Assert.Equal("item", Assert.Single(result.Items).Key);
            Assert.Equal(1, result.Matrix.RowCount);
            Assert.Equal(1, result.Matrix.ColumnCount);
            Assert.Equal(2f, result.Matrix[result.Users["user"], result.Items["item"]]);
        }

        [Fact]
        public void BuildAggregatesRepeatedPairs()
        {
            var records = new[]
            {
                new UserItemValue("alice", "book", 2f),
                new UserItemValue("alice", "other", -1f),
                new UserItemValue("bob", "book", 4f),
                new UserItemValue("alice", "book", 3f),
                new UserItemValue("alice", "other", -0.5f),
                new UserItemValue("bob", "book", -1f),
            };

            var result = UserItemMatrix.Build(records);

            Assert.Equal(2, result.Users.Count);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(2, result.Matrix.RowCount);
            Assert.Equal(2, result.Matrix.ColumnCount);
            Assert.Equal(5f, result.Matrix[result.Users["alice"], result.Items["book"]]);
            Assert.Equal(-1.5f, result.Matrix[result.Users["alice"], result.Items["other"]]);
            Assert.Equal(3f, result.Matrix[result.Users["bob"], result.Items["book"]]);
            Assert.Equal(0f, result.Matrix[result.Users["bob"], result.Items["other"]]);
        }

        [Fact]
        public void BuildRetainsCancelledIdsOnlyWhenReadingRecords()
        {
            var records = new[]
            {
                new UserItemValue("cancelled-user", "cancelled-item", 2f),
                new UserItemValue("active-user", "active-item", 1f),
                new UserItemValue("cancelled-user", "cancelled-item", -2f),
            };

            var direct = UserItemMatrix.Build(records);
            var aggregated = UserItemMatrix.Build(UserItemValueExtensions.ToUserItems(records));

            Assert.Equal(2, direct.Users.Count);
            Assert.Equal(2, direct.Items.Count);
            Assert.Equal(2, direct.Matrix.RowCount);
            Assert.Equal(2, direct.Matrix.ColumnCount);
            Assert.All(direct.Matrix.Row(direct.Users["cancelled-user"]), value => Assert.Equal(0f, value));
            Assert.All(direct.Matrix.Column(direct.Items["cancelled-item"]), value => Assert.Equal(0f, value));
            Assert.Equal(1f, direct.Matrix[direct.Users["active-user"], direct.Items["active-item"]]);

            Assert.Equal("active-user", Assert.Single(aggregated.Users).Key);
            Assert.Equal("active-item", Assert.Single(aggregated.Items).Key);
            Assert.Equal(1, aggregated.Matrix.RowCount);
            Assert.Equal(1, aggregated.Matrix.ColumnCount);
            Assert.Equal(1f, aggregated.Matrix[aggregated.Users["active-user"], aggregated.Items["active-item"]]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void BuildEmptyInput(bool fromDictionary)
        {
            var result = fromDictionary
                ? UserItemMatrix.Build(new Dictionary<string, Dictionary<string, float>>())
                : UserItemMatrix.Build(Array.Empty<UserItemValue>());

            Assert.Empty(result.Users);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.Matrix.RowCount);
            Assert.Equal(0, result.Matrix.ColumnCount);
        }
    }
}
