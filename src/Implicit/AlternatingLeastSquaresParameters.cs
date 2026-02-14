using MathNet.Numerics.Random;

namespace Implicit
{
    public sealed class AlternatingLeastSquaresParameters
    {
        public AlternatingLeastSquaresParameters(
            int factors = 64,
            float regularization = 0.01f,
            float alpha = 1f,
            int iterations = 15,
            bool useConjugateGradient = true,
            int conjugateGradientSteps = 3,
            bool calculateLossAtIteration = false,
            ParallelOptions? parallelOptions = null,
            Random? random = null)
        {
            if (factors <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(factors), factors, $"'{nameof(factors)}' cannot be negative or zero.");
            }

            if (regularization < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(regularization), regularization, $"'{nameof(regularization)}' cannot be negative.");
            }

            if (alpha <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(alpha), alpha, $"'{nameof(alpha)}' cannot be negative or zero.");
            }

            if (iterations <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(iterations), iterations, $"'{nameof(iterations)}' cannot be negative or zero.");
            }

            if (conjugateGradientSteps <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(conjugateGradientSteps), conjugateGradientSteps, $"'{nameof(conjugateGradientSteps)}' cannot be negative or zero.");
            }

            this.Factors = factors;
            this.Regularization = regularization;
            this.Alpha = alpha;
            this.Iterations = iterations;
            this.UseConjugateGradient = useConjugateGradient;
            this.ConjugateGradientSteps = conjugateGradientSteps;
            this.CalculateLossAtIteration = calculateLossAtIteration;
            this.ParallelOptions = parallelOptions ?? new();
            this.Random = random ?? SystemRandomSource.Default;
        }

        public int Factors { get; }

        public float Regularization { get; }

        public float Alpha { get; }

        public int Iterations { get; }

        public bool UseConjugateGradient { get; }

        public int ConjugateGradientSteps { get; }

        public bool CalculateLossAtIteration { get; }

        public ParallelOptions ParallelOptions { get; }

        public Random Random { get; }

        public Action<AlternatingLeastSquaresIterationCompletedContext>? OnIterationCompleted { get; set; }

        internal void IterationCompleted(int iteration, float? loss, TimeSpan elapsed)
        {
            this.OnIterationCompleted?.Invoke(new AlternatingLeastSquaresIterationCompletedContext(iteration, loss, elapsed));
        }
    }
}
