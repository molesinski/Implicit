#pragma warning disable SA1312 // Variable names should begin with lower-case letter

using System;
using System.Collections.Generic;
using System.Linq;

namespace Implicit
{
    public sealed class DataMatrix
    {
        private DataMatrix(Dictionary<string, int> userMap, Dictionary<string, int> itemMap, Dictionary<int, Dictionary<int, double>> cui, Dictionary<int, Dictionary<int, double>> ciu)
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

        public double FillFactor
        {
            get
            {
                return 1.0 * this.Cui.Values.Sum(o => o.Values.Count) / (this.UserMap.Count * this.ItemMap.Count);
            }
        }

        internal Dictionary<string, int> UserMap { get; }

        internal Dictionary<string, int> ItemMap { get; }

        internal Dictionary<int, Dictionary<int, double>> Cui { get; }

        internal Dictionary<int, Dictionary<int, double>> Ciu { get; }

        public static DataMatrix Load(Dictionary<string, Dictionary<string, double>> data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var userMap = new Dictionary<string, int>();
            var itemMap = new Dictionary<string, int>();
            var Cui = new Dictionary<int, Dictionary<int, double>>();
            var Ciu = new Dictionary<int, Dictionary<int, double>>();
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

                    if (!Cui.TryGetValue(u, out var user))
                    {
                        Cui.Add(u, user = new Dictionary<int, double>());
                    }

                    if (!Ciu.TryGetValue(i, out var item))
                    {
                        Ciu.Add(i, item = new Dictionary<int, double>());
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
                    Cui,
                    Ciu);
        }

        public static DataMatrix Load(IEnumerable<DataRow> data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var userMap = new Dictionary<string, int>();
            var itemMap = new Dictionary<string, int>();
            var Cui = new Dictionary<int, Dictionary<int, double>>();
            var Ciu = new Dictionary<int, Dictionary<int, double>>();
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

                if (!Cui.TryGetValue(u, out var user))
                {
                    Cui.Add(u, user = new Dictionary<int, double>());
                }

                if (!Ciu.TryGetValue(i, out var item))
                {
                    Ciu.Add(i, item = new Dictionary<int, double>());
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
                    Cui,
                    Ciu);
        }
    }
}
