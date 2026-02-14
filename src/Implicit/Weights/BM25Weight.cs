using System.Text;

namespace Implicit.Weights
{
    public sealed class BM25Weight : Weight
    {
        private readonly float k1;
        private readonly float b;
        private readonly float averageLength;
        private readonly Dictionary<string, float> idf;

        internal BM25Weight(
            float k1,
            float b,
            float averageLength,
            Dictionary<string, float> idf)
        {
            this.k1 = k1;
            this.b = b;
            this.averageLength = averageLength;
            this.idf = idf;
        }

        public static BM25Weight Fit(Dictionary<int, Dictionary<string, float>> userItems, float k1 = 100, float b = 0.8f)
        {
            if (userItems is null)
            {
                throw new ArgumentNullException(nameof(userItems));
            }

            if (k1 < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(k1), k1, $"'{nameof(k1)}' cannot be negative.");
            }

            if (b < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(b), b, $"'{nameof(b)}' cannot be negative.");
            }

            var rowSums = userItems.ToDictionary(x => x.Key, x => x.Value.Sum(x => x.Value));
            var averageLength = rowSums.Values.Average();

            var n = userItems.Count;
            var idf = userItems
                .SelectMany(x => x.Value)
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => MathF.Log((n - x.Count() + 0.5f) / (x.Count() + 0.5f)));

            return new BM25Weight(k1, b, averageLength, idf);
        }

        public static BM25Weight Load(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            var k1 = reader.ReadSingle();
            var b = reader.ReadSingle();
            var averageLength = reader.ReadSingle();
            var idfCount = reader.ReadInt32();

            var idf = new Dictionary<string, float>(capacity: idfCount);

            for (var i = 0; i < idfCount; i++)
            {
                var key = reader.ReadString();
                var value = (float)reader.ReadSingle();

                idf.Add(key, value);
            }

            return new BM25Weight(k1, b, averageLength, idf);
        }

        public override void Save(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            writer.Write(this.k1);
            writer.Write(this.b);
            writer.Write(this.averageLength);
            writer.Write(this.idf.Count);

            foreach (var pair in this.idf)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }

        public override void Normalize(Dictionary<string, float> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var rowSum = items.Sum(x => x.Value);
            var lengthNorm = 1f - this.b + (this.b * (rowSum / this.averageLength));

            foreach (var item in items)
            {
                if (this.idf.TryGetValue(item.Key, out var idf))
                {
                    var tf = item.Value;

                    tf = tf * (this.k1 + 1f) / (tf + (this.k1 * lengthNorm));

                    items[item.Key] = tf * idf;
                }
            }
        }
    }
}
