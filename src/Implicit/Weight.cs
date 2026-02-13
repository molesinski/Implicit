namespace Implicit
{
    public static class Weight
    {
        public static Dictionary<string, Dictionary<string, float>> TFIDF(Dictionary<string, Dictionary<string, float>> data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var n = data.Keys.Count;
            var idf = data
                .SelectMany(x => x.Value)
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => MathF.Log(n) - MathF.Log(1 + x.Count()));

            var weighted = data
                .SelectMany(x => x.Value, (x, y) => new { UserId = x.Key, ItemId = y.Key, Confidence = y.Value })
                .Select(x => new { x.UserId, x.ItemId, Confidence = MathF.Sqrt(x.Confidence) * idf[x.ItemId] })
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
                        var idf = MathF.Log(n) - MathF.Log(1 + items[userItem.ItemId]);
                        var confidence = MathF.Sqrt(userItem.Confidence) * idf;

                        return new DataRow(userItem.UserId, userItem.ItemId, confidence);
                    });
        }

        public static Dictionary<string, Dictionary<string, float>> BM25(Dictionary<string, Dictionary<string, float>> data, int k1 = 100, float b = 0.8f)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var n = data.Keys.Count;
            var idf = data
                .SelectMany(x => x.Value)
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => MathF.Log(n) - MathF.Log(1 + x.Count()));

            var averageLength = (float)data.Average(x => x.Value.Count);
            var lengthNorm = data
                .ToDictionary(x => x.Key, x => 1f - b + (b * x.Value.Count / averageLength));

            var weighted = data
                .SelectMany(x => x.Value, (x, y) => new { UserId = x.Key, ItemId = y.Key, Confidence = y.Value })
                .Select(x => new { x.UserId, x.ItemId, Confidence = x.Confidence * (k1 + 1f) / ((k1 * lengthNorm[x.UserId]) + x.Confidence) * idf[x.ItemId] })
                .GroupBy(x => x.UserId)
                .ToDictionary(x => x.Key, x => x.ToDictionary(p => p.ItemId, p => p.Confidence));

            return weighted;
        }

        public static IEnumerable<DataRow> BM25(IEnumerable<DataRow> data, int k1 = 100, float b = 0.8f)
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
            var averageLength = (float)users.Average(x => x.Value);

            return data
                .Select(
                    userItem =>
                    {
                        var idf = MathF.Log(n) - MathF.Log(1 + items[userItem.ItemId]);
                        var lengthNorm = 1f - b + (b * users[userItem.UserId] / averageLength);
                        var confidence = userItem.Confidence * (k1 + 1f) / ((k1 * lengthNorm) + userItem.Confidence) * idf;

                        return new DataRow(userItem.UserId, userItem.ItemId, confidence);
                    });
        }
    }
}
