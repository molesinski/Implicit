# API Behavior

The public API operates on identifier maps and dense factors. The main implementation is in [MatrixFactorizationRecommender](../src/Implicit/MatrixFactorizationRecommender.cs), with fitting and inference in [AlternatingLeastSquaresRecommender](../src/Implicit/AlternatingLeastSquaresRecommender.cs).

## Model creation and exposed state

`AlternatingLeastSquaresRecommender.Fit(UserItemMatrix, parameters)` trains a model; omitted or null parameters use defaults. `Load(Stream)` reconstructs one from binary data. Both reject null inputs.

The public constructor accepts `factors`, `regularization`, `alpha`, user and item maps, and factor matrices. Callers using this constructor must ensure dimensions, row indices, parameter values, and factor values are consistent; it bypasses `AlternatingLeastSquaresParameters` validation.

`Factors`, `Regularization`, and `Alpha` expose model settings. `Users` and `Items` expose ID-to-row dictionaries; `UserFactors` and `ItemFactors` expose MathNet matrices. These properties do not return immutable wrappers. Mutating them is incompatible with cached norms and Gram matrices.

## Query contracts

| Method | Return behavior |
| --- | --- |
| `Recommend(string userId)` | All model items sorted by descending user/item dot product; unknown user returns `RecommenderResult.Empty`. |
| `SimilarUsers(string userId)` | All model users sorted by similarity, including the query user; unknown user returns empty. |
| `SimilarItems(string itemId)` | All model items sorted by similarity, including the query item; unknown item returns empty. |
| `GetUserFeatures(string userId)` | Feature vector and cached norm for a known user; otherwise `null`. |
| `GetItemFeatures(string itemId)` | Feature vector and cached norm for a known item; otherwise `null`. |
| `Recommend(RecommenderFeatures)` | Scores all stored items against the supplied vector. |
| `SimilarUsers(RecommenderFeatures)` | Scores all stored users against the supplied vector. |
| `SimilarItems(RecommenderFeatures)` | Scores all stored items against the supplied vector. |
| `RecommenderFeatures.SimilarFeatures(candidates)` | Scores the supplied ID/feature sequence and sorts it descending. |

Null IDs, null feature objects, and null candidate sequences throw `ArgumentNullException`. Feature vectors must belong to a compatible latent space and have the expected dimension. Equal dimensionality alone does not make independently trained models compatible.

## Scores and ranking

Recommendation scores are `dot(query, candidate)`. Similarity scores are:

```text
score(query, candidate) = dot(query, candidate) / ||candidate||
```

The query norm is not divided out. For a fixed nonzero query, this gives the same mathematical ordering as cosine similarity, but scores are scaled by the query norm and need not lie between minus one and one. Scores from different queries are not normalized for direct comparison. Zero candidate norms are replaced with `1e-10f`.

Queries enumerate every candidate, store all scores, then sort the full list. For `C` candidates and `F` factors, that means approximately `O(C F + C log C)` time and `O(C)` result storage. Calling `.Take(k)` does not reduce scoring or sorting work. Equal-score ordering is unspecified.

The library does not filter seen, unavailable, or blocked items. Similarity methods do not remove the query entity. Apply application filters before taking the final number of results, as shown in [Getting started](getting-started.md).

## Temporary features

`ComputeUserFeatures(IEnumerable<KeyValuePair<string, float>> items)` infers a user vector against fixed item factors. `ComputeItemFeatures(IEnumerable<KeyValuePair<string, float>> users)` performs the symmetric operation against fixed user factors.

Both methods:

1. Reject a null sequence.
2. Skip exact zeros and unknown IDs.
3. Sum repeated known IDs into a sparse vector.
4. Return `null` if no known, individually nonzero entry was accepted.
5. Apply the model's `Alpha` and solve the regularized least-squares system.

If accepted duplicate entries cancel to zero, inference still attempts a solve rather than returning `null`. A singular system may fail. Negative values use the same zero-preference semantics as training.

The inferred vector does not insert an ID, replace trained factors, or retrain the model. A new user can receive recommendations based on known items; a new item can be compared with stored items through `SimilarItems(features)`. To compare a custom collection of inferred items, keep their features and call `SimilarFeatures`.

## Result lifetime

[`RecommenderResult`](../src/Implicit/RecommenderResult.cs) implements `IReadOnlyList<KeyValuePair<string, float>>` and `IDisposable`. Its list is borrowed from a pool and returned, cleared, on disposal.

```csharp
KeyValuePair<string, float>[] snapshot;
using (var result = model.Recommend("alice"))
{
    snapshot = result.Take(10).ToArray();
}
// snapshot remains valid here.
```

For a pooled result, accessing `Count`, its indexer, or obtaining an enumerator after disposal throws `ObjectDisposedException`. Previously acquired enumerators also throw from `MoveNext`, `Current`, and `Reset`, including copied and boxed enumerators, even if another result has reused the pooled list. Disposing an enumerator remains safe after its owner is disposed. LINQ queries are usually deferred, so returning `result.Take(10)` from a disposed scope is invalid.

`Dispose` on the result object is idempotent. The shared `Empty` result has no pool lease and remains readable after disposal. Always using a `using` scope handles both cases correctly.

The public `RecommenderResult.Create(builder)` invokes a callback with a mutable list. The callback is responsible for ordering and must not retain the list reference. If it throws, `Create` releases the lease, clearing and returning the list to the pool, then rethrows the original exception. On success, the result owns the lease until it is disposed.
