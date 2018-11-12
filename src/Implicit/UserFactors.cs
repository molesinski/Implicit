using MathNet.Numerics.LinearAlgebra;

namespace Implicit
{
    public class UserFactors
    {
        private float? norm;

        internal UserFactors(Vector<float> vector)
        {
            this.Vector = vector;
        }

        public float[] Factors
        {
            get
            {
                return this.Vector.ToArray();
            }
        }

        public float Norm
        {
            get
            {
                if (this.norm == null)
                {
                    var norm = (float)this.Vector.Norm(2.0);

                    if (norm == 0.0)
                    {
                        norm = AlternatingLeastSquares.Epsilon;
                    }

                    this.norm = norm;
                }

                return this.norm.Value;
            }
        }

        internal Vector<float> Vector { get; private set; }
    }
}
