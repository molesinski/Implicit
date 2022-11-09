using System.Globalization;

namespace Implicit.Benchmark
{
    public static class DataFactory
    {
        public static IEnumerable<DataRow> GetLastFm360k()
        {
            var fileName = "usersha1-artmbid-artname-plays.tsv";
            var file = new FileInfo(fileName);

            while (!file.Exists)
            {
                if (file.Directory?.Parent is null)
                {
                    throw new InvalidOperationException($"Unable to find data set file '{fileName}' within parent directory structure.");
                }

                file = new FileInfo(Path.Combine(file.Directory.Parent.FullName, fileName));
            }

            using var stream = file.OpenText();

            while (stream.ReadLine() is string line)
            {
                var parts = line.Split('\t');

                if (parts.Length >= 3)
                {
                    var user = parts[0];
                    var artist = parts[2];

                    if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(artist))
                    {
                        if (double.TryParse(parts.Last(), NumberStyles.None, CultureInfo.InvariantCulture, out var plays) && plays > 0)
                        {
                            yield return new DataRow(user, artist, plays);
                        }
                    }
                }
            }
        }
    }
}
