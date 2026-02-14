using BenchmarkDotNet.Attributes;
using MathNet.Numerics;
using MathNet.Numerics.Providers.MKL;

namespace Implicit.Benchmark
{
    [MemoryDiagnoser(false)]
    public class AlternatingLeastSquaresBenchmark
    {
        private UserItemMatrix? data;

        public enum ProviderId
        {
            Managed,
            NativeMKL,
        }

        [Params(64)]
        public int Factors { get; set; }

        [Params(ProviderId.Managed, ProviderId.NativeMKL)]
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
                    MklControl.UseNativeMKL(MklConsistency.Auto, MklPrecision.Single, MklAccuracy.High);
                    break;
            }

            this.data = UserItemMatrix.Build(DataFactory.CreateCheckerBoard(n: 3_000));
        }

        [Benchmark(OperationsPerInvoke = 1)]
        public MatrixFactorizationRecommender FitModel()
        {
            var recommender = AlternatingLeastSquaresRecommender.Fit(
                this.data!,
                new AlternatingLeastSquaresParameters(
                    factors: this.Factors));

            return recommender;
        }
    }
}
