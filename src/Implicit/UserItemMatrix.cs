using MathNet.Numerics.LinearAlgebra;

namespace Implicit
{
    public sealed class UserItemMatrix
    {
        private UserItemMatrix(
            Dictionary<string, int> users,
            Dictionary<string, int> items,
            Matrix<float> matrix)
        {
            this.Users = users;
            this.Items = items;
            this.Matrix = matrix;
        }

        public Dictionary<string, int> Users { get; }

        public Dictionary<string, int> Items { get; }

        public Matrix<float> Matrix { get; }

        public static UserItemMatrix Build(Dictionary<string, Dictionary<string, float>> userItems)
        {
            if (userItems is null)
            {
                throw new ArgumentNullException(nameof(userItems));
            }

            var users = new Dictionary<string, int>();
            var items = new Dictionary<string, int>();
            var values = new Dictionary<(int, int), float>();
            var nextUserIndex = 0;
            var nextItemIndex = 0;

            foreach (var userPair in userItems)
            {
                foreach (var itemPair in userPair.Value)
                {
                    if (!(itemPair.Value > 0))
                    {
                        continue;
                    }

                    if (!users.TryGetValue(userPair.Key, out var u))
                    {
                        users.Add(userPair.Key, u = nextUserIndex++);
                    }

                    if (!items.TryGetValue(itemPair.Key, out var i))
                    {
                        items.Add(itemPair.Key, i = nextItemIndex++);
                    }

                    if (values.TryGetValue((u, i), out var value))
                    {
                        values[(u, i)] = value + itemPair.Value;
                    }
                    else
                    {
                        values.Add((u, i), itemPair.Value);
                    }
                }
            }

            var matrix = Matrix<float>.Build.SparseOfIndexed(users.Count, items.Count, values.Select(x => (x.Key.Item1, x.Key.Item2, x.Value)));

            return
                new UserItemMatrix(
                    users,
                    items,
                    matrix);
        }

        public static UserItemMatrix Build(IEnumerable<UserItemValue> userItems)
        {
            if (userItems is null)
            {
                throw new ArgumentNullException(nameof(userItems));
            }

            var users = new Dictionary<string, int>();
            var items = new Dictionary<string, int>();
            var values = new Dictionary<(int, int), float>();
            var nextUserIndex = 0;
            var nextItemIndex = 0;

            foreach (var userItem in userItems)
            {
                if (!(userItem.Value > 0))
                {
                    continue;
                }

                if (!users.TryGetValue(userItem.UserId, out var u))
                {
                    users.Add(userItem.UserId, u = nextUserIndex++);
                }

                if (!items.TryGetValue(userItem.ItemId, out var i))
                {
                    items.Add(userItem.ItemId, i = nextItemIndex++);
                }

                if (values.TryGetValue((u, i), out var currentValue))
                {
                    values[(u, i)] = currentValue + userItem.Value;
                }
                else
                {
                    values.Add((u, i), userItem.Value);
                }
            }

            var matrix = Matrix<float>.Build.SparseOfIndexed(users.Count, items.Count, values.Select(x => (x.Key.Item1, x.Key.Item2, x.Value)));

            return
                new UserItemMatrix(
                    users,
                    items,
                    matrix);
        }
    }
}
