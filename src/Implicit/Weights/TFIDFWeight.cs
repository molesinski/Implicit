using System.Text;

namespace Implicit.Weights
{
    public sealed class TFIDFWeight : Weight
    {
        private readonly Dictionary<string, float> idf;

        internal TFIDFWeight(
            Dictionary<string, float> idf)
        {
            this.idf = idf;
        }

        public static TFIDFWeight Fit(Dictionary<int, Dictionary<string, float>> userItems)
        {
            if (userItems is null)
            {
                throw new ArgumentNullException(nameof(userItems));
            }

            var n = userItems.Count;
            var idf = userItems
                .SelectMany(x => x.Value)
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => MathF.Log((float)n / x.Count()));

            return new TFIDFWeight(idf);
        }

        public static TFIDFWeight Load(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            var idfCount = reader.ReadInt32();

            var idf = new Dictionary<string, float>(capacity: idfCount);

            for (var i = 0; i < idfCount; i++)
            {
                var key = reader.ReadString();
                var value = reader.ReadSingle();

                idf.Add(key, value);
            }

            return new TFIDFWeight(idf);
        }

        public override void Save(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

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

            foreach (var item in items)
            {
                if (this.idf.TryGetValue(item.Key, out var idf))
                {
                    var tf = item.Value;

                    items[item.Key] = tf * idf;
                }
            }
        }
    }
}
