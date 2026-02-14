using System.Collections;
using Implicit.Utils;

namespace Implicit
{
    public sealed class RecommenderResult : IReadOnlyList<KeyValuePair<string, float>>, IDisposable
    {
        private static readonly List<KeyValuePair<string, float>> EmptyStorage = new();
        private static readonly ObjectPoolSlim<List<KeyValuePair<string, float>>> StoragePool = new(() => new(), x => x.Clear());

        private readonly ObjectPoolSlimLease<List<KeyValuePair<string, float>>>? storage;
        private bool disposed;

        private RecommenderResult()
        {
        }

        private RecommenderResult(ObjectPoolSlimLease<List<KeyValuePair<string, float>>> storage)
        {
            this.storage = storage;
        }

        public static RecommenderResult Empty { get; } = new();

        public int Count
        {
            get
            {
                if (this.storage is null)
                {
                    return EmptyStorage.Count;
                }

                if (this.disposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                return this.storage.Value.Instance.Count;
            }
        }

        public KeyValuePair<string, float> this[int index]
        {
            get
            {
                if (this.storage is null)
                {
                    return EmptyStorage[index];
                }

                if (this.disposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                return this.storage.Value.Instance[index];
            }
        }

        public static RecommenderResult Create(Action<List<KeyValuePair<string, float>>> builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var storage = StoragePool.Lease();

            builder(storage.Instance);

            return new RecommenderResult(storage);
        }

        public Enumerator GetEnumerator()
        {
            if (this.storage is null)
            {
                return new Enumerator(EmptyStorage);
            }

            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            return new Enumerator(this.storage.Value.Instance);
        }

        IEnumerator<KeyValuePair<string, float>> IEnumerable<KeyValuePair<string, float>>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.storage?.Dispose();
                this.disposed = true;
            }
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, float>>
        {
            private readonly List<KeyValuePair<string, float>> storage;
            private readonly int count;
            private int index;
            private KeyValuePair<string, float> current;

            internal Enumerator(List<KeyValuePair<string, float>> storage)
            {
                this.storage = storage;
                this.count = storage.Count;
                this.index = 0;
                this.current = default;
            }

            public readonly KeyValuePair<string, float> Current
            {
                get
                {
                    return this.current;
                }
            }

            readonly object IEnumerator.Current
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

            void IEnumerator.Reset()
            {
                this.index = 0;
                this.current = default;
            }

            public readonly void Dispose()
            {
            }
        }
    }
}
