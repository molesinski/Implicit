# Engineering Considerations

These constraints follow from the current implementation and matter when extending or integrating the library. They describe existing behavior, not changes already implemented.

## Mutable model state and caches

User/item maps, factor matrices, and feature vectors are publicly mutable. Norms and regularized Gram matrices are cached lazily and are never invalidated. Changing a matrix after a query or inference call can make later operations combine new factors with old cached values. `Fit` also shares identifier maps with its input `UserItemMatrix`.

Treat a trained or loaded model as a fixed snapshot. A future supported update API would need ownership rules, dimension validation, and cache invalidation together. See [Architecture](architecture.md) and [MatrixFactorizationRecommender](../src/Implicit/MatrixFactorizationRecommender.cs).

## Confidence and loss edge cases

Training expects finite values with a deliberate sign interpretation. Parameter guards do not reject NaN or infinity, and interaction records have no numerical validation. Zero regularization can leave direct systems singular; CG does not explicitly guard a zero or nonfinite `p.DotProduct(Ap)` denominator.

`CalculateLoss` treats negative observations as zero preference and includes their absolute confidence in the normalization denominator. Regression tests compare the reported loss against a direct evaluation of the regularized objective for positive, mixed-sign, and negative-only input. Both loss implementations multiply dimensions in 64-bit integer arithmetic to prevent `U * I` from overflowing a 32-bit integer. Sparse diagonal regression cases cover 46,341 and 65,536 users and items without allocating a dense interaction matrix; see the formula in [ALS implementation](als.md).

The direct and CG implementations each have sequential and parallel private overloads. `Fit` selects the parallel ones. Keep equivalent formulas aligned if maintaining the unused overloads, or assess removing duplication in a separate code change.

## Weighting compatibility and semantics

Weight fitting uses integer outer keys while model building uses string keys. Treat the conversion as an explicit API boundary.

Both `Normalize` implementations use `NETSTANDARD2_0` conditional compilation to snapshot row entries before updating the dictionary in the build consumed by .NET Framework. The library has no direct .NET Framework target, so this compatibility path also applies to any other runtime consuming its `netstandard2.0` assembly. The `netstandard2.1` and .NET 8/9/10 builds enumerate the dictionary directly and avoid the temporary array. Regression tests exercise both transforms through the single-row and multi-user overloads on all test targets, including .NET Framework 4.7.2.

BM25 can turn positive event counts into negative weights; ALS interprets those as zero preference. Empty fitting data, zero average length, and nonfinite normalization results also need caller handling. See [Data and weighting](data-and-weighting.md) before adopting either transform.

## Pool and result ownership

[`ObjectPoolSlim`](../src/Implicit/Utils/ObjectPoolSlim.cs) atomically acquires slots, but returns an instance using an ordinary null check and assignment. Concurrent returns can overwrite a retained slot and reduce reuse. The pool is a best-effort allocation optimization, not a bounded resource allocator.

[`ObjectPoolSlimLease`](../src/Implicit/Utils/ObjectPoolSlimLease.cs) is a public readonly struct. Copying a lease and disposing both copies, or disposing the same lease twice, can return one instance multiple times. Use one lexical `using` owner per lease. `RecommenderResult` adds its own idempotent-disposal guard; the raw lease does not.

If a `RecommenderResult.Create` builder throws, the acquired list is not returned. Holding pooled results indefinitely also prevents reuse, and a returned list retains its capacity after clearing. A pool containing large result lists can therefore retain substantial memory.

Existing enumerators do not detect owner disposal. Never share or enumerate a result after disposal, including through deferred LINQ queries. These ownership cases are not covered by the current tests.

## Ranking and serving scale

Every query scores and sorts the entire candidate population. Filtering and `.Take(k)` happen afterward, and seen-item history is not retained in the model. A serving layer must own filtering and any retrieval index it introduces. Similarity values are query-scaled cosine scores, so fixed thresholds across different queries need careful interpretation.

There is no built-in relevance evaluation, model selection, online update protocol, or cache invalidation service. The synthetic benchmark measures ALS fitting only. Establish workload-specific performance and recommendation-quality measurements before using benchmark results as deployment sizing evidence.

## Regression coverage priorities

The existing 32 cases per framework cover core recommendation behavior, small-matrix factorization, ALS loss reporting, and weighting normalization. Useful additions for future functional changes include:

- Matrix construction with duplicate pairs, cancellation to zero, empty input, and invalid values.
- Unknown-ID behavior and `ComputeItemFeatures`, which has no direct dedicated test.
- Numerical similarity scores, not just ranking or parity groups.
- Result disposal, deferred enumeration, builder exceptions, and pool lease ownership.
- Weight fitting edge cases and persistence on every supported runtime; normalizing known, unknown, and empty rows already has regression coverage.
- Iteration numbering, cancellation, callback exceptions, and numerical precision with large confidence sums.
- Corrupt or truncated persistence data, score/factor round trips, and cross-version fixtures.
- Concurrent inference and cache initialization on an unchanged model.

Choose tests relevant to the code being changed. The current save/load test validates ranking within the same implementation version; it does not establish format stability or resilience to malformed input.
