using MathNet.Numerics.LinearAlgebra;

namespace Implicit
{
    public sealed class UserFeatures
    {
        internal const float Epsilon = 1e-10f;

        private float? norm;

        internal UserFeatures(Vector<float> vector)
        {
            this.Vector = vector;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "By design")]
        public float[] Factors
        {
            get
            {
                return this.Vector.ToArray();
            }
        }

        internal Vector<float> Vector { get; }

        internal float Norm
        {
            get
            {
                if (this.norm is null)
                {
                    var norm = (float)this.Vector.Norm(2.0);

                    if (norm == 0f)
                    {
                        norm = Epsilon;
                    }

                    this.norm = norm;
                }

                return this.norm.Value;
            }
        }
    }
}
