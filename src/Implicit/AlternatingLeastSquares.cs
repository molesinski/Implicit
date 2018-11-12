using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;

namespace Implicit
{
    using LabeledMatrix = Dictionary<string, Dictionary<string, float>>;
    using SparseMatrix = Dictionary<int, Dictionary<int, float>>;

    public class AlternatingLeastSquares
    {
        internal const float Epsilon = 1e-10f;

        private readonly int factors;
        private readonly float regularization;
        private readonly int iterations;
        private readonly bool useConjugateGradient;
        private readonly bool calculateLossAtIteration;

        public AlternatingLeastSquares(
            int factors = 100,
            float regularization = 0.01f,
            int iterations = 15,
            bool useConjugateGradient = true,
            bool calculateLossAtIteration = false)
        {
            this.factors = factors;
            this.regularization = regularization;
            this.iterations = iterations;
            this.useConjugateGradient = useConjugateGradient;
            this.calculateLossAtIteration = calculateLossAtIteration;
        }

        public event EventHandler<AlternatingLeastSquaresIterationCompletedEventArgs> OnIterationCompleted;

        public AlternatingLeastSquaresRecommender Fit(LabeledMatrix data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return this.Fit(data.SelectMany(user => user.Value, (user, item) => new UserItem(user.Key, item.Key, item.Value)));
        }

        public AlternatingLeastSquaresRecommender Fit(IEnumerable<UserItem> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var userMap = new Dictionary<string, int>();
            var itemMap = new Dictionary<string, int>();
            var Cui = new SparseMatrix();
            var Ciu = new SparseMatrix();
            var nextUserIndex = 0;
            var nextItemIndex = 0;

            foreach (var userItem in data)
            {
                if (!userMap.TryGetValue(userItem.UserId, out var u))
                {
                    userMap.Add(userItem.UserId, u = nextUserIndex++);
                }

                if (!itemMap.TryGetValue(userItem.ItemId, out var i))
                {
                    itemMap.Add(userItem.ItemId, i = nextItemIndex++);
                }

                if (!Cui.TryGetValue(u, out var user))
                {
                    Cui.Add(u, user = new Dictionary<int, float>());
                }

                if (!Ciu.TryGetValue(i, out var item))
                {
                    Ciu.Add(i, item = new Dictionary<int, float>());
                }

                user.Add(i, userItem.Confidence);
                item.Add(u, userItem.Confidence);
            }

            var users = userMap.Count;
            var items = itemMap.Count;
            var loss = 0.0f;

            var userFactors = Matrix<float>.Build.Random(users, this.factors, new ContinuousUniform(0, 0.01));
            var itemFactors = Matrix<float>.Build.Random(items, this.factors, new ContinuousUniform(0, 0.01));

            for (var iteration = 0; iteration < this.iterations; iteration++)
            {
                var stopwatch = Stopwatch.StartNew();

                if (this.useConjugateGradient)
                {
                    LeastSquaresConjugateGradientFast(Cui, userFactors, itemFactors, this.regularization);
                    LeastSquaresConjugateGradientFast(Ciu, itemFactors, userFactors, this.regularization);
                }
                else
                {
                    LeastSquaresFast(Cui, userFactors, itemFactors, this.regularization);
                    LeastSquaresFast(Ciu, itemFactors, userFactors, this.regularization);
                }

                if (this.calculateLossAtIteration)
                {
                    loss = CalculateLossFast(Cui, userFactors, itemFactors, this.regularization);
                }

                this.OnIterationCompleted?.Invoke(this, new AlternatingLeastSquaresIterationCompletedEventArgs(iteration, loss, stopwatch.Elapsed));
            }

            if (!this.calculateLossAtIteration)
            {
                loss = CalculateLossFast(Cui, userFactors, itemFactors, this.regularization);
            }

            return new AlternatingLeastSquaresRecommender(
                this.factors,
                this.regularization,
                loss,
                userMap,
                itemMap,
                userFactors,
                itemFactors);
        }

        internal static Vector<float> UserFactor(Matrix<float> Y, Matrix<float> YtY, SparseMatrix Cui, int u, float regularization, int factors)
        {
            var equation = UserLinearEquation(Y, YtY, Cui, u, regularization, factors);

            return equation.A.Solve(equation.b);
        }

