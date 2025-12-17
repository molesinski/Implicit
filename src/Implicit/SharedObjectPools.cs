using Implicit.Utils;

namespace Implicit
{
    internal static class SharedObjectPools
    {
        public static ObjectPool<List<KeyValuePair<string, double>>> KeyValueLists { get; } = new(() => new(), x => x.Clear());
    }
}
