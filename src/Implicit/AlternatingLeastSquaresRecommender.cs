using System.Diagnostics;
using System.Text;
using Implicit.Utils;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;

namespace Implicit
{
    public sealed class AlternatingLeastSquaresRecommender : MatrixFactorizationRecommender
    {
        private readonly ObjectPoolSlim<FactorizationState> factorizationStatePool;
        private readonly float regularization;
        private readonly float alpha;
        private Matrix<float>? xtxir;
        private Matrix<float>? ytyir;

        public AlternatingLeastSquaresRecommender(
            int factors,
            float regularization,
            float alpha,
            Dictionary<string, int> users,
            Dictionary<string, int> items,
            Matrix<float> userFactors,
            Matrix<float> itemFactors)
            : base(
                factors,
                users,
                items,
                userFactors,
                itemFactors)
        {
            this.factorizationStatePool =
                new(
                    () => new FactorizationState(
                        userFactors.RowCount,
                        itemFactors.RowCount,
                        factors),
                    _ => { });

            this.regularization = regularization;
            this.alpha = alpha;
        }

        public float Regularization
        {
            get
            {
                return this.regularization;
            }
        }

        public float Alpha
        {
            get
            {
                return this.alpha;
            }
        }

        private Matrix<float> XtXiR
        {
            get
            {
                if (this.xtxir is null)
                {
                    var x = this.UserFactors;
                    var xtxir = x.TransposeThisAndMultiply(x).Add(Matrix<float>.Build.DiagonalIdentity(x.ColumnCount).Multiply(this.regularization));

                    this.xtxir = xtxir;
                }

                return this.xtxir;
            }
        }

        private Matrix<float> YtYiR
        {
            get
            {
                if (this.ytyir is null)
                {
                    var y = this.ItemFactors;
                    var ytyir = y.TransposeThisAndMultiply(y).Add(Matrix<float>.Build.DiagonalIdentity(y.ColumnCount).Multiply(this.regularization));

                    this.ytyir = ytyir;
                }

                return this.ytyir;
            }
        }

        public static AlternatingLeastSquaresRecommender Fit(UserItemMatrix userItems, AlternatingLeastSquaresParameters? parameters = null)
        {
            if (userItems is null)
            {
                throw new ArgumentNullException(nameof(userItems));
            }

            parameters ??= new();

            var users = userItems.Users;
            var items = userItems.Items;
            var cui = userItems.Matrix;
            var ciu = userItems.Matrix.Transpose();

            var loss = default(float?);
            var userFactors = Matrix<float>.Build.Random(users.Count, parameters.Factors, new ContinuousUniform(0, 0.01, parameters.Random));
            var itemFactors = Matrix<float>.Build.Random(items.Count, parameters.Factors, new ContinuousUniform(0, 0.01, parameters.Random));

            for (var iteration = 0; iteration < parameters.Iterations; iteration++)
            {
                parameters.ParallelOptions.CancellationToken.ThrowIfCancellationRequested();

                var stopwatch = Stopwatch.StartNew();

                if (parameters.UseConjugateGradient)
                {
                    LeastSquaresConjugateGradient(cui, userFactors, itemFactors, parameters.Regularization, parameters.Alpha, parameters.ConjugateGradientSteps, parameters.ParallelOptions);
                    LeastSquaresConjugateGradient(ciu, itemFactors, userFactors, parameters.Regularization, parameters.Alpha, parameters.ConjugateGradientSteps, parameters.ParallelOptions);
                }
                else
                {
                    LeastSquares(cui, userFactors, itemFactors, parameters.Regularization, parameters.Alpha, parameters.ParallelOptions);
                    LeastSquares(ciu, itemFactors, userFactors, parameters.Regularization, parameters.Alpha, parameters.ParallelOptions);
                }

                if (parameters.CalculateLossAtIteration)
                {
                    parameters.ParallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    loss = CalculateLoss(cui, userFactors, itemFactors, parameters.Regularization, parameters.Alpha, parameters.ParallelOptions);
                }

                parameters.IterationCompleted(iteration, loss, stopwatch.Elapsed);
            }

            return
                new AlternatingLeastSquaresRecommender(
                    parameters.Factors,
                    parameters.Regularization,
                    parameters.Alpha,
                    users,
                    items,
                    userFactors,
                    itemFactors);
        }

