using Implicit.Utils;
using Xunit;

namespace Implicit.Tests
{
    public class ObjectPoolSlimTest
    {
        [Theory]
        [InlineData(2)]
        [InlineData(4)]
        public async Task ConcurrentReturnsPreserveAllItems(int workers)
        {
            using var barrier = new Barrier(workers);
            var created = 0;
            var pool = new ObjectPoolSlim<List<int>>(
                () =>
                {
                    Interlocked.Increment(ref created);
                    return new List<int>();
                },
                item =>
                {
                    item.Clear();

                    // Release all returners together, while every item is still leased.
                    SignalAndWait(barrier);
                });
            var tasks = Enumerable.Range(0, workers).Select(worker => Task.Factory.StartNew(
                () =>
                {
                    for (var iteration = 0; iteration < 1000; iteration++)
                    {
                        using (var lease = pool.Lease())
                        {
                            Assert.Empty(lease.Instance);
                            lease.Instance.Add(worker);
                        }

                        // Finish every return before leasing the next batch.
                        SignalAndWait(barrier);
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default)).ToArray();

            await Task.WhenAll(tasks);

            // Both worker counts fit even in a single-processor pool, so no item should be lost.
            Assert.Equal(workers, created);
        }

        [Fact]
        public void ReturningToFullPoolPreservesRetainedItems()
        {
            var capacity = Environment.ProcessorCount * 5;
            var pool = new ObjectPoolSlim<List<int>>(() => new(), item => item.Clear());
            var retained = Enumerable.Range(0, capacity).Select(_ => pool.Lease()).ToArray();
            var expected = new HashSet<List<int>>(retained.Select(lease => lease.Instance));
            var overflow = pool.Lease();
            overflow.Instance.Add(1);

            foreach (var lease in retained)
            {
                lease.Dispose();
            }

            overflow.Dispose();

            Assert.Empty(overflow.Instance);
            var recovered = Enumerable.Range(0, capacity).Select(_ => pool.Lease()).ToArray();

            try
            {
                foreach (var lease in recovered)
                {
                    Assert.True(expected.Remove(lease.Instance));
                }

                Assert.Empty(expected);
            }
            finally
            {
                foreach (var lease in recovered)
                {
                    lease.Dispose();
                }
            }
        }

        private static void SignalAndWait(Barrier barrier)
        {
            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Pool worker synchronization timed out.");
            }
        }
    }
}
