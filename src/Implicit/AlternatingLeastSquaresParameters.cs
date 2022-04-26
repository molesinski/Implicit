using System;

namespace Implicit
{
    public class AlternatingLeastSquaresParameters
    {
        public AlternatingLeastSquaresParameters(
            int factors,
            double regularization = 0.01,
            int iterations = 10,
            bool useConjugateGradient = true,
            bool calculateLossAtIteration = false)
        {
            this.Factors = factors;
            this.Regularization = regularization;
            this.Iterations = iterations;
            this.UseConjugateGradient = useConjugateGradient;
            this.CalculateLossAtIteration = calculateLossAtIteration;
        }

        public int Factors { get; }

        public double Regularization { get; }

        public int Iterations { get; }

        public bool UseConjugateGradient { get; }

        public bool CalculateLossAtIteration { get; }

        public Action<AlternatingLeastSquaresIterationCompletedContext>? OnIterationCompleted { get; set; }

        internal void IterationCompleted(int iteration, double loss, TimeSpan elapsed)
        {
            this.OnIterationCompleted?.Invoke(new AlternatingLeastSquaresIterationCompletedContext(iteration, loss, elapsed));
        }
    }
}
