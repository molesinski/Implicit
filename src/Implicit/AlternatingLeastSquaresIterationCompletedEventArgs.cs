﻿using System;

namespace Implicit
{
    public class AlternatingLeastSquaresIterationCompletedEventArgs : EventArgs
    {
        public AlternatingLeastSquaresIterationCompletedEventArgs(int iteration, float loss, TimeSpan elapsed)
        {
            this.Iteration = iteration;
            this.Loss = loss;
            this.Elapsed = elapsed;
        }

        public int Iteration { get; }

        public float Loss { get; }

        public TimeSpan Elapsed { get; }
    }
}
