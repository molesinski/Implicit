using System;
using System.Collections.Generic;
using System.Linq;

namespace Implicit
{
    using LabeledMatrix = Dictionary<string, Dictionary<string, double>>;

    public static class Weight
    {
        public static LabeledMatrix TFIDF(LabeledMatrix data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var n = data.Keys.Count;
            var idf = data
                .SelectMany(o => o.Value)
                .GroupBy(o => o.Key)
                .ToDictionary(o => o.Key, o => Math.Log(n) - Math.Log(1 + o.Count()));

            var weighted = data
                .SelectMany(o => o.Value, (o, p) => new { UserId = o.Key, ItemId = p.Key, Confidence = p.Value })
                .Select(o => new { o.UserId, o.ItemId, Confidence = Math.Sqrt(o.Confidence) * idf[o.ItemId] })
                .GroupBy(o => o.UserId)
                .ToDictionary(o => o.Key, o => o.ToDictionary(p => p.ItemId, p => p.Confidence));

            return weighted;
        }

        public static IEnumerable<UserItem> TFIDF(IEnumerable<UserItem> data)
        {
            if (data == null)
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

                        return new UserItem(userItem.UserId, userItem.ItemId, confidence);
                    });
        }

        public static LabeledMatrix BM25(LabeledMatrix data, int K1 = 100, double B = 0.5)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var n = data.Keys.Count;
            var idf = data
                .SelectMany(o => o.Value)
                .GroupBy(o => o.Key)
                .ToDictionary(o => o.Key, o => Math.Log(n) - Math.Log(1 + o.Count()));

            var averageLength = data.Average(o => o.Value.Count);
            var lengthNorm = data
                .ToDictionary(o => o.Key, o => (1.0 - B) + B * o.Value.Count / averageLength);

            var weighted = data
                .SelectMany(o => o.Value, (o, p) => new { UserId = o.Key, ItemId = p.Key, Confidence = p.Value })
                .Select(o => new { o.UserId, o.ItemId, Confidence = o.Confidence * (K1 + 1.0) / (K1 * lengthNorm[o.UserId] + o.Confidence) * idf[o.ItemId] })
                .GroupBy(o => o.UserId)
                .ToDictionary(o => o.Key, o => o.ToDictionary(p => p.ItemId, p => p.Confidence));

            return weighted;
        }

        public static IEnumerable<UserItem> BM25(IEnumerable<UserItem> data, int K1 = 100, double B = 0.5)
        {
            if (data == null)
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
            var averageLength = users.Average(o => o.Value);

            return data
                .Select(
                    userItem =>
                    {
                        var idf = Math.Log(n) - Math.Log(1 + items[userItem.ItemId]);
                        var lengthNorm = (1.0 - B) + B * users[userItem.UserId] / averageLength;
                        var confidence = userItem.Confidence * (K1 + 1.0) / (K1 * lengthNorm + userItem.Confidence) * idf;

                        return new UserItem(userItem.UserId, userItem.ItemId, confidence);
                    });
        }
    }
}
