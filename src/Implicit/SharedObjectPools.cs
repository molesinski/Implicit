using Implicit.Utils;

namespace Implicit
{
    internal static class SharedObjectPools
    {
        public static ObjectPoolSlim<List<KeyValuePair<string, float>>> KeyValueLists { get; } = new(() => new(), x => x.Clear());
    }
}
