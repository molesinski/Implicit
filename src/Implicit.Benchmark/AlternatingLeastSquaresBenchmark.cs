using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MathNet.Numerics;
using MathNet.Numerics.Providers.MKL;

namespace Implicit.Benchmark
{
    public class AlternatingLeastSquaresBenchmark
    {
        private const string DefaultFileName = "usersha1-artmbid-artname-plays.tsv";

        private readonly string fileName;
        private AlternatingLeastSquaresData? data;

        public enum ProviderId
        {
            Managed,
            NativeMKL,
        }

        public AlternatingLeastSquaresBenchmark()
            : this(DefaultFileName)
        {
        }

        public AlternatingLeastSquaresBenchmark(string fileName)
        {
            this.fileName = fileName;
        }

        [Params(32)]
        public int Factors { get; set; }

        [Params(ProviderId.NativeMKL)]
        public ProviderId Provider { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            switch (this.Provider)
            {
                case ProviderId.Managed:
                    Control.UseManaged();
                    break;
                case ProviderId.NativeMKL:
                    MklControl.UseNativeMKL(MklConsistency.Auto, MklPrecision.Double, MklAccuracy.High);
                    break;
            }

            this.data = AlternatingLeastSquaresData.Load(LoadUserItems(this.fileName));

            static IEnumerable<UserItem> LoadUserItems(string fileName)
            {
                var file = new FileInfo(fileName);

                while (!file.Exists)
                {
                    if (file.Directory?.Parent == null)
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
                        var userId = parts[0];
                        var itemId = parts[1];

                        if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(itemId))
                        {
                            if (double.TryParse(parts.Last(), NumberStyles.None, CultureInfo.InvariantCulture, out var confidence) && confidence > 0)
                            {
                                yield return new UserItem(userId, itemId, confidence);
                            }
                        }
                    }
                }
            }
        }

        [Benchmark(OperationsPerInvoke = 1)]
        public IMatrixFactorizationRecommender FitModel()
        {
            var parameters = new AlternatingLeastSquaresParameters(
                factors: this.Factors,
                regularization: 0.01f,
                iterations: 1,
                useConjugateGradient: true,
                calculateLossAtIteration: true);

            var recommender = AlternatingLeastSquares.Fit(this.data!, parameters);

            return recommender;
        }
    }
}