        public static AlternatingLeastSquaresRecommender Load(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            var factors = reader.ReadInt32();
            var regularization = reader.ReadSingle();
            var alpha = reader.ReadSingle();
            var userCount = reader.ReadInt32();
            var itemCount = reader.ReadInt32();

            var users = new Dictionary<string, int>(capacity: userCount);
            var items = new Dictionary<string, int>(capacity: itemCount);
            var userFactors = Matrix<float>.Build.Dense(userCount, factors);
            var itemFactors = Matrix<float>.Build.Dense(itemCount, factors);

            var xu = Vector<float>.Build.Dense(factors);
            var yi = Vector<float>.Build.Dense(factors);

            for (var u = 0; u < userCount; u++)
            {
                var userId = reader.ReadString();

                for (var f = 0; f < factors; f++)
                {
                    xu[f] = reader.ReadSingle();
                }

                users.Add(userId, u);
                userFactors.SetRow(u, xu);
            }

            for (var i = 0; i < itemCount; i++)
            {
                var itemId = reader.ReadString();

                for (var f = 0; f < factors; f++)
                {
                    yi[f] = reader.ReadSingle();
                }

                items.Add(itemId, i);
                itemFactors.SetRow(i, yi);
            }

            return
                new AlternatingLeastSquaresRecommender(
                    factors,
                    regularization,
                    alpha,
                    users,
                    items,
                    userFactors,
                    itemFactors);
        }

        public override void Save(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            var xu = Vector<float>.Build.Dense(this.UserFactors.ColumnCount);
            var yi = Vector<float>.Build.Dense(this.ItemFactors.ColumnCount);

            writer.Write(this.Factors);
            writer.Write(this.regularization);
            writer.Write(this.alpha);
            writer.Write(this.Users.Count);
            writer.Write(this.Items.Count);

            foreach (var pair in this.Users)
            {
                this.UserFactors.Row(pair.Value, xu);

                writer.Write(pair.Key);

                for (var f = 0; f < xu.Count; f++)
                {
                    writer.Write(xu[f]);
                }
            }

            foreach (var pair in this.Items)
            {
                this.ItemFactors.Row(pair.Value, yi);

                writer.Write(pair.Key);

                for (var f = 0; f < yi.Count; f++)
                {
                    writer.Write(yi[f]);
                }
            }
        }

        public override RecommenderFeatures? ComputeUserFeatures(IEnumerable<KeyValuePair<string, float>> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            using var state = this.factorizationStatePool.Lease();

            var empty = true;
            var ci = state.Instance.ci;

            ci.Clear();

            foreach (var item in items)
            {
                if (!(item.Value > 0))
                {
                    continue;
                }

                if (this.Items.TryGetValue(item.Key, out var i))
                {
                    empty = false;
                    ci[i] += item.Value;
                }
            }

            if (empty)
            {
                return null;
            }

            var xu = UserFactor(this.ItemFactors, this.YtYiR, ci, this.alpha, state.Instance);

            return new RecommenderFeatures(xu, norm: null);
        }

        public override RecommenderFeatures? ComputeItemFeatures(IEnumerable<KeyValuePair<string, float>> users)
        {
            if (users is null)
            {
                throw new ArgumentNullException(nameof(users));
            }

            using var state = this.factorizationStatePool.Lease();

            var empty = true;
            var cu = state.Instance.cu;

            cu.Clear();

            foreach (var user in users)
            {
                if (!(user.Value > 0))
                {
                    continue;
                }

                if (this.Users.TryGetValue(user.Key, out var u))
                {
                    empty = false;
                    cu[u] += user.Value;
                }
            }

            if (empty)
            {
                return null;
            }

            var yi = UserFactor(this.UserFactors, this.XtXiR, cu, this.alpha, state.Instance);

            return new RecommenderFeatures(yi, norm: null);
        }

