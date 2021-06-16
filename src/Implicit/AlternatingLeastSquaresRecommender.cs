using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

namespace Implicit
{
    public class AlternatingLeastSquaresRecommender : IRecommender, IMatrixFactorizationRecommender
    {
        private readonly int factors;
        private readonly double regularization;
        private readonly double loss;
        private readonly Dictionary<string, int> userMap;
        private readonly Dictionary<string, int> itemMap;
        private readonly Matrix<double> userFactors;
        private readonly Matrix<double> itemFactors;
        private Vector<double> itemNorms;
        private Matrix<double> yty;

        internal AlternatingLeastSquaresRecommender(
            int factors,
            double regularization,
            double loss,
            Dictionary<string, int> userMap,
            Dictionary<string, int> itemMap,
            Matrix<double> userFactors,
            Matrix<double> itemFactors)
        {
            this.factors = factors;
            this.regularization = regularization;
            this.loss = loss;
            this.userMap = userMap;
            this.itemMap = itemMap;
            this.userFactors = userFactors;
            this.itemFactors = itemFactors;
        }

        public AlternatingLeastSquaresRecommender(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            this.factors = int.Parse(reader.ReadLine(), CultureInfo.InvariantCulture);
            this.regularization = double.Parse(reader.ReadLine(), CultureInfo.InvariantCulture);
            this.loss = double.Parse(reader.ReadLine(), CultureInfo.InvariantCulture);

            var users = int.Parse(reader.ReadLine(), CultureInfo.InvariantCulture);
            var items = int.Parse(reader.ReadLine(), CultureInfo.InvariantCulture);

            this.userMap = new Dictionary<string, int>();
            this.itemMap = new Dictionary<string, int>();
            this.userFactors = Matrix<double>.Build.Dense(users, this.factors);
            this.itemFactors = Matrix<double>.Build.Dense(items, this.factors);

            for (var u = 0; u < users; u++)
            {
                var line = reader.ReadLine();
                var parts = line.Split('\t');

                this.userMap.Add(parts.First(), u);
                this.userFactors.SetRow(u, parts.Skip(1).Select(o => double.Parse(o, CultureInfo.InvariantCulture)).ToArray());
            }

            for (var i = 0; i < items; i++)
            {
                var line = reader.ReadLine();
                var parts = line.Split('\t');

                this.itemMap.Add(parts.First(), i);
                this.itemFactors.SetRow(i, parts.Skip(1).Select(o => double.Parse(o, CultureInfo.InvariantCulture)).ToArray());
            }
        }

        public AlternatingLeastSquaresRecommender(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            this.factors = reader.ReadInt32();
            this.regularization = reader.ReadDouble();
            this.loss = reader.ReadDouble();

            var users = reader.ReadInt32();
            var items = reader.ReadInt32();

            this.userMap = new Dictionary<string, int>();
            this.itemMap = new Dictionary<string, int>();
            this.userFactors = Matrix<double>.Build.Dense(users, this.factors);
            this.itemFactors = Matrix<double>.Build.Dense(items, this.factors);

            var xu = Vector<double>.Build.Dense(this.factors);
            var yi = Vector<double>.Build.Dense(this.factors);

            for (var u = 0; u < users; u++)
            {
                var userId = reader.ReadString();

                for (var f = 0; f < this.factors; f++)
                {
                    xu[f] = reader.ReadDouble();
                }

                this.userMap.Add(userId, u);
                this.userFactors.SetRow(u, xu);
            }

            for (var i = 0; i < items; i++)
            {
                var itemId = reader.ReadString();

                for (var f = 0; f < this.factors; f++)
                {
                    yi[f] = reader.ReadDouble();
                }

                this.itemMap.Add(itemId, i);
                this.itemFactors.SetRow(i, yi);
            }
        }

        public int Factors
        {
            get
            {
                return this.factors;
            }
        }

        public double Regularization
        {
            get
            {
                return this.regularization;
            }
        }

        public double Loss
        {
            get
            {
                return this.loss;
            }
        }

        public Dictionary<string, double[]> UserFactors
        {
            get
            {
                var xu = Vector<double>.Build.Dense(this.factors);

                return this.userMap
                    .ToDictionary(
                        o => o.Key,
                        o =>
                        {
                            this.userFactors.Row(o.Value, xu);

                            return xu.ToArray();
                        });
            }
        }

        public Dictionary<string, double[]> ItemFactors
        {
            get
            {
                var yi = Vector<double>.Build.Dense(this.factors);

                return this.itemMap
                    .ToDictionary(
                        o => o.Key,
                        o =>
                        {
                            this.itemFactors.Row(o.Value, yi);

                            return yi.ToArray();
                        });
            }
        }

        private Vector<double> ItemNorms
        {
            get
            {
                if (this.itemNorms == null)
                {
                    var itemNorms = this.itemFactors.RowNorms(2.0);

                    for (var i = 0; i < itemNorms.Count; i++)
                    {
                        if (itemNorms[i] == 0.0)
                        {
                            itemNorms[i] = AlternatingLeastSquares.Epsilon;
                        }
                    }

                    this.itemNorms = itemNorms;
                }

                return this.itemNorms;
            }
        }

        private Matrix<double> YtY
        {
            get
            {
                if (this.yty == null)
                {
                    var Y = this.itemFactors;
                    var YtY = Y.TransposeThisAndMultiply(Y);

                    this.yty = YtY;
                }

                return this.yty;
            }
        }

