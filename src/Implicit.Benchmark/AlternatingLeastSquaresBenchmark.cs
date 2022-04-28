using BenchmarkDotNet.Attributes;
using MathNet.Numerics;
using MathNet.Numerics.Providers.MKL;

namespace Implicit.Benchmark
{
    public class AlternatingLeastSquaresBenchmark
    {
        private AlternatingLeastSquaresData? data;

        public enum ProviderId
        {
            Managed,
            NativeMKL,
        }

        [Params(64)]
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

            this.data = AlternatingLeastSquaresData.Load(DataFactory.GetLastFm360k());
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
