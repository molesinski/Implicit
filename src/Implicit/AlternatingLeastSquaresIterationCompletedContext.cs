namespace Implicit
{
    public sealed class AlternatingLeastSquaresIterationCompletedContext
    {
        public AlternatingLeastSquaresIterationCompletedContext(int iteration, double loss, TimeSpan elapsed)
        {
            this.Iteration = iteration;
            this.Loss = loss;
            this.Elapsed = elapsed;
        }

        public int Iteration { get; }

        public double Loss { get; }

        public TimeSpan Elapsed { get; }
    }
}
