using MathNet.Numerics.LinearAlgebra;

namespace Implicit
{
    public abstract class MatrixFactorizationRecommender : Recommender
    {
        private readonly int factors;
        private readonly Dictionary<string, int> users;
        private readonly Dictionary<string, int> items;
        private readonly Matrix<float> userFactors;
        private readonly Matrix<float> itemFactors;
        private Vector<float>? userNorms;
        private Vector<float>? itemNorms;

        protected MatrixFactorizationRecommender(
            int factors,
            Dictionary<string, int> users,
            Dictionary<string, int> items,
            Matrix<float> userFactors,
            Matrix<float> itemFactors)
        {
            this.factors = factors;
            this.users = users;
            this.items = items;
            this.userFactors = userFactors;
            this.itemFactors = itemFactors;
        }

        public int Factors
        {
            get
            {
                return this.factors;
            }
        }

        public Dictionary<string, int> Users
        {
            get
            {
                return this.users;
            }
        }

        public Dictionary<string, int> Items
        {
            get
            {
                return this.items;
            }
        }

        public Matrix<float> UserFactors
        {
            get
            {
                return this.userFactors;
            }
        }

        public Matrix<float> ItemFactors
        {
            get
            {
                return this.itemFactors;
            }
        }

        private Vector<float> UserNorms
        {
            get
            {
                if (this.userNorms is null)
                {
                    var userNorms = this.userFactors.RowNorms(2.0).ToSingle();

                    for (var i = 0; i < userNorms.Count; i++)
                    {
                        if (userNorms[i] == 0)
                        {
                            userNorms[i] = 1e-10f;
                        }
                    }

                    this.userNorms = userNorms;
                }

                return this.userNorms;
            }
        }

        private Vector<float> ItemNorms
        {
            get
            {
                if (this.itemNorms is null)
                {
                    var itemNorms = this.itemFactors.RowNorms(2.0).ToSingle();

                    for (var i = 0; i < itemNorms.Count; i++)
                    {
                        if (itemNorms[i] == 0)
                        {
                            itemNorms[i] = 1e-10f;
                        }
                    }

                    this.itemNorms = itemNorms;
                }

                return this.itemNorms;
            }
        }

        public override RecommenderResult Recommend(string userId)
        {
            if (userId is null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (this.GetUserFeatures(userId) is not RecommenderFeatures userFeatures)
            {
                return RecommenderResult.Empty;
            }

            return this.Recommend(userFeatures);
        }

        public override RecommenderResult SimilarUsers(string userId)
        {
            if (userId is null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (this.GetUserFeatures(userId) is not RecommenderFeatures userFeatures)
            {
                return RecommenderResult.Empty;
            }

            return this.SimilarUsers(userFeatures);
        }

        public override RecommenderResult SimilarItems(string itemId)
        {
            if (itemId is null)
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            if (this.GetItemFeatures(itemId) is not RecommenderFeatures itemFeatures)
            {
                return RecommenderResult.Empty;
            }

            return this.SimilarItems(itemFeatures);
        }

        public RecommenderResult Recommend(RecommenderFeatures userFeatures)
        {
            if (userFeatures is null)
            {
                throw new ArgumentNullException(nameof(userFeatures));
            }

            return
                RecommenderResult.Create(
                    storage =>
                    {
                        var xu = userFeatures.Factors;
                        var yi = Vector<float>.Build.Dense(this.factors);

                        foreach (var item in this.items)
                        {
                            this.itemFactors.Row(item.Value, yi);

                            storage.Add(new(item.Key, xu.DotProduct(yi)));
                        }

                        storage.Sort((x, y) => y.Value.CompareTo(x.Value));
                    });
        }

        public RecommenderResult SimilarUsers(RecommenderFeatures userFeatures)
        {
            if (userFeatures is null)
            {
                throw new ArgumentNullException(nameof(userFeatures));
            }

            return
                RecommenderResult.Create(
                    storage =>
                    {
                        var xu = userFeatures.Factors;
                        var xv = Vector<float>.Build.Dense(this.factors);

                        foreach (var user in this.users)
                        {
                            var v = user.Value;

                            this.userFactors.Row(v, xv);

                            storage.Add(new(user.Key, xu.DotProduct(xv) / this.UserNorms[v]));
                        }

                        storage.Sort((x, y) => y.Value.CompareTo(x.Value));
                    });
        }

        public RecommenderResult SimilarItems(RecommenderFeatures itemFeatures)
        {
            if (itemFeatures is null)
            {
                throw new ArgumentNullException(nameof(itemFeatures));
            }

            return
                RecommenderResult.Create(
                    storage =>
                    {
                        var yi = itemFeatures.Factors;
                        var yj = Vector<float>.Build.Dense(this.factors);

                        foreach (var item in this.items)
                        {
                            var j = item.Value;

                            this.itemFactors.Row(j, yj);

                            storage.Add(new(item.Key, yi.DotProduct(yj) / this.ItemNorms[j]));
                        }

                        storage.Sort((x, y) => y.Value.CompareTo(x.Value));
                    });
        }

        public RecommenderFeatures? GetUserFeatures(string userId)
        {
            if (userId is null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (!this.users.TryGetValue(userId, out var u))
            {
                return null;
            }

            var xu = this.userFactors.Row(u);
            var norm = this.UserNorms[u];

            return new RecommenderFeatures(xu, norm);
        }

        public RecommenderFeatures? GetItemFeatures(string itemId)
        {
            if (itemId is null)
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            if (!this.items.TryGetValue(itemId, out var i))
            {
                return null;
            }

            var yi = this.itemFactors.Row(i);
            var norm = this.ItemNorms[i];

            return new RecommenderFeatures(yi, norm);
        }

        public abstract RecommenderFeatures? ComputeUserFeatures(IEnumerable<KeyValuePair<string, float>> items);

        public abstract RecommenderFeatures? ComputeItemFeatures(IEnumerable<KeyValuePair<string, float>> users);
    }
}
