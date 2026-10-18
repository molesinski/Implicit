# Architecture

The library separates interaction preparation, latent factor training, and querying. All numerical matrices and vectors use `float` through MathNet.Numerics.

## Repository components

| Location | Responsibility |
| --- | --- |
| `src/Implicit/` | Public model types, sparse input construction, training, ranking, and binary persistence. |
| `src/Implicit/Weights/` | Standalone TF-IDF and BM25 fitting and in-place normalization. |
| `src/Implicit/Utils/` | Reusable object pools and disposable leases. |
| `src/Implicit.Tests/` | Inherited xUnit behavior tests and ALS-specific cases. |
| `src/Implicit.Benchmark/` | Synthetic checkerboard data and ALS training benchmarks. |

## Type hierarchy

[`Recommender`](../src/Implicit/Recommender.cs) declares `Recommend(string)`, `SimilarUsers(string)`, `SimilarItems(string)`, and `Save(Stream)`.

[`MatrixFactorizationRecommender`](../src/Implicit/MatrixFactorizationRecommender.cs) supplies identifier lookup, factor storage, recommendation and similarity scoring, and cached row norms. It adds abstract methods for computing user and item features from interactions.

[`AlternatingLeastSquaresRecommender`](../src/Implicit/AlternatingLeastSquaresRecommender.cs) implements fitting, binary loading and saving, and feature inference. Its public constructor also accepts precomputed factors and index maps; it does not validate their consistency.

[`RecommenderFeatures`](../src/Implicit/RecommenderFeatures.cs) wraps one factor vector and a cached norm. Its constructor is internal: obtain instances through model lookup or inference. [`RecommenderResult`](../src/Implicit/RecommenderResult.cs) exposes ranked identifier/score pairs backed by pooled storage.

## Data flow

```text
Interaction records or nested dictionaries
  -> optional fitted weighting transform
  -> UserItemMatrix.Build
  -> sparse users-by-items matrix and identifier maps
  -> AlternatingLeastSquaresRecommender.Fit
  -> dense user and item factors
  -> recommendations, similarities, temporary feature inference
  -> Save / Load
```

For `U` users, `I` items, and `F` factors, the input matrix has shape `U x I`; user factors have shape `U x F`; item factors have shape `I x F`. Map values are row indices, not external numeric identifiers. IDs are case-sensitive strings under the default dictionary comparer.

## Ownership and lifetime

`UserItemMatrix.Build` allocates new maps and a sparse matrix. `Fit` reuses the input object's maps in the resulting model and allocates new factor matrices. It uses the input matrix directly when `Alpha == 1`; otherwise it makes a scaled matrix, then constructs a transpose for alternating updates. The fitted model keeps maps and factors, but not the interaction matrix or training parameters object.

The maps, matrices, and feature vectors exposed by public properties are mutable. Treat them as immutable after construction: mutating maps can break row alignment; mutating factors can leave cached norms and Gram matrices stale. Feature lookup obtains a row vector from MathNet rather than an API for updating the model.

Results own a pool lease until disposed. Model objects and feature objects do not implement `IDisposable`. Inference leases scratch matrices and returns a separately solved feature vector, so later inference calls do not intentionally overwrite earlier results.

## Concurrency and allocation

Training runs one `Parallel.For` over users, then another over items. Each loop uses worker-local scratch buffers; the fixed side's factor matrix is read while distinct rows on the other side are updated. The caller's `ParallelOptions` controls these loops and loss evaluation.

Inference uses a per-model `ObjectPoolSlim<FactorizationState>`; result lists use a static pool. Each pool has `Environment.ProcessorCount * 5` retention slots, allocates when none are available, and discards returns when full. Retention slots are not a concurrency limit.

Each pooled object has a shared metadata object containing its owner and a 64-bit lease version. A lease remains a readonly struct holding that metadata reference and its version. Copies share one return state, while later leases receive different versions. Metadata is allocated when a new pooled object is created and reused with that object; ordinary unboxed leasing of retained objects adds no per-lease managed allocation.

Norm and Gram matrix caches initialize lazily without explicit synchronization. The pool uses atomic compare-and-exchange operations to acquire and return objects. A return resets the object before publishing it and continues to the next slot if another return claims the same slot first. Pool synchronization does not establish a general thread-safety guarantee for mutable models or shared result instances. Keep model state unchanged, give each request its own result lifetime, and validate concurrent serving behavior for your workload.

See [Engineering considerations](engineering-notes.md) before changing cache or pool ownership.