        public IEnumerable<ItemResult> RecommendUser(string userId)
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (!this.userMap.ContainsKey(userId))
            {
                return Enumerable.Empty<ItemResult>();
            }

            var xu = this.userFactors.Row(this.userMap[userId]);
            var user = new UserFactors(xu);

            return this.RecommendUser(user);
        }

        public IEnumerable<ItemResult> RecommendUser(UserFactors user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var xu = user.Vector;
            var yi = Vector<double>.Build.Dense(this.factors);

            var result = new List<ItemResult>(this.itemMap.Count);

            foreach (var item in this.itemMap)
            {
                this.itemFactors.Row(item.Value, yi);

                result.Add(new ItemResult(item.Key, xu.DotProduct(yi)));
            }

            result.Sort((a, b) => -1 * a.Score.CompareTo(b.Score));

            return result;
        }

        public IEnumerable<ItemResult> RecommendItem(string itemId)
        {
            if (itemId == null)
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            if (!this.itemMap.ContainsKey(itemId))
            {
                return Enumerable.Empty<ItemResult>();
            }

            var yi = this.itemFactors.Row(this.itemMap[itemId]);
            var yj = Vector<double>.Build.Dense(this.factors);

            var result = new List<ItemResult>(this.itemMap.Count);

            foreach (var item in this.itemMap)
            {
                if (item.Key != itemId)
                {
                    var j = item.Value;
                    this.itemFactors.Row(j, yj);

                    result.Add(new ItemResult(item.Key, yi.DotProduct(yj) / this.ItemNorms[j]));
                }
            }

            result.Sort((a, b) => -1 * a.Score.CompareTo(b.Score));

            return result;
        }

        public IEnumerable<TKey> RankUsers<TKey>(string userId, IEnumerable<KeyValuePair<TKey, UserFactors>> users)
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (users == null)
            {
                throw new ArgumentNullException(nameof(users));
            }

            if (!this.userMap.ContainsKey(userId))
            {
                return Enumerable.Empty<TKey>();
            }

            var xu = this.userFactors.Row(this.userMap[userId]);
            var user = new UserFactors(xu);

            return this.RankUsers(user, users);
        }

        public IEnumerable<TKey> RankUsers<TKey>(UserFactors user, IEnumerable<KeyValuePair<TKey, UserFactors>> users)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (users == null)
            {
                throw new ArgumentNullException(nameof(users));
            }

            var xu = user.Vector;

            return users
                .Select(
                    pair =>
                    {
                        var xv = pair.Value.Vector;
                        var norm = pair.Value.Norm;

                        return new { pair.Key, Score = xu.DotProduct(xv) / norm };
                    })
                .OrderByDescending(o => o.Score)
                .Select(o => o.Key)
                .ToList();
        }

        public UserFactors GetUserFactors(string userId)
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (!this.userMap.ContainsKey(userId))
            {
                return null;
            }

            var xu = this.userFactors.Row(this.userMap[userId]);
            var user = new UserFactors(xu);

            return user;
        }

        public UserFactors ComputeUserFactors(Dictionary<string, double> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var u = 0;
            var Cui = items
                .Select(o => new { ItemId = o.Key, Confidence = o.Value })
                .Where(o => this.itemMap.ContainsKey(o.ItemId))
                .Select(o => new { User = u, Item = this.itemMap[o.ItemId], o.Confidence })
                .GroupBy(o => o.User)
                .ToDictionary(o => o.Key, o => o.ToDictionary(p => p.Item, p => p.Confidence));

            var xu = AlternatingLeastSquares.UserFactor(this.itemFactors, this.YtY, Cui, u, this.regularization, this.factors);

            return new UserFactors(xu);
        }

        public void Save(TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var xu = Vector<double>.Build.Dense(this.factors);
            var yi = Vector<double>.Build.Dense(this.factors);

            writer.WriteLine(this.factors.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine(this.regularization.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine(this.loss.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine(this.userMap.Count.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine(this.itemMap.Count.ToString(CultureInfo.InvariantCulture));

            foreach (var pair in this.userMap)
            {
                this.userFactors.Row(pair.Value, xu);

                writer.Write(pair.Key);

                for (var i = 0; i < this.factors; i++)
                {
                    writer.Write('\t');
                    writer.Write(xu[i].ToString("R", CultureInfo.InvariantCulture));
                }

                writer.WriteLine();
            }

            foreach (var pair in this.itemMap)
            {
                this.itemFactors.Row(pair.Value, yi);

                writer.Write(pair.Key);

                for (var i = 0; i < this.factors; i++)
                {
                    writer.Write('\t');
                    writer.Write(yi[i].ToString("R", CultureInfo.InvariantCulture));
                }

                writer.WriteLine();
            }
        }

        public void Save(BinaryWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var xu = Vector<double>.Build.Dense(this.factors);
            var yi = Vector<double>.Build.Dense(this.factors);

            writer.Write(this.factors);
            writer.Write(this.regularization);
            writer.Write(this.loss);
            writer.Write(this.userMap.Count);
            writer.Write(this.itemMap.Count);

            foreach (var pair in this.userMap)
            {
                this.userFactors.Row(pair.Value, xu);

                writer.Write(pair.Key);

                for (var f = 0; f < this.factors; f++)
                {
                    writer.Write(xu[f]);
                }
            }

            foreach (var pair in this.itemMap)
            {
                this.itemFactors.Row(pair.Value, yi);

                writer.Write(pair.Key);

                for (var f = 0; f < this.factors; f++)
                {
                    writer.Write(yi[f]);
                }
            }
        }
    }
}
