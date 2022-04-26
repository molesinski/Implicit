using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;

namespace Implicit
{
    using SparseMatrix = Dictionary<int, Dictionary<int, double>>;

    public static class AlternatingLeastSquares
    {
        internal const double Epsilon = 1e-10;

        public static AlternatingLeastSquaresRecommender Fit(AlternatingLeastSquaresData data, AlternatingLeastSquaresParameters parameters)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
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
                var stopwatch = Stopwatch.StartNew();

                if (parameters.UseConjugateGradient)
                {
                    LeastSquaresConjugateGradientFast(Cui, userFactors, itemFactors, parameters.Regularization);
                    LeastSquaresConjugateGradientFast(Ciu, itemFactors, userFactors, parameters.Regularization);
                }
                else
                {
                    LeastSquaresFast(Cui, userFactors, itemFactors, parameters.Regularization);
                    LeastSquaresFast(Ciu, itemFactors, userFactors, parameters.Regularization);
                }

                if (parameters.CalculateLossAtIteration)
                {
                    loss = CalculateLossFast(Cui, userFactors, itemFactors, parameters.Regularization);
                }

                parameters.IterationCompleted(iteration, loss, stopwatch.Elapsed);
            }

            if (!parameters.CalculateLossAtIteration)
            {
                loss = CalculateLossFast(Cui, userFactors, itemFactors, parameters.Regularization);
            }

            return new AlternatingLeastSquaresRecommender(
                parameters.Factors,
                parameters.Regularization,
                loss,
                userMap,
                itemMap,
                userFactors,
                itemFactors);
        }

        internal static Vector<double> UserFactor(Matrix<double> Y, Matrix<double> YtY, SparseMatrix Cui, int u, double regularization, int factors)
        {
            var equation = UserLinearEquation(Y, YtY, Cui, u, regularization, factors);

            return equation.A.Solve(equation.b);
        }

        private static LinearEquation UserLinearEquation(Matrix<double> Y, Matrix<double> YtY, SparseMatrix Cui, int u, double regularization, int factors)
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

            return new LinearEquation(A, b);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Illustrates less performant, but more readable implementation.")]
        private static void LeastSquares(SparseMatrix Cui, Matrix<double> X, Matrix<double> Y, double regularization)
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

        private static void LeastSquaresFast(SparseMatrix Cui, Matrix<double> X, Matrix<double> Y, double regularization)
        {
            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y).Add(Matrix<double>.Build.DenseIdentity(factors).Multiply(regularization));

            Parallel.For(
                0,
                X.RowCount,
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Illustrates less performant, but more readable implementation.")]
        private static void LeastSquaresConjugateGradient(SparseMatrix Cui, Matrix<double> X, Matrix<double> Y, double regularization, int iterations = 3)
        {
            var users = X.RowCount;
            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y).Add(Matrix<double>.Build.DenseIdentity(factors).Multiply(regularization));

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

        private static void LeastSquaresConjugateGradientFast(SparseMatrix Cui, Matrix<double> X, Matrix<double> Y, double regularization, int iterations = 3)
        {
            var factors = X.ColumnCount;
            var YtY = Y.TransposeThisAndMultiply(Y).Add(Matrix<double>.Build.DenseIdentity(factors).Multiply(regularization));

            Parallel.For(
                0,
                X.RowCount,
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Illustrates less performant, but more readable implementation.")]
        private static double CalculateLoss(SparseMatrix Cui, Matrix<double> X, Matrix<double> Y, double regularization)
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

        private static double CalculateLossFast(SparseMatrix Cui, Matrix<double> X, Matrix<double> Y, double regularization)
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

        private readonly struct LinearEquation
        {
            public LinearEquation(Matrix<double> A, Vector<double> b)
            {
                this.A = A;
                this.b = b;
            }

            public Matrix<double> A { get; }


            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Readability for linear equation coefficients.")]
            public Vector<double> b { get; }
        }
    }
}
