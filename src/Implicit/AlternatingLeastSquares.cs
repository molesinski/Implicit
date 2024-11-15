﻿#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1313 // Variable names should begin with lower-case letter

using System.Diagnostics;
using System.Text;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;

namespace Implicit
{
    public sealed class AlternatingLeastSquares : IRecommender, IMatrixFactorizationRecommender
    {
        private const int ConjugateGradientSteps = 3;

        private readonly int factors;
        private readonly double regularization;
        private readonly double loss;
        private readonly Dictionary<string, int> userMap;
        private readonly Dictionary<string, int> itemMap;
        private readonly Matrix<double> userFactors;
        private readonly Matrix<double> itemFactors;
        private Vector<double>? itemNorms;
        private Matrix<double>? yty;

        private AlternatingLeastSquares(
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

        public IReadOnlyCollection<string> Users
        {
            get
            {
                return this.userMap.Keys;
            }
        }

        public IReadOnlyCollection<string> Items
        {
            get
            {
                return this.itemMap.Keys;
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
                if (this.itemNorms is null)
                {
                    var itemNorms = this.itemFactors.RowNorms(2.0);

                    for (var i = 0; i < itemNorms.Count; i++)
                    {
                        if (itemNorms[i] == 0.0)
                        {
                            itemNorms[i] = UserFeatures.Epsilon;
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
                if (this.yty is null)
                {
                    var Y = this.itemFactors;
                    var YtY = Y.TransposeThisAndMultiply(Y);

                    this.yty = YtY;
                }

                return this.yty;
            }
        }

        public static AlternatingLeastSquares Fit(DataMatrix data, AlternatingLeastSquaresParameters parameters)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var userMap = data.UserMap;
            var itemMap = data.ItemMap;
            var Cui = data.Cui;
            var Ciu = data.Ciu;

            var loss = 0.0;
            var userFactors = Matrix<double>.Build.Random(userMap.Count, parameters.Factors, new ContinuousUniform(0, 0.01));
            var itemFactors = Matrix<double>.Build.Random(itemMap.Count, parameters.Factors, new ContinuousUniform(0, 0.01));

            for (var iteration = 0; iteration < parameters.Iterations; iteration++)
            {
                parameters.ParallelOptions.CancellationToken.ThrowIfCancellationRequested();

                var stopwatch = Stopwatch.StartNew();

                if (parameters.UseConjugateGradient)
                {
                    LeastSquaresConjugateGradientFast(Cui, userFactors, itemFactors, parameters.Regularization, parameters.ParallelOptions);
                    LeastSquaresConjugateGradientFast(Ciu, itemFactors, userFactors, parameters.Regularization, parameters.ParallelOptions);
                }
                else
                {
                    LeastSquaresFast(Cui, userFactors, itemFactors, parameters.Regularization, parameters.ParallelOptions);
                    LeastSquaresFast(Ciu, itemFactors, userFactors, parameters.Regularization, parameters.ParallelOptions);
                }

                if (parameters.CalculateLossAtIteration)
                {
                    parameters.ParallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    loss = CalculateLossFast(Cui, userFactors, itemFactors, parameters.Regularization, parameters.ParallelOptions);
                }

                parameters.IterationCompleted(iteration, loss, stopwatch.Elapsed);
            }

            if (!parameters.CalculateLossAtIteration)
            {
                parameters.ParallelOptions.CancellationToken.ThrowIfCancellationRequested();

                loss = CalculateLossFast(Cui, userFactors, itemFactors, parameters.Regularization, parameters.ParallelOptions);
            }

            return
                new AlternatingLeastSquares(
                    parameters.Factors,
                    parameters.Regularization,
                    loss,
                    userMap,
                    itemMap,
                    userFactors,
                    itemFactors);
        }

        public static AlternatingLeastSquares Load(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            var factors = reader.ReadInt32();
            var regularization = reader.ReadDouble();
            var loss = reader.ReadDouble();

            var users = reader.ReadInt32();
            var items = reader.ReadInt32();

            var userMap = new Dictionary<string, int>();
            var itemMap = new Dictionary<string, int>();
            var userFactors = Matrix<double>.Build.Dense(users, factors);
            var itemFactors = Matrix<double>.Build.Dense(items, factors);

            var xu = Vector<double>.Build.Dense(factors);
            var yi = Vector<double>.Build.Dense(factors);

            for (var u = 0; u < users; u++)
            {
                var userId = reader.ReadString();

                for (var f = 0; f < factors; f++)
                {
                    xu[f] = reader.ReadDouble();
                }

                userMap.Add(userId, u);
                userFactors.SetRow(u, xu);
            }

            for (var i = 0; i < items; i++)
            {
                var itemId = reader.ReadString();

                for (var f = 0; f < factors; f++)
                {
                    yi[f] = reader.ReadDouble();
                }

                itemMap.Add(itemId, i);
                itemFactors.SetRow(i, yi);
            }

            return
                new AlternatingLeastSquares(
                    factors,
                    regularization,
                    loss,
                    userMap,
                    itemMap,
                    userFactors,
                    itemFactors);
        }

        public RecommenderResult RecommendUser(string userId)
        {
            if (userId is null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (!this.userMap.TryGetValue(userId, out var u))
            {
                return new RecommenderResult(SharedObjectPools.KeyValueLists.Lease());
            }

            var xu = this.userFactors.Row(u);
            var user = new UserFeatures(xu);

            return this.RecommendUser(user);
        }

        public RecommenderResult RecommendUser(UserFeatures user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var xu = user.Vector;
            var yi = Vector<double>.Build.Dense(this.factors);

            var storage = SharedObjectPools.KeyValueLists.Lease();

            foreach (var item in this.itemMap)
            {
                this.itemFactors.Row(item.Value, yi);

                storage.Instance.Add(new KeyValuePair<string, double>(item.Key, xu.DotProduct(yi)));
            }

            return new RecommenderResult(storage);
        }

        public RecommenderResult RecommendItem(string itemId)
        {
            if (itemId is null)
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            if (!this.itemMap.TryGetValue(itemId, out var i))
            {
                return new RecommenderResult(SharedObjectPools.KeyValueLists.Lease());
            }

            var yi = this.itemFactors.Row(i);
            var yj = Vector<double>.Build.Dense(this.factors);

            var storage = SharedObjectPools.KeyValueLists.Lease();

            foreach (var item in this.itemMap)
            {
                var j = item.Value;

                this.itemFactors.Row(j, yj);

                storage.Instance.Add(new KeyValuePair<string, double>(item.Key, yi.DotProduct(yj) / this.ItemNorms[j]));
            }

            return new RecommenderResult(storage);
        }

        public RecommenderResult RankUsers(string userId, IEnumerable<KeyValuePair<string, UserFeatures>> users)
        {
            if (userId is null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (users is null)
            {
                throw new ArgumentNullException(nameof(users));
            }

            if (!this.userMap.TryGetValue(userId, out var u))
            {
                return new RecommenderResult(SharedObjectPools.KeyValueLists.Lease());
            }

            var xu = this.userFactors.Row(u);
            var user = new UserFeatures(xu);

            return this.RankUsers(user, users);
        }

        public RecommenderResult RankUsers(UserFeatures user, IEnumerable<KeyValuePair<string, UserFeatures>> users)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (users is null)
            {
                throw new ArgumentNullException(nameof(users));
            }

            var xu = user.Vector;

            var storage = SharedObjectPools.KeyValueLists.Lease();

            foreach (var pair in users)
            {
                var xv = pair.Value.Vector;
                var norm = pair.Value.Norm;
                var score = xu.DotProduct(xv) / norm;

                storage.Instance.Add(new KeyValuePair<string, double>(pair.Key, score));
            }

            return new RecommenderResult(storage);
        }

        public UserFeatures? GetUserFeatures(string userId)
        {
            if (userId is null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (!this.userMap.TryGetValue(userId, out var u))
            {
                return null;
            }

            var xu = this.userFactors.Row(u);
            var user = new UserFeatures(xu);

            return user;
        }

        public UserFeatures? ComputeUserFeatures(Dictionary<string, double> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var userItems = default(Dictionary<int, double>);

            foreach (var item in items)
            {
                if (this.itemMap.TryGetValue(item.Key, out var i))
                {
                    userItems ??= new Dictionary<int, double>(items.Count);
                    userItems.Add(i, item.Value);
                }
            }

            if (userItems is null)
            {
                return null;
            }

            var u = 0;
            var Cui = new Dictionary<int, Dictionary<int, double>>(1) { [u] = userItems };
            var xu = AlternatingLeastSquares.UserFactor(this.itemFactors, this.YtY, Cui, u, this.regularization, this.factors);

            return new UserFeatures(xu);
        }

        public void Save(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

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

        private static Vector<double> UserFactor(Matrix<double> Y, Matrix<double> YtY, Dictionary<int, Dictionary<int, double>> Cui, int u, double regularization, int factors)
        {
            var (a, b) = UserLinearEquation(Y, YtY, Cui, u, regularization, factors);

            return a.Solve(b);
        }

        private static (Matrix<double> A, Vector<double> B) UserLinearEquation(Matrix<double> Y, Matrix<double> YtY, Dictionary<int, Dictionary<int, double>> Cui, int u, double regularization, int factors)
        {
            var yi = Vector<double>.Build.Dense(factors);
            var A = YtY.Add(Matrix<double>.Build.DenseIdentity(factors).Multiply(regularization));
            var b = Vector<double>.Build.Dense(factors);

            foreach (var pair in Cui[u])
            {
                var i = pair.Key;
                var confidence = pair.Value;
                Y.Row(i, yi);

                A.Add(yi.OuterProduct(yi).Multiply(confidence - 1), A);
                b.Add(yi.Multiply(confidence), b);
            }

            return (A, b);
        }

#pragma warning disable IDE0051 // Remove unused private members
        private static void LeastSquares(Dictionary<int, Dictionary<int, double>> Cui, Matrix<double> X, Matrix<double> Y, double regularization, ParallelOptions parallelOptions)
#pragma warning restore IDE0051 // Remove unused private members
        {
            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y);

            Parallel.For(
                0,
                X.RowCount,
                parallelOptions,
                u =>
                {
                    X.SetRow(u, UserFactor(Y, YtY, Cui, u, regularization, factors));
                });
        }

        private static void LeastSquaresFast(Dictionary<int, Dictionary<int, double>> Cui, Matrix<double> X, Matrix<double> Y, double regularization, ParallelOptions parallelOptions)
        {
            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y).Add(Matrix<double>.Build.DenseIdentity(factors).Multiply(regularization));

            Parallel.For(
                0,
                X.RowCount,
                parallelOptions,
                () => new
                {
                    xu = Vector<double>.Build.Dense(factors),
                    yi = Vector<double>.Build.Dense(factors),
                    A = Matrix<double>.Build.Dense(factors, factors),
                    b = Vector<double>.Build.Dense(factors),
                    op = Matrix<double>.Build.Dense(factors, factors),
                },
                (u, _, s) =>
                {
                    YtY.CopyTo(s.A);
                    s.b.Clear();

                    foreach (var pair in Cui[u])
                    {
                        var i = pair.Key;
                        var confidence = pair.Value;
                        Y.Row(i, s.yi);

                        s.yi.OuterProduct(s.yi, s.op);
                        s.op.Multiply(confidence - 1, s.op);
                        s.A.Add(s.op, s.A);

                        s.yi.Multiply(confidence, s.yi);
                        s.b.Add(s.yi, s.b);
                    }

                    s.A.Solve(s.b, s.xu);

                    X.SetRow(u, s.xu);

                    return s;
                },
                _ => { });
        }

#pragma warning disable IDE0051 // Remove unused private members
        private static void LeastSquaresConjugateGradient(Dictionary<int, Dictionary<int, double>> Cui, Matrix<double> X, Matrix<double> Y, double regularization, ParallelOptions parallelOptions)
#pragma warning restore IDE0051 // Remove unused private members
        {
            var users = X.RowCount;
            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y).Add(Matrix<double>.Build.DenseIdentity(factors).Multiply(regularization));

            Parallel.For(
                0,
                users,
                parallelOptions,
                u =>
                {
                    var xu = X.Row(u);
                    var r = YtY.Multiply(xu).Multiply(-1);

                    foreach (var pair in Cui[u])
                    {
                        var i = pair.Key;
                        var confidence = pair.Value;
                        var yi = Y.Row(i);

                        r.Add(yi.Multiply(confidence - ((confidence - 1) * yi.DotProduct(xu))), r);
                    }

                    var p = r.Clone();
                    var rsold = r.DotProduct(r);

                    for (var step = 0; step < ConjugateGradientSteps; step++)
                    {
                        var Ap = YtY.Multiply(p);

                        foreach (var pair in Cui[u])
                        {
                            var i = pair.Key;
                            var confidence = pair.Value;
                            var yi = Y.Row(i);

                            Ap.Add(yi.Multiply(yi.DotProduct(p)).Multiply(confidence - 1), Ap);
                        }

                        var alpha = rsold / p.DotProduct(Ap);

                        xu.Add(p.Multiply(alpha), xu);
                        r.Subtract(Ap.Multiply(alpha), r);

                        var rsnew = r.DotProduct(r);

                        if (rsnew < UserFeatures.Epsilon)
                        {
                            break;
                        }

                        p = r.Add(p.Multiply(rsnew / rsold));
                        rsold = rsnew;
                    }

                    X.SetRow(u, xu);
                });
        }

        private static void LeastSquaresConjugateGradientFast(Dictionary<int, Dictionary<int, double>> Cui, Matrix<double> X, Matrix<double> Y, double regularization, ParallelOptions parallelOptions)
        {
            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y).Add(Matrix<double>.Build.DenseIdentity(factors).Multiply(regularization));

            Parallel.For(
                0,
                X.RowCount,
                parallelOptions,
                () => new
                {
                    xu = Vector<double>.Build.Dense(factors),
                    yi = Vector<double>.Build.Dense(factors),
                    r = Vector<double>.Build.Dense(factors),
                    p = Vector<double>.Build.Dense(factors),
                    pm = Vector<double>.Build.Dense(factors),
                    Ap = Vector<double>.Build.Dense(factors),
                },
                (u, _, s) =>
                {
                    X.Row(u, s.xu);
                    YtY.Multiply(s.xu, s.r);
                    s.r.Multiply(-1, s.r);

                    foreach (var pair in Cui[u])
                    {
                        var i = pair.Key;
                        var confidence = pair.Value;
                        Y.Row(i, s.yi);

                        s.yi.Multiply(confidence - ((confidence - 1) * s.yi.DotProduct(s.xu)), s.yi);
                        s.r.Add(s.yi, s.r);
                    }

                    s.r.CopyTo(s.p);

                    var rsold = s.r.DotProduct(s.r);

                    for (var step = 0; step < ConjugateGradientSteps; step++)
                    {
                        YtY.Multiply(s.p, s.Ap);

                        foreach (var pair in Cui[u])
                        {
                            var i = pair.Key;
                            var confidence = pair.Value;
                            Y.Row(i, s.yi);

                            s.yi.Multiply((confidence - 1) * s.yi.DotProduct(s.p), s.yi);
                            s.Ap.Add(s.yi, s.Ap);
                        }

                        var alpha = rsold / s.p.DotProduct(s.Ap);

                        s.p.Multiply(alpha, s.pm);
                        s.xu.Add(s.pm, s.xu);
                        s.Ap.Multiply(alpha, s.Ap);
                        s.r.Subtract(s.Ap, s.r);

                        var rsnew = s.r.DotProduct(s.r);

                        if (rsnew < UserFeatures.Epsilon)
                        {
                            break;
                        }

                        s.p.Multiply(rsnew / rsold, s.pm);
                        s.r.Add(s.pm, s.p);
                        rsold = rsnew;
                    }

                    X.SetRow(u, s.xu);
                    return s;
                },
                _ => { });
        }

#pragma warning disable IDE0051 // Remove unused private members
        private static double CalculateLoss(Dictionary<int, Dictionary<int, double>> Cui, Matrix<double> X, Matrix<double> Y, double regularization)
#pragma warning restore IDE0051 // Remove unused private members
        {
            var nnz = 0;
            var loss = 0.0;
            var total_confidence = 0.0;
            var item_norm = 0.0;
            var user_norm = 0.0;

            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y);
            var xu = Vector<double>.Build.Dense(factors);
            var yi = Vector<double>.Build.Dense(factors);
            var r = Vector<double>.Build.Dense(factors);

            for (var u = 0; u < X.RowCount; u++)
            {
                X.Row(u, xu);
                YtY.Multiply(xu, r);

                foreach (var pair in Cui[u])
                {
                    var i = pair.Key;
                    var confidence = pair.Value;
                    Y.Row(i, yi);

                    var temp = ((confidence - 1) * yi.DotProduct(xu)) - (2 * confidence);

                    r.Add(yi.Multiply(temp), r);

                    nnz += 1;
                    total_confidence += confidence;
                    loss += confidence;
                }

                loss += r.DotProduct(xu);
                user_norm += xu.DotProduct(xu);
            }

            for (var i = 0; i < Y.RowCount; i++)
            {
                Y.Row(i, yi);

                item_norm += yi.DotProduct(yi);
            }

            loss += regularization * (item_norm + user_norm);

            return loss / (total_confidence + (Y.RowCount * X.RowCount) - nnz);
        }

        private static double CalculateLossFast(Dictionary<int, Dictionary<int, double>> Cui, Matrix<double> X, Matrix<double> Y, double regularization, ParallelOptions parallelOptions)
        {
            var mutex = new object();
            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y);

            var nnz = 0;
            var loss = 0.0;
            var total_confidence = 0.0;
            var item_norm = 0.0;
            var user_norm = 0.0;

            Parallel.For(
                0,
                X.RowCount,
                parallelOptions,
                () => new
                {
                    xu = Vector<double>.Build.Dense(factors),
                    yi = Vector<double>.Build.Dense(factors),
                    r = Vector<double>.Build.Dense(factors),
                    nnz = new int[1],
                    loss = new double[1],
                    total_confidence = new double[1],
                    user_norm = new double[1],
                },
                (u, _, s) =>
                {
                    X.Row(u, s.xu);

                    YtY.Multiply(s.xu, s.r);

                    foreach (var pair in Cui[u])
                    {
                        var i = pair.Key;
                        var confidence = pair.Value;
                        Y.Row(i, s.yi);

                        var temp = ((confidence - 1) * s.yi.DotProduct(s.xu)) - (2 * confidence);

                        s.yi.Multiply(temp, s.yi);
                        s.r.Add(s.yi, s.r);

                        s.nnz[0] += 1;
                        s.total_confidence[0] += confidence;
                        s.loss[0] += confidence;
                    }

                    s.loss[0] += s.r.DotProduct(s.xu);
                    s.user_norm[0] += s.xu.DotProduct(s.xu);

                    return s;
                },
                s =>
                {
                    lock (mutex)
                    {
                        nnz += s.nnz[0];
                        loss += s.loss[0];
                        total_confidence += s.total_confidence[0];
                        user_norm += s.user_norm[0];
                    }
                });

            Parallel.For(
                0,
                Y.RowCount,
                parallelOptions,
                () => new
                {
                    yi = Vector<double>.Build.Dense(factors),
                    item_norm = new double[1],
                },
                (i, _, s) =>
                {
                    Y.Row(i, s.yi);

                    s.item_norm[0] += s.yi.DotProduct(s.yi);

                    return s;
                },
                s =>
                {
                    lock (mutex)
                    {
                        item_norm += s.item_norm[0];
                    }
                });

            loss += regularization * (item_norm + user_norm);

            return loss / (total_confidence + (Y.RowCount * X.RowCount) - nnz);
        }
    }
}
