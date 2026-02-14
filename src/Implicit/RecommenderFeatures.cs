using MathNet.Numerics.LinearAlgebra;

namespace Implicit
{
    public sealed class RecommenderFeatures
    {
        private float? norm;

        internal RecommenderFeatures(Vector<float> factors, float? norm)
        {
            this.Factors = factors;
            this.norm = norm;
        }

        public Vector<float> Factors { get; }

        internal float Norm
        {
            get
            {
                if (this.norm is null)
                {
                    var norm = (float)this.Factors.Norm(2.0);

                    if (norm == 0f)
                    {
                        norm = 1e-10f;
                    }

                    this.norm = norm;
                }

                return this.norm.Value;
            }
        }

        public RecommenderResult SimilarFeatures(IEnumerable<KeyValuePair<string, RecommenderFeatures>> features)
        {
            if (features is null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            return
                RecommenderResult.Create(
                    storage =>
                    {
                        var xu = this.Factors;

                        foreach (var feature in features)
                        {
                            var xv = feature.Value.Factors;
                            var norm = feature.Value.Norm;

                            storage.Add(new(feature.Key, xu.DotProduct(xv) / norm));
                        }

                        storage.Sort((x, y) => y.Value.CompareTo(x.Value));
                    });
        }
    }
}