        private static Vector<float> UserFactor(Matrix<float> y, Matrix<float> ytyir, Vector<float> ci, float alpha, FactorizationState s)
        {
            ytyir.CopyTo(s.A);
            s.b.Clear();

            foreach (var (i, value) in ci.EnumerateIndexed(Zeros.AllowSkip))
            {
                var confidence = alpha * value;
                y.Row(i, s.yi);

                s.yi.OuterProduct(s.yi, s.op);
                s.op.Multiply(confidence - 1, s.op);
                s.A.Add(s.op, s.A);

                s.yi.Multiply(confidence, s.yi);
                s.b.Add(s.yi, s.b);
            }

            return s.A.Solve(s.b);
        }

        private static void LeastSquares(Matrix<float> cui, Matrix<float> x, Matrix<float> y, float regularization, float alpha)
        {
            var factors = x.ColumnCount;
            var ytyir = y.TransposeThisAndMultiply(y).Add(Matrix<float>.Build.DiagonalIdentity(factors).Multiply(regularization));

            for (var u = 0; u < x.RowCount; u++)
            {
                var a = ytyir.Clone();
                var b = Vector<float>.Build.Dense(factors);

                foreach (var (i, value) in cui.Row(u).EnumerateIndexed(Zeros.AllowSkip))
                {
                    var confidence = alpha * value;
                    var yi = y.Row(i);

                    a.Add(yi.OuterProduct(yi).Multiply(confidence - 1), a);
                    b.Add(yi.Multiply(confidence), b);
                }

                var xu = a.Solve(b);

                x.SetRow(u, xu);
            }
        }

        private static void LeastSquares(Matrix<float> cui, Matrix<float> x, Matrix<float> y, float regularization, float alpha, ParallelOptions parallelOptions)
        {
            var factors = x.ColumnCount;
            var ytyir = y.TransposeThisAndMultiply(y).Add(Matrix<float>.Build.DiagonalIdentity(factors).Multiply(regularization));

            Parallel.For(
                0,
                x.RowCount,
                parallelOptions,
                () => new
                {
                    ci = Vector<float>.Build.Sparse(cui.ColumnCount),
                    xu = Vector<float>.Build.Dense(factors),
                    yi = Vector<float>.Build.Dense(factors),
                    A = Matrix<float>.Build.Dense(factors, factors),
                    b = Vector<float>.Build.Dense(factors),
                    op = Matrix<float>.Build.Dense(factors, factors),
                },
                (u, _, s) =>
                {
                    cui.Row(u, s.ci);
                    ytyir.CopyTo(s.A);
                    s.b.Clear();

                    foreach (var (i, value) in s.ci.EnumerateIndexed(Zeros.AllowSkip))
                    {
                        var confidence = alpha * value;
                        y.Row(i, s.yi);

                        s.yi.OuterProduct(s.yi, s.op);
                        s.op.Multiply(confidence - 1, s.op);
                        s.A.Add(s.op, s.A);

                        s.yi.Multiply(confidence, s.yi);
                        s.b.Add(s.yi, s.b);
                    }

                    s.A.Solve(s.b, s.xu);

                    x.SetRow(u, s.xu);

                    return s;
                },
                _ => { });
        }

