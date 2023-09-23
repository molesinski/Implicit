using Implicit.Utils;

namespace Implicit
{
    internal static class SharedObjectPools
    {
        public static ObjectPool<ListSlim<KeyValuePair<string, double>>> KeyValueLists { get; } = new(() => new());
    }
}
