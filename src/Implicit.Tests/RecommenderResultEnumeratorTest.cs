using System.Collections;
using Xunit;

namespace Implicit.Tests
{
    [Collection(nameof(RecommenderResultPoolTestScope))]
    public class RecommenderResultEnumeratorTest
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EnumeratorsRejectDisposedOwner(bool reuseStorage)
        {
            List<KeyValuePair<string, float>>? originalStorage = null;
            using var result = RecommenderResult.Create(storage =>
            {
                originalStorage = storage;
                storage.Add(new("first", 1f));
                storage.Add(new("second", 2f));
            });
            var enumerator = result.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            var copy = enumerator;
            using var generic = ((IEnumerable<KeyValuePair<string, float>>)result).GetEnumerator();
            var nonGeneric = ((IEnumerable)result).GetEnumerator();
            Assert.True(generic.MoveNext());
            Assert.True(nonGeneric.MoveNext());

            result.Dispose();

            using var replacement = reuseStorage
                ? RecommenderResult.Create(storage =>
                {
                    Assert.Same(originalStorage, storage);
                    storage.Add(new("replacement-first", 3f));
                    storage.Add(new("replacement-second", 4f));
                })
                : null;

            var error = Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
            Assert.Equal(typeof(RecommenderResult).FullName, error.ObjectName);
            Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
            Assert.Throws<ObjectDisposedException>(() => copy.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => copy.Current);
            Assert.Throws<ObjectDisposedException>(() => generic.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => generic.Current);
            Assert.Throws<ObjectDisposedException>(() => generic.Reset());
            Assert.Throws<ObjectDisposedException>(() => nonGeneric.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => nonGeneric.Current);
            Assert.Throws<ObjectDisposedException>(() => nonGeneric.Reset());

            // Enumerator cleanup must remain safe after the owner has been disposed.
            enumerator.Dispose();
            copy.Dispose();
            ((IDisposable)nonGeneric).Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EnumeratorRejectsDisposedOwnerBeforeStartOrAfterEnd(bool exhausted)
        {
            using var result = RecommenderResult.Create(storage => storage.Add(new("item", 1f)));
            var enumerator = result.GetEnumerator();

            if (exhausted)
            {
                Assert.True(enumerator.MoveNext());
                Assert.False(enumerator.MoveNext());
            }

            result.Dispose();

            Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
            Assert.Throws<ObjectDisposedException>(() => ((IEnumerator)enumerator).Reset());
        }

        [Fact]
        public void EmptyPooledResultEnumeratorRejectsDisposedOwner()
        {
            using var result = RecommenderResult.Create(_ => { });
            var enumerator = result.GetEnumerator();
            Assert.False(enumerator.MoveNext());

            result.Dispose();

            Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
            Assert.Throws<ObjectDisposedException>(() => ((IEnumerator)enumerator).Reset());
        }

        [Fact]
        public void SharedEmptyResultRemainsEnumerableAfterDisposal()
        {
            var enumerator = RecommenderResult.Empty.GetEnumerator();
            using var generic = ((IEnumerable<KeyValuePair<string, float>>)RecommenderResult.Empty).GetEnumerator();

            RecommenderResult.Empty.Dispose();

            Assert.False(enumerator.MoveNext());
            Assert.False(generic.MoveNext());
            generic.Reset();
            Assert.False(generic.MoveNext());
            Assert.Empty(RecommenderResult.Empty);
        }

        [Fact]
        public void EnumeratorSupportsIterationAndResetWhileOwnerIsAlive()
        {
            var first = new KeyValuePair<string, float>("first", 1f);
            var second = new KeyValuePair<string, float>("second", 2f);
            using var result = RecommenderResult.Create(storage =>
            {
                storage.Add(first);
                storage.Add(second);
            });
            using var enumerator = ((IEnumerable<KeyValuePair<string, float>>)result).GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(first, enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(second, enumerator.Current);
            Assert.False(enumerator.MoveNext());

            enumerator.Reset();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(first, enumerator.Current);
            Assert.Equal(first, ((IEnumerator)enumerator).Current);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DeferredEnumerationRejectsDisposedOwner(bool started)
        {
            using var result = RecommenderResult.Create(storage => storage.Add(new("item", 1f)));
            using var enumerator = result.Where(item => item.Value > 0f).GetEnumerator();

            if (started)
            {
                Assert.True(enumerator.MoveNext());
            }

            result.Dispose();

            Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
        }
    }
}