        private static LinearEquation UserLinearEquation(Matrix<float> Y, Matrix<float> YtY, SparseMatrix Cui, int u, float regularization, int factors)
        {
            var yi = Vector<float>.Build.Dense(factors);
            var A = YtY.Add(Matrix<float>.Build.DenseIdentity(factors).Multiply(regularization));
            var b = Vector<float>.Build.Dense(factors);

            foreach (var pair in Cui[u])
            {
                var i = pair.Key;
                var confidence = pair.Value;
                Y.Row(i, yi);

                A.Add(yi.OuterProduct(yi).Multiply(confidence - 1), A);
                b.Add(yi.Multiply(confidence), b);
            }

            return new LinearEquation(A, b);
        }

        private static void LeastSquares(SparseMatrix Cui, Matrix<float> X, Matrix<float> Y, float regularization)
        {
            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y);

            Parallel.For(
                0,
                X.RowCount,
                u =>
                {
                    X.SetRow(u, UserFactor(Y, YtY, Cui, u, regularization, factors));
                });
        }

        private static void LeastSquaresFast(SparseMatrix Cui, Matrix<float> X, Matrix<float> Y, float regularization)
        {
            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y).Add(Matrix<float>.Build.DenseIdentity(factors).Multiply(regularization));

            Parallel.For(
                0,
                X.RowCount,
                () => new
                {
                    xu = Vector<float>.Build.Dense(factors),
                    yi = Vector<float>.Build.Dense(factors),
                    A = Matrix<float>.Build.Dense(factors, factors),
                    b = Vector<float>.Build.Dense(factors),
                    op = Matrix<float>.Build.Dense(factors, factors),
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

        private static void LeastSquaresConjugateGradient(SparseMatrix Cui, Matrix<float> X, Matrix<float> Y, float regularization, int iterations = 3)
        {
            var users = X.RowCount;
            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y).Add(Matrix<float>.Build.DenseIdentity(factors).Multiply(regularization));

            Parallel.For(0, users, u =>
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

                for (var iteration = 0; iteration < iterations; iteration++)
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

                    if (rsnew < Epsilon)
                    {
                        break;
                    }

                    p = r.Add(p.Multiply(rsnew / rsold));
                    rsold = rsnew;
                }

                X.SetRow(u, xu);
            });
        }

        private static void LeastSquaresConjugateGradientFast(SparseMatrix Cui, Matrix<float> X, Matrix<float> Y, float regularization, int iterations = 3)
        {
            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y).Add(Matrix<float>.Build.DenseIdentity(factors).Multiply(regularization));

            Parallel.For(
                0,
                X.RowCount,
                () => new
                {
                    xu = Vector<float>.Build.Dense(factors),
                    yi = Vector<float>.Build.Dense(factors),
                    r = Vector<float>.Build.Dense(factors),
                    p = Vector<float>.Build.Dense(factors),
                    pm = Vector<float>.Build.Dense(factors),
                    Ap = Vector<float>.Build.Dense(factors),
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

                    for (var iteration = 0; iteration < iterations; iteration++)
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

                        if (rsnew < Epsilon)
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

        private static float CalculateLoss(SparseMatrix Cui, Matrix<float> X, Matrix<float> Y, float regularization)
        {
            var nnz = 0;
            var loss = 0.0f;
            var total_confidence = 0.0f;
            var item_norm = 0.0f;
            var user_norm = 0.0f;

            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y);
            var xu = Vector<float>.Build.Dense(factors);
            var yi = Vector<float>.Build.Dense(factors);
            var r = Vector<float>.Build.Dense(factors);

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

        private static float CalculateLossFast(SparseMatrix Cui, Matrix<float> X, Matrix<float> Y, float regularization)
        {
            var mutex = new object();
            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y);

            var nnz = 0;
            var loss = 0.0f;
            var total_confidence = 0.0f;
            var item_norm = 0.0f;
            var user_norm = 0.0f;

            Parallel.For(
                0,
                X.RowCount,
                () => new
                {
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
                () => new
                {
                    yi = Vector<float>.Build.Dense(factors),
                    item_norm = new float[1],
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

        private struct LinearEquation
        {
            public LinearEquation(Matrix<float> A, Vector<float> b)
            {
                this.A = A;
                this.b = b;
            }

            public Matrix<float> A { get; }

            public Vector<float> b { get; }
        }
    }
}
