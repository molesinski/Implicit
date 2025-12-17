using System.Collections;
using Implicit.Utils;

namespace Implicit
{
    public sealed class RecommenderResult : IReadOnlyList<KeyValuePair<string, double>>, IDisposable
    {
        private readonly ObjectPoolLease<List<KeyValuePair<string, double>>> storage;
        private bool isDisposed;

        internal RecommenderResult(ObjectPoolLease<List<KeyValuePair<string, double>>> storage)
        {
            this.storage = storage;
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

        public KeyValuePair<string, double> this[int index]
        {
            get
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                return this.storage.Instance[index];
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

        IEnumerator<KeyValuePair<string, double>> IEnumerable<KeyValuePair<string, double>>.GetEnumerator()
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

        public struct Enumerator : IEnumerator<KeyValuePair<string, double>>
        {
            private readonly List<KeyValuePair<string, double>> storage;
            private readonly int count;
            private int index;
            private KeyValuePair<string, double> current;

            internal Enumerator(List<KeyValuePair<string, double>> storage)
            {
                this.storage = storage;
                this.count = storage.Count;
                this.index = 0;
                this.current = default;
            }

            public KeyValuePair<string, double> Current
            {
                get
                {
                    if (this.index == 0 || this.index == this.count + 1)
                    {
                        throw new InvalidOperationException("Enumerator is currently positioned before first element or after last element.");
                    }

                    return this.current;
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
                    this.current = this.storage[this.index];
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
    }
}
