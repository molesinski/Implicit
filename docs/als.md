# ALS Implementation

[`AlternatingLeastSquaresRecommender`](../src/Implicit/AlternatingLeastSquaresRecommender.cs) factorizes implicit feedback into dense user factors `X` and item factors `Y`. Training alternately holds one matrix fixed and solves for rows of the other.

## Objective and confidence

Let `x_u` and `y_i` be factor vectors, `p_ui` the binary preference, and `c_ui` the nonnegative confidence defined in [Data and weighting](data-and-weighting.md). The normal equations correspond to this regularized objective:

```text
E = sum(u, i) c_ui * (p_ui - dot(x_u, y_i))^2
    + lambda * (sum(u) ||x_u||^2 + sum(i) ||y_i||^2)
```

`lambda` is `Regularization`. Missing entries have `p = 0, c = 1`; positive entries have `p = 1`; negative entries have `p = 0` and confidence equal to their absolute scaled value.

For one user, the implementation starts with the Gram matrix `G = Y^T Y + lambda I` and solves:

```text
A_u = G + sum(observed i) (c_ui - 1) * y_i y_i^T
b_u = sum(positive i) c_ui * y_i
A_u x_u = b_u
```

The Gram matrix accounts for all missing observations without iterating over every missing entry. The item update applies the same calculation to the transposed input.

## Training sequence

1. Reuse input identifier maps and scale the interaction matrix by `Alpha` when needed.
2. Construct the transpose and initialize both factor matrices from a uniform distribution between `0` and `0.01` using the supplied random source.
3. Update all user rows against fixed item factors.
4. Update all item rows against the new user factors.
5. Optionally calculate loss, then invoke the iteration callback.
6. Repeat for the configured iteration count and return the factor matrices.

There is no public warm-start overload, incremental model update, validation-based early stopping, or convergence criterion across ALS iterations. A seeded random source stabilizes initialization; different numerical providers and parallel execution can still affect floating-point results.

## Solver choices

The direct solver explicitly forms `A_u` and calls MathNet's `Solve`; it does not explicitly select a Cholesky decomposition. The conjugate gradient (CG) solver starts from the current factor row and computes matrix-vector products without constructing every row's full coefficient matrix. CG stops after the requested number of steps or when the squared residual drops below `1e-20f`.

`Fit` uses the overloads taking `ParallelOptions`. Sequential private overloads also exist, but are not selected by `Fit`. Setting `MaxDegreeOfParallelism = 1` still uses the parallel overload with one worker.

Temporary feature inference always uses the direct solver, regardless of the training solver. It caches the fixed-side regularized Gram matrix and reuses pooled scratch storage.

## Parameter reference

Source: [AlternatingLeastSquaresParameters](../src/Implicit/AlternatingLeastSquaresParameters.cs).

| Constructor argument | Default | Behavior |
| --- | --- | --- |
| `factors` | `64` | Positive latent dimension. |
| `regularization` | `0.01f` | Nonnegative diagonal penalty. Zero may produce singular systems. |
| `alpha` | `1f` | Positive multiplier for observed training and inference values. |
| `iterations` | `15` | Positive number of complete user/item updates. |
| `useConjugateGradient` | `true` | Selects CG instead of direct solves for training. |
| `conjugateGradientSteps` | `3` | Positive maximum CG steps per row; validated even when CG is disabled. |
| `calculateLossAtIteration` | `false` | Enables an additional loss pass after each iteration. |
| `parallelOptions` | New `ParallelOptions` | Controls parallel loops and cancellation. |
| `random` | `SystemRandomSource.Default` | Controls initial factor generation. |

Properties are read-only except `OnIterationCompleted`; referenced `Random` and `ParallelOptions` objects remain mutable. Numeric guards check bounds, not finiteness: reject NaN and infinity in caller input.

## Progress and cancellation

```csharp
var parameters = new AlternatingLeastSquaresParameters(
    calculateLossAtIteration: true,
    parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = 4 },
    random: new Random(42));

parameters.OnIterationCompleted = context =>
    Console.WriteLine($"Iteration {context.Iteration + 1}: loss={context.Loss}, time={context.Elapsed}");
```

This fragment needs `System.Threading.Tasks`. `Iteration` is zero-based. `Loss` is `null` when calculation is disabled. `Elapsed` covers that iteration's updates and optional loss calculation, excluding initialization and callback execution. The callback runs synchronously after both update loops finish; exceptions propagate out of `Fit`.

Supply a `CancellationToken` through `ParallelOptions`. Training checks cancellation before each iteration and before loss evaluation; the parallel loops also receive the token. Cancellation aborts fitting rather than returning a partial model. Single-row inference has no cancellation parameter.

## Reported loss

The implementation divides its accumulated loss by:

```text
D = sum(observed u, i) abs(Alpha * value_ui) + U * I - nnz
```

`nnz` counts enumerated nonzero interactions. The reported loss is `E / D`, including the regularization term. Positive observations contribute `c_ui * (1 - prediction)^2`; negative observations contribute `c_ui * prediction^2`, because their target preference is zero. The constant term in the expanded squared error therefore applies only to positive observations. Both signs contribute their absolute confidence to `D`, while missing entries contribute one.

Loss is not an offline ranking metric such as recall or NDCG. Large dimensions also require care: `U * I` is evaluated as an integer product before entering the floating-point expression.

## Computational cost

With `F` factors and `N` nonzero interactions, each half-iteration computes a fixed-side Gram matrix. Direct row updates add approximately `O(N F^2)` outer-product work plus `O(rows * F^3)` solves. With `K` CG steps, row updates cost approximately `O((K + 1) * (rows * F^2 + N F))`. These are operation estimates, not measured benchmark results.

Dense factor storage contains `(U + I) * F` floats, approximately `4 * (U + I) * F` bytes before matrix and object overhead. Training additionally holds sparse input and transpose data plus worker-local buffers. See [Development and testing](development.md) for the benchmark configuration.
