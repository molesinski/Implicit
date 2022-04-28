using MathNet.Numerics.LinearAlgebra;

namespace Implicit
{
    public sealed class UserFactors
    {
        private double? norm;

        internal UserFactors(Vector<double> vector)
        {
            this.Vector = vector;
        }

        public double[] Factors
        {
            get
            {
                return this.Vector.ToArray();
            }
        }

        public double Norm
        {
            get
            {
                if (this.norm == null)
                {
                    var norm = this.Vector.Norm(2.0);

                    if (norm == 0.0)
                    {
                        norm = AlternatingLeastSquares.Epsilon;
                    }

                    this.norm = norm;
                }

                return this.norm.Value;
            }
        }

        internal Vector<double> Vector { get; private set; }
    }
}
