# Engineering Considerations

These constraints follow from the current implementation and matter when extending or integrating the library. They describe existing behavior, not changes already implemented.

## Mutable model state and caches

User/item maps, factor matrices, and feature vectors are publicly mutable. Norms and regularized Gram matrices are cached lazily and are never invalidated. Changing a matrix after a query or inference call can make later operations combine new factors with old cached values. `Fit` also shares identifier maps with its input `UserItemMatrix`.

Treat a trained or loaded model as a fixed snapshot. A future supported update API would need ownership rules, dimension validation, and cache invalidation together. See [Architecture](architecture.md) and [MatrixFactorizationRecommender](../src/Implicit/MatrixFactorizationRecommender.cs).

## Confidence and loss edge cases

Training expects finite values with a deliberate sign interpretation. Parameter guards do not reject NaN or infinity, and interaction records have no numerical validation. Zero regularization can leave direct systems singular; CG does not explicitly guard a zero or nonfinite `p.DotProduct(Ap)` denominator.

`CalculateLoss` adds confidence for negative observations even though their target preference is zero. The result differs from the normalized training objective by a dataset-dependent constant. Its dimension product `U * I` uses integer arithmetic and can overflow for large catalogs. These affect loss reporting and deserve focused regression tests before changes; see the derivation in [ALS implementation](als.md).

The direct and CG implementations each have sequential and parallel private overloads. `Fit` selects the parallel ones. Keep equivalent formulas aligned if maintaining the unused overloads, or assess removing duplication in a separate code change.

## Weighting compatibility and semantics

Weight fitting uses integer outer keys while model building uses string keys. Treat the conversion as an explicit API boundary.

Both `Normalize` implementations enumerate a dictionary while assigning new values to existing entries. This works on modern .NET, but overwriting a matched entry invalidates enumeration on .NET Framework and causes `InvalidOperationException` on the next enumerator advance. The project targets .NET Framework through its `netstandard2.0` library, while existing tests do not exercise normalization there. A compatible implementation would need a stable key snapshot or a separate update pass.

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

The existing 12 cases per framework cover core recommendation behavior and small-matrix factorization. Useful additions for future functional changes include:

- Matrix construction with duplicate pairs, cancellation to zero, empty input, and invalid values.
- Unknown-ID behavior and `ComputeItemFeatures`, which has no direct dedicated test.
- Numerical similarity scores, not just ranking or parity groups.
- Result disposal, deferred enumeration, builder exceptions, and pool lease ownership.
- Weight fitting, normalization, and persistence on every supported runtime.
- Iteration numbering, cancellation, callbacks, and the reported loss formula.
- Corrupt or truncated persistence data, score/factor round trips, and cross-version fixtures.
- Concurrent inference and cache initialization on an unchanged model.

Choose tests relevant to the code being changed. The current save/load test validates ranking within the same implementation version; it does not establish format stability or resilience to malformed input.