        private static void LeastSquaresConjugateGradient(Matrix<float> cui, Matrix<float> x, Matrix<float> y, float regularization, float alpha, int steps)
        {
            var factors = x.ColumnCount;
            var ytyir = y.TransposeThisAndMultiply(y).Add(Matrix<float>.Build.DiagonalIdentity(factors).Multiply(regularization));

            for (var u = 0; u < x.RowCount; u++)
            {
                var xu = x.Row(u);
                var r = ytyir.Multiply(xu).Multiply(-1);

                foreach (var (i, value) in cui.Row(u).EnumerateIndexed(Zeros.AllowSkip))
                {
                    var confidence = alpha * value;
                    var yi = y.Row(i);

                    r.Add(yi.Multiply(confidence - ((confidence - 1) * yi.DotProduct(xu))), r);
                }

                var p = r.Clone();
                var rsold = r.DotProduct(r);

                if (rsold < 1e-20f)
                {
                    continue;
                }

                for (var step = 0; step < steps; step++)
                {
                    var ap = ytyir.Multiply(p);

                    foreach (var (i, value) in cui.Row(u).EnumerateIndexed(Zeros.AllowSkip))
                    {
                        var confidence = alpha * value;
                        var yi = y.Row(i);

                        ap.Add(yi.Multiply(yi.DotProduct(p)).Multiply(confidence - 1), ap);
                    }

                    var alpha2 = rsold / p.DotProduct(ap);

                    xu.Add(p.Multiply(alpha2), xu);
                    r.Subtract(ap.Multiply(alpha2), r);

                    var rsnew = r.DotProduct(r);

                    if (rsnew < 1e-20f)
                    {
                        break;
                    }

                    p = r.Add(p.Multiply(rsnew / rsold));
                    rsold = rsnew;
                }

                x.SetRow(u, xu);
            }
        }

        private static void LeastSquaresConjugateGradient(Matrix<float> cui, Matrix<float> x, Matrix<float> y, float regularization, float alpha, int steps, ParallelOptions parallelOptions)
        {
            var factors = x.ColumnCount;
            var ytyir = y.TransposeThisAndMultiply(y).Add(Matrix<float>.Build.DiagonalIdentity(factors).Multiply(regularization));

            Parallel.For(
                0,
                x.RowCount,
                parallelOptions,
                () => new
                {
                    ci = Vector<float>.Build.Sparse(cui.ColumnCount),
                    xu = Vector<float>.Build.Dense(factors),
                    yi = Vector<float>.Build.Dense(factors),
                    r = Vector<float>.Build.Dense(factors),
                    p = Vector<float>.Build.Dense(factors),
                    pm = Vector<float>.Build.Dense(factors),
                    Ap = Vector<float>.Build.Dense(factors),
                },
                (u, _, s) =>
                {
                    cui.Row(u, s.ci);
                    x.Row(u, s.xu);
                    ytyir.Multiply(s.xu, s.r);
                    s.r.Multiply(-1, s.r);

                    foreach (var (i, value) in s.ci.EnumerateIndexed(Zeros.AllowSkip))
                    {
                        var confidence = alpha * value;
                        y.Row(i, s.yi);

                        s.yi.Multiply(confidence - ((confidence - 1) * s.yi.DotProduct(s.xu)), s.yi);
                        s.r.Add(s.yi, s.r);
                    }

                    s.r.CopyTo(s.p);

                    var rsold = s.r.DotProduct(s.r);

                    if (rsold < 1e-20f)
                    {
                        return s;
                    }

                    for (var step = 0; step < steps; step++)
                    {
                        ytyir.Multiply(s.p, s.Ap);

                        foreach (var (i, value) in cui.Row(u).EnumerateIndexed(Zeros.AllowSkip))
                        {
                            var confidence = alpha * value;
                            y.Row(i, s.yi);

                            s.yi.Multiply((confidence - 1) * s.yi.DotProduct(s.p), s.yi);
                            s.Ap.Add(s.yi, s.Ap);
                        }

                        var alpha2 = rsold / s.p.DotProduct(s.Ap);

                        s.p.Multiply(alpha2, s.pm);
                        s.xu.Add(s.pm, s.xu);
                        s.Ap.Multiply(alpha2, s.Ap);
                        s.r.Subtract(s.Ap, s.r);

                        var rsnew = s.r.DotProduct(s.r);

                        if (rsnew < 1e-20f)
                        {
                            break;
                        }

                        s.p.Multiply(rsnew / rsold, s.pm);
                        s.r.Add(s.pm, s.p);
                        rsold = rsnew;
                    }

                    x.SetRow(u, s.xu);
                    return s;
                },
                _ => { });
        }

