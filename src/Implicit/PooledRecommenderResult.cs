#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace Implicit
{
    public sealed class PooledRecommenderResult : IDisposable
    {
        private readonly KeyValuePair<string, double>[] storage;
        private readonly ArrayPool<KeyValuePair<string, double>>? pool;
        private bool disposed;

        internal PooledRecommenderResult(KeyValuePair<string, double>[] storage, int count, ArrayPool<KeyValuePair<string, double>>? pool)
        {
            this.pool = pool;
            this.storage = storage;

            this.IsEmpty = count == 0;
            this.Keys = new KeysCollection(storage, count);
        }

        public bool IsEmpty { get; }

        public KeysCollection Keys { get; }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.pool?.Return(this.storage);
                }

                this.disposed = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public sealed class KeysCollection : IEnumerable<string>
        {
            private readonly KeyValuePair<string, double>[] storage;
            private readonly int count;

            internal KeysCollection(KeyValuePair<string, double>[] storage, int count)
            {
                this.storage = storage;
                this.count = count;
            }

            public string this[int index]
            {
                get
                {
                    if (index < 0 || index >= this.count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    return this.storage[index].Key;
                }
            }

            public int Count
            {
                get
                {
                    return this.count;
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(this.storage, this.count);
            }

            IEnumerator<string> IEnumerable<string>.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            public struct Enumerator : IEnumerator<string>
            {
                private readonly KeyValuePair<string, double>[] storage;
                private readonly int count;
                private int index;
                private string? current;

                internal Enumerator(KeyValuePair<string, double>[] storage, int count)
                {
                    this.storage = storage;
                    this.count = count;
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
        }
    }
}

#endif
