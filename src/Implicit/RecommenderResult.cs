using System.Collections;
using Implicit.Utils;

namespace Implicit
{
    public sealed class RecommenderResult : IReadOnlyList<string>, IDisposable
    {
        private readonly ObjectPoolLease<ListSlim<KeyValuePair<string, double>>> storage;
        private bool isDisposed;

        internal RecommenderResult(ObjectPoolLease<ListSlim<KeyValuePair<string, double>>> storage)
        {
            this.storage = storage;

            this.storage.Instance.Sort(DescendingScoreComparer.Instance);
        }

        public int Count
        {
            get
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                return this.storage.Instance.Count;
            }
        }

        public string this[int index]
        {
            get
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                if (index < 0 || index >= this.storage.Instance.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return this.storage.Instance[index].Key;
            }
        }

        public Enumerator GetEnumerator()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            return new Enumerator(this.storage.Instance);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                this.storage.Dispose();

                this.isDisposed = true;
            }
        }

        public struct Enumerator : IEnumerator<string>
        {
            private readonly ListSlim<KeyValuePair<string, double>> storage;
            private readonly int count;
            private int index;
            private string? current;

            internal Enumerator(ListSlim<KeyValuePair<string, double>> storage)
            {
                this.storage = storage;
                this.count = storage.Count;
                this.index = 0;
                this.current = default;
            }

            public string Current
            {
                get
                {
                    if (this.index == 0 || this.index == this.count + 1)
                    {
                        throw new InvalidOperationException("Enumerator is currently positioned before first element or after last element.");
                    }

                    return this.current!;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            public bool MoveNext()
            {
                if (this.index < this.count)
                {
                    this.current = this.storage[this.index].Key;
                    this.index++;

                    return true;
                }

                this.index = this.count + 1;
                this.current = default;

                return false;
            }

            public void Reset()
            {
                this.index = 0;
                this.current = default;
            }

            public void Dispose()
            {
            }
        }

        private sealed class DescendingScoreComparer : IComparer<KeyValuePair<string, double>>
        {
            public static IComparer<KeyValuePair<string, double>> Instance { get; } = new DescendingScoreComparer();

            public int Compare(KeyValuePair<string, double> x, KeyValuePair<string, double> y)
            {
                return y.Value.CompareTo(x.Value);
            }
        }
    }
}
