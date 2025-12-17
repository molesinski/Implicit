namespace Implicit.Utils
{
    internal sealed class ObjectPool<TItem>
        where TItem : class
    {
        private const int DegreeOfParallelism = 5;

        private readonly Item[] items;
        private readonly Func<TItem> factory;
        private readonly Action<TItem> resetter;

        public ObjectPool(Func<TItem> factory, Action<TItem> resetter)
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (resetter is null)
            {
                throw new ArgumentNullException(nameof(resetter));
            }

            this.items = new Item[Environment.ProcessorCount * DegreeOfParallelism];
            this.factory = factory;
            this.resetter = resetter;
        }

        public ObjectPoolLease<TItem> Lease()
        {
            var instance = default(TItem);

            for (var i = 0; i < this.items.Length; i++)
            {
                instance = this.items[i].Value;

                if (instance is not null)
                {
                    if (instance == Interlocked.CompareExchange(ref this.items[i].Value!, null, instance))
                    {
                        return new ObjectPoolLease<TItem>(this, instance);
                    }
                }
            }

            return new ObjectPoolLease<TItem>(this, this.factory());
        }

        internal void Return(TItem instance)
        {
            this.resetter(instance);

            for (var i = 0; i < this.items.Length; i++)
            {
                if (this.items[i].Value is null)
                {
                    this.items[i].Value = instance;
                    break;
                }
            }
        }

        private struct Item
        {
            public TItem Value;
        }
    }
}
