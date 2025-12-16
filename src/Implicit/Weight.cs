namespace Implicit
{
    public static class Weight
    {
        public static Dictionary<string, Dictionary<string, double>> TFIDF(Dictionary<string, Dictionary<string, double>> data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var n = data.Keys.Count;
            var idf = data
                .SelectMany(x => x.Value)
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => Math.Log(n) - Math.Log(1 + x.Count()));

            var weighted = data
                .SelectMany(x => x.Value, (x, y) => new { UserId = x.Key, ItemId = y.Key, Confidence = y.Value })
                .Select(x => new { x.UserId, x.ItemId, Confidence = Math.Sqrt(x.Confidence) * idf[x.ItemId] })
                .GroupBy(x => x.UserId)
                .ToDictionary(x => x.Key, x => x.ToDictionary(p => p.ItemId, p => p.Confidence));

            return weighted;
        }

        public static IEnumerable<DataRow> TFIDF(IEnumerable<DataRow> data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var users = new HashSet<string>();
            var items = new Dictionary<string, int>();

            foreach (var userItem in data)
            {
                users.Add(userItem.UserId);

                items.TryGetValue(userItem.ItemId, out var count);
                items[userItem.ItemId] = count + 1;
            }

            var n = users.Count;

            return data
                .Select(
                    userItem =>
                    {
                        var idf = Math.Log(n) - Math.Log(1 + items[userItem.ItemId]);
                        var confidence = Math.Sqrt(userItem.Confidence) * idf;

                        return new DataRow(userItem.UserId, userItem.ItemId, confidence);
                    });
        }

        public static Dictionary<string, Dictionary<string, double>> BM25(Dictionary<string, Dictionary<string, double>> data, int k1 = 100, double b = 0.8)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var n = data.Keys.Count;
            var idf = data
                .SelectMany(x => x.Value)
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => Math.Log(n) - Math.Log(1 + x.Count()));

            var averageLength = data.Average(x => x.Value.Count);
            var lengthNorm = data
                .ToDictionary(x => x.Key, x => 1.0 - b + (b * x.Value.Count / averageLength));

            var weighted = data
                .SelectMany(x => x.Value, (x, y) => new { UserId = x.Key, ItemId = y.Key, Confidence = y.Value })
                .Select(x => new { x.UserId, x.ItemId, Confidence = x.Confidence * (k1 + 1.0) / ((k1 * lengthNorm[x.UserId]) + x.Confidence) * idf[x.ItemId] })
                .GroupBy(x => x.UserId)
                .ToDictionary(x => x.Key, x => x.ToDictionary(p => p.ItemId, p => p.Confidence));

            return weighted;
        }

        public static IEnumerable<DataRow> BM25(IEnumerable<DataRow> data, int k1 = 100, double b = 0.8)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var users = new Dictionary<string, int>();
            var items = new Dictionary<string, int>();

            foreach (var userItem in data)
            {
                users.TryGetValue(userItem.UserId, out var count1);
                users[userItem.UserId] = count1 + 1;

                items.TryGetValue(userItem.ItemId, out var count2);
                items[userItem.ItemId] = count2 + 1;
            }

            var n = users.Count;
            var averageLength = users.Average(x => x.Value);

            return data
                .Select(
                    userItem =>
                    {
                        var idf = Math.Log(n) - Math.Log(1 + items[userItem.ItemId]);
                        var lengthNorm = 1.0 - b + (b * users[userItem.UserId] / averageLength);
                        var confidence = userItem.Confidence * (k1 + 1.0) / ((k1 * lengthNorm) + userItem.Confidence) * idf;

                        return new DataRow(userItem.UserId, userItem.ItemId, confidence);
                    });
        }
    }
}
