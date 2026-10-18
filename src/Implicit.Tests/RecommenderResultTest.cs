using Xunit;

namespace Implicit.Tests
{
    [Collection(nameof(RecommenderResultPoolTestScope))]
    public class RecommenderResultTest
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CreateReturnsStorageWhenBuilderThrows(bool addItems)
        {
            List<KeyValuePair<string, float>>? failedStorage = null;
            var failure = new InvalidOperationException("Builder failed.");

            var actual = Assert.Throws<InvalidOperationException>(() =>
            {
                using var result = RecommenderResult.Create(storage =>
                {
                    failedStorage = storage;

                    if (addItems)
                    {
                        storage.Add(new("partial-first", 1f));
                        storage.Add(new("partial-second", 2f));
                    }

                    throw failure;
                });
            });

            Assert.Same(failure, actual);
            Assert.NotNull(failedStorage);
            Assert.Empty(failedStorage);

            var expected = new KeyValuePair<string, float>("complete", 3f);
            using var recovered = RecommenderResult.Create(storage =>
            {
                Assert.Same(failedStorage, storage);
                Assert.Empty(storage);
                storage.Add(expected);
            });
            using var concurrent = RecommenderResult.Create(storage =>
            {
                Assert.NotSame(failedStorage, storage);
                storage.Add(new("other", 4f));
            });

            Assert.Equal(expected, Assert.Single(recovered));
        }

        [Fact]
        public void CreateKeepsStorageLeasedUntilResultIsDisposed()
        {
            List<KeyValuePair<string, float>>? firstStorage = null;
            var expected = new KeyValuePair<string, float>("first", 1f);
            using var first = RecommenderResult.Create(storage =>
            {
                firstStorage = storage;
                storage.Add(expected);
            });
            using var second = RecommenderResult.Create(storage =>
            {
                Assert.NotSame(firstStorage, storage);
                storage.Add(new("second", 2f));
            });

            Assert.Equal(expected, Assert.Single(first));

            first.Dispose();

            using var reused = RecommenderResult.Create(storage =>
            {
                Assert.Same(firstStorage, storage);
                Assert.Empty(storage);
            });
        }
    }
}
