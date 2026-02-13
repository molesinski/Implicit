namespace Implicit
{
    public sealed class DataMatrix
    {
        private DataMatrix(
            Dictionary<string, int> userMap,
            Dictionary<string, int> itemMap,
            Dictionary<int, Dictionary<int, float>> cui,
            Dictionary<int, Dictionary<int, float>> ciu)
        {
            this.UserMap = userMap;
            this.ItemMap = itemMap;
            this.Cui = cui;
            this.Ciu = ciu;
        }

        public int UserCount
        {
            get
            {
                return this.UserMap.Count;
            }
        }

        public int ItemCount
        {
            get
            {
                return this.ItemMap.Count;
            }
        }

        public float FillFactor
        {
            get
            {
                return 1f * this.Cui.Values.Sum(x => x.Values.Count) / (this.UserMap.Count * this.ItemMap.Count);
            }
        }

        internal Dictionary<string, int> UserMap { get; }

        internal Dictionary<string, int> ItemMap { get; }

        internal Dictionary<int, Dictionary<int, float>> Cui { get; }

        internal Dictionary<int, Dictionary<int, float>> Ciu { get; }

        public static DataMatrix Build(Dictionary<string, Dictionary<string, float>> data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var userMap = new Dictionary<string, int>();
            var itemMap = new Dictionary<string, int>();
            var cui = new Dictionary<int, Dictionary<int, float>>();
            var ciu = new Dictionary<int, Dictionary<int, float>>();
            var nextUserIndex = 0;
            var nextItemIndex = 0;

            foreach (var userPair in data)
            {
                foreach (var itemPair in userPair.Value)
                {
                    var userId = userPair.Key;
                    var itemId = itemPair.Key;
                    var confidence = itemPair.Value;

                    if (!userMap.TryGetValue(userId, out var u))
                    {
                        userMap.Add(userId, u = nextUserIndex++);
                    }

                    if (!itemMap.TryGetValue(itemId, out var i))
                    {
                        itemMap.Add(itemId, i = nextItemIndex++);
                    }

                    if (!cui.TryGetValue(u, out var user))
                    {
                        cui.Add(u, user = new Dictionary<int, float>());
                    }

                    if (!ciu.TryGetValue(i, out var item))
                    {
                        ciu.Add(i, item = new Dictionary<int, float>());
                    }

                    user.TryGetValue(i, out var userConfidence);
                    user[i] = userConfidence + confidence;

                    item.TryGetValue(u, out var itemConfidence);
                    item[u] = itemConfidence + confidence;
                }
            }

            return
                new DataMatrix(
                    userMap,
                    itemMap,
                    cui,
                    ciu);
        }

        public static DataMatrix Build(IEnumerable<DataRow> data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var userMap = new Dictionary<string, int>();
            var itemMap = new Dictionary<string, int>();
            var cui = new Dictionary<int, Dictionary<int, float>>();
            var ciu = new Dictionary<int, Dictionary<int, float>>();
            var nextUserIndex = 0;
            var nextItemIndex = 0;

            foreach (var row in data)
            {
                var userId = row.UserId;
                var itemId = row.ItemId;
                var confidence = row.Confidence;

                if (!userMap.TryGetValue(userId, out var u))
                {
                    userMap.Add(userId, u = nextUserIndex++);
                }

                if (!itemMap.TryGetValue(itemId, out var i))
                {
                    itemMap.Add(itemId, i = nextItemIndex++);
                }

                if (!cui.TryGetValue(u, out var user))
                {
                    cui.Add(u, user = new Dictionary<int, float>());
                }

                if (!ciu.TryGetValue(i, out var item))
                {
                    ciu.Add(i, item = new Dictionary<int, float>());
                }

                user.TryGetValue(i, out var userConfidence);
                user[i] = userConfidence + confidence;

                item.TryGetValue(u, out var itemConfidence);
                item[u] = itemConfidence + confidence;
            }

            return
                new DataMatrix(
                    userMap,
                    itemMap,
                    cui,
                    ciu);
        }
    }
}
