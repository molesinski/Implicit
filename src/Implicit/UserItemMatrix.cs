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

            foreach (var user in userItems)
            {
                foreach (var item in user.Value)
                {
                    if (item.Value == 0)
                    {
                        continue;
                    }

                    if (!users.TryGetValue(user.Key, out var u))
                    {
                        users.Add(user.Key, u = nextUserIndex++);
                    }

                    if (!items.TryGetValue(item.Key, out var i))
                    {
                        items.Add(item.Key, i = nextItemIndex++);
                    }

                    if (values.TryGetValue((u, i), out var value))
                    {
                        values[(u, i)] = value + item.Value;
                    }
                    else
                    {
                        values.Add((u, i), item.Value);
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

        public static UserItemMatrix Build(IEnumerable<UserItemValue> userItemValues)
        {
            if (userItemValues is null)
            {
                throw new ArgumentNullException(nameof(userItemValues));
            }

            var users = new Dictionary<string, int>();
            var items = new Dictionary<string, int>();
            var values = new Dictionary<(int, int), float>();
            var nextUserIndex = 0;
            var nextItemIndex = 0;

            foreach (var userItemValue in userItemValues)
            {
                if (userItemValue.Value == 0)
                {
                    continue;
                }

                if (!users.TryGetValue(userItemValue.UserId, out var u))
                {
                    users.Add(userItemValue.UserId, u = nextUserIndex++);
                }

                if (!items.TryGetValue(userItemValue.ItemId, out var i))
                {
                    items.Add(userItemValue.ItemId, i = nextItemIndex++);
                }

                if (values.TryGetValue((u, i), out var currentValue))
                {
                    values[(u, i)] = currentValue + userItemValue.Value;
                }
                else
                {
                    values.Add((u, i), userItemValue.Value);
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
