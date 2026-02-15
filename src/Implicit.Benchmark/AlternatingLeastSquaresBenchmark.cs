using BenchmarkDotNet.Attributes;
using MathNet.Numerics;

namespace Implicit.Benchmark
{
    [MemoryDiagnoser(false)]
    public class AlternatingLeastSquaresBenchmark
    {
        private UserItemMatrix data = default!;

        public enum ProviderId
        {
            Managed,
            NativeMKL,
        }

        [Params(64)]
        public int Factors { get; set; }

        [Params(true)]
        public bool UseConjugateGradient { get; set; }

        [Params(ProviderId.Managed, ProviderId.NativeMKL)]
        public ProviderId Provider { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            Control.MaxDegreeOfParallelism = 1;

            switch (this.Provider)
            {
                case ProviderId.Managed:
                    Control.UseManaged();
                    break;
                case ProviderId.NativeMKL:
                    Control.UseNativeMKL();
                    break;
            }

            this.data = UserItemMatrix.Build(DataFactory.CreateCheckerBoard(n: 1024));
        }

        [Benchmark(OperationsPerInvoke = 1)]
        public MatrixFactorizationRecommender FitModel()
        {
            var recommender = AlternatingLeastSquaresRecommender.Fit(
                this.data,
                new AlternatingLeastSquaresParameters(
                    factors: this.Factors,
                    useConjugateGradient: this.UseConjugateGradient,
                    random: new Random(42)));

            return recommender;
        }
    }
}
