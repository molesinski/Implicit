using MathNet.Numerics.LinearAlgebra;

namespace Implicit
{
    public sealed class UserFeatures
    {
        internal const double Epsilon = 1e-10;

        private double? norm;

        internal UserFeatures(Vector<double> vector)
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

        internal double Norm
        {
            get
            {
                if (this.norm == null)
                {
                    var norm = this.Vector.Norm(2.0);

                    if (norm == 0.0)
                    {
                        norm = Epsilon;
                    }

                    this.norm = norm;
                }

                return this.norm.Value;
            }
        }

        internal Vector<double> Vector { get; }
    }
}
