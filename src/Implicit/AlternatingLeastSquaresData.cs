using System;
using System.Collections.Generic;

namespace Implicit
{
    using LabeledMatrix = Dictionary<string, Dictionary<string, double>>;
    using SparseMatrix = Dictionary<int, Dictionary<int, double>>;

    public class AlternatingLeastSquaresData
    {
        public AlternatingLeastSquaresData(Dictionary<string, int> userMap, Dictionary<string, int> itemMap, SparseMatrix cui, SparseMatrix ciu)
        {
            this.UserMap = userMap;
            this.ItemMap = itemMap;
            this.Cui = cui;
            this.Ciu = ciu;
        }

        public Dictionary<string, int> UserMap { get; }

        public Dictionary<string, int> ItemMap { get; }

        public SparseMatrix Cui { get; }

        public SparseMatrix Ciu { get; }

        public static AlternatingLeastSquaresData Load(LabeledMatrix data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var userMap = new Dictionary<string, int>();
            var itemMap = new Dictionary<string, int>();
            var Cui = new SparseMatrix();
            var Ciu = new SparseMatrix();
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
                new AlternatingLeastSquaresData(
                    userMap,
                    itemMap,
                    Cui,
                    Ciu);
        }

        public static AlternatingLeastSquaresData Load(IEnumerable<UserItem> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var userMap = new Dictionary<string, int>();
            var itemMap = new Dictionary<string, int>();
            var Cui = new SparseMatrix();
            var Ciu = new SparseMatrix();
            var nextUserIndex = 0;
            var nextItemIndex = 0;

            foreach (var userItem in data)
            {
                var userId = userItem.UserId;
                var itemId = userItem.ItemId;
                var confidence = userItem.Confidence;

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
                new AlternatingLeastSquaresData(
                    userMap,
                    itemMap,
                    Cui,
                    Ciu);
        }
    }
}