        private static float CalculateLoss(Matrix<float> cui, Matrix<float> x, Matrix<float> y, float regularization, float alpha)
        {
            var nnz = 0;
            var loss = 0f;
            var total_confidence = 0f;
            var item_norm = 0f;
            var user_norm = 0f;

            var yty = y.TransposeThisAndMultiply(y);

            for (var u = 0; u < x.RowCount; u++)
            {
                var xu = x.Row(u);
                var r = yty.Multiply(xu);

                foreach (var (i, value) in cui.Row(u).EnumerateIndexed(Zeros.AllowSkip))
                {
                    var confidence = alpha * value;
                    var yi = y.Row(i);

                    var temp = ((confidence - 1) * yi.DotProduct(xu)) - (2 * confidence);

                    r = r.Add(yi.Multiply(temp));

                    nnz += 1;
                    total_confidence += confidence;
                    loss += confidence;
                }

                loss += r.DotProduct(xu);
                user_norm += xu.DotProduct(xu);
            }

            for (var i = 0; i < y.RowCount; i++)
            {
                var yi = y.Row(i);

                item_norm += yi.DotProduct(yi);
            }

            loss += regularization * (item_norm + user_norm);

            return loss / (total_confidence + (y.RowCount * x.RowCount) - nnz);
        }

        private static float CalculateLoss(Matrix<float> cui, Matrix<float> x, Matrix<float> y, float regularization, float alpha, ParallelOptions parallelOptions)
        {
            var factors = x.ColumnCount;
            var yty = y.TransposeThisAndMultiply(y);

            var mutex = new object();
            var nnz = 0;
            var loss = 0f;
            var total_confidence = 0f;
            var item_norm = 0f;
            var user_norm = 0f;

            Parallel.For(
                0,
                x.RowCount,
                parallelOptions,
                () => new
                {
                    ci = Vector<float>.Build.Sparse(cui.ColumnCount),
                    xu = Vector<float>.Build.Dense(factors),
                    yi = Vector<float>.Build.Dense(factors),
                    r = Vector<float>.Build.Dense(factors),
                    nnz = new int[1],
                    loss = new float[1],
                    total_confidence = new float[1],
                    user_norm = new float[1],
                },
                (u, _, s) =>
                {
                    cui.Row(u, s.ci);
                    x.Row(u, s.xu);

                    yty.Multiply(s.xu, s.r);

                    foreach (var (i, value) in s.ci.EnumerateIndexed(Zeros.AllowSkip))
                    {
                        var confidence = alpha * value;
                        y.Row(i, s.yi);

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
                y.RowCount,
                parallelOptions,
                () => new
                {
                    yi = Vector<float>.Build.Dense(factors),
                    item_norm = new float[1],
                },
                (i, _, s) =>
                {
                    y.Row(i, s.yi);

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

            return loss / (total_confidence + (y.RowCount * x.RowCount) - nnz);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "By design")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "By design")]
        private sealed class FactorizationState
        {
            public FactorizationState(int users, int items, int factors)
            {
                this.cu = Vector<float>.Build.Sparse(users);
                this.ci = Vector<float>.Build.Sparse(items);
                this.yi = Vector<float>.Build.Dense(factors);
                this.A = Matrix<float>.Build.Dense(factors, factors);
                this.b = Vector<float>.Build.Dense(factors);
                this.op = Matrix<float>.Build.Dense(factors, factors);
            }

            public Vector<float> cu { get; }

            public Vector<float> ci { get; }

            public Vector<float> yi { get; }

            public Matrix<float> A { get; }

            public Vector<float> b { get; }

            public Matrix<float> op { get; }
        }
    }
}
