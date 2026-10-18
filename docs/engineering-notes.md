# Engineering Considerations

These constraints follow from the current implementation and matter when extending or integrating the library. They describe existing behavior, not changes already implemented.

## Mutable model state and caches

User/item maps, factor matrices, and feature vectors are publicly mutable. Norms and regularized Gram matrices are cached lazily and are never invalidated. Changing a matrix after a query or inference call can make later operations combine new factors with old cached values. `Fit` also shares identifier maps with its input `UserItemMatrix`.

Treat a trained or loaded model as a fixed snapshot. A future supported update API would need ownership rules, dimension validation, and cache invalidation together. See [Architecture](architecture.md) and [MatrixFactorizationRecommender](../src/Implicit/MatrixFactorizationRecommender.cs).

## Confidence and loss edge cases

Training expects finite values with a deliberate sign interpretation. Parameter guards do not reject NaN or infinity, and interaction records have no numerical validation. Zero regularization can leave direct systems singular; CG does not explicitly guard a zero or nonfinite `p.DotProduct(Ap)` denominator.

`CalculateLoss` treats negative observations as zero preference and includes their absolute confidence in the normalization denominator. Regression tests compare the reported loss against a direct evaluation of the regularized objective for positive, mixed-sign, and negative-only input. Both loss implementations calculate the missing-pair count `U * I - nnz` in 64-bit integer arithmetic before adding the double-precision confidence sum. Regressions cover confidence magnitudes from `1e-7` to `1e-20` with both solvers and signs, including fully observed data where adding and then subtracting the pair count could erase confidence. Sparse diagonal regression cases cover 46,341 and 65,536 users and items without allocating a dense interaction matrix; see the formula in [ALS implementation](als.md).

The direct and CG implementations each have sequential and parallel private overloads. `Fit` selects the parallel ones. Keep equivalent formulas aligned if maintaining the unused overloads, or assess removing duplication in a separate code change.

## Weighting compatibility and semantics

Weight fitting uses integer outer keys while model building uses string keys. Treat the conversion as an explicit API boundary.

Both `Normalize` implementations use `NETSTANDARD2_0` conditional compilation to snapshot row entries before updating the dictionary in the build consumed by .NET Framework. The library has no direct .NET Framework target, so this compatibility path also applies to any other runtime consuming its `netstandard2.0` assembly. The `netstandard2.1` and .NET 8/9/10 builds enumerate the dictionary directly and avoid the temporary array. Regression tests exercise both transforms through the single-row and multi-user overloads on all test targets, including .NET Framework 4.7.2.

BM25 can turn positive event counts into negative weights; ALS interprets those as zero preference. Empty fitting data, zero average length, and nonfinite normalization results also need caller handling. See [Data and weighting](data-and-weighting.md) before adopting either transform.

## Pool and result ownership

[`ObjectPoolSlim`](../src/Implicit/Utils/ObjectPoolSlim.cs) uses atomic compare-and-exchange operations for both acquisition and return. After resetting an object, a return claims an empty slot only if it is still empty; a competing return cannot overwrite the retained object. If another thread wins that slot, the return continues scanning. A full pool still discards excess returns after resetting them. Retention capacity does not limit the number of simultaneously leased objects.

[`ObjectPoolSlimLease`](../src/Implicit/Utils/ObjectPoolSlimLease.cs) is a public readonly struct holding the pool and instance references. It has no shared disposal state or per-instance metadata allocation. Disposing a default lease is a no-op. The default resetter calls `IResettable.Reset()` when supported; a custom resetter replaces this behavior. If resetting throws, the instance is not republished. Prefer non-throwing resetters and do not retry disposal after a reset failure.

Keep one logical owner and use `using`. Dispose each acquired lease exactly once, including across copied or boxed values. Repeated disposal can publish the same instance more than once or reset it while another caller uses it. Do not access `Instance` after disposal. `RecommenderResult` retains its own disposal guard and enumerator lifetime checks; inference uses one scoped lease per call.

If a `RecommenderResult.Create` builder throws, the factory releases the lease and rethrows the original exception. The list is cleared before it can be reused. On success, ownership transfers to the result until disposal. Holding pooled results indefinitely prevents reuse, and a returned list retains its capacity after clearing. A pool containing large result lists can therefore retain substantial memory.

Result enumerators retain an owner reference and check disposal before advancing, reading the current item, or resetting. Copies and boxed enumerators share that lifetime check, preventing access to a list reused by another result. The enumerator remains a struct; the owner reference needs no additional heap object. Regression tests cover disposal at different iteration stages, pool reuse, copies, interfaces, and deferred LINQ enumeration. The shared `Empty` result remains reusable after disposal. These checks do not synchronize concurrent enumeration and disposal; callers must keep result disposal outside active enumeration.

## Ranking and serving scale

Every query scores and sorts the entire candidate population. Filtering and `.Take(k)` happen afterward, and seen-item history is not retained in the model. A serving layer must own filtering and any retrieval index it introduces. Similarity values are query-scaled cosine scores, so fixed thresholds across different queries need careful interpretation.

There is no built-in relevance evaluation, model selection, online update protocol, or cache invalidation service. The synthetic benchmark measures ALS fitting only. Establish workload-specific performance and recommendation-quality measurements before using benchmark results as deployment sizing evidence.

## Regression coverage priorities

The existing 65 cases on .NET Framework and 66 on each modern .NET target cover core recommendation behavior, small-matrix factorization, high-confidence direct training and feature inference, ALS loss reporting, weighting normalization, result pool ownership and enumerator lifetime, concurrent pool returns, resetting, and default lease disposal. Factorization tests explicitly reject NaN and infinity; high-confidence regressions also compare factors against an analytical solution. Modern .NET targets also verify allocation-free reuse of retained objects. Useful additions for future functional changes include:

- Matrix construction with duplicate pairs, cancellation to zero, empty input, and invalid values.
- Unknown-ID behavior and mixed-sign or duplicate input to `ComputeUserFeatures` and `ComputeItemFeatures`.
- Numerical similarity scores, not just ranking or parity groups.
- Weight fitting edge cases and persistence on every supported runtime; normalizing known, unknown, and empty rows already has regression coverage.
- Iteration numbering, cancellation, callback exceptions, and numerical precision with large confidence sums.
- Corrupt or truncated persistence data, score/factor round trips, and cross-version fixtures.
- Concurrent inference and cache initialization on an unchanged model.

Choose tests relevant to the code being changed. The current save/load test validates ranking within the same implementation version; it does not establish format stability or resilience to malformed input.
