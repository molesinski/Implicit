# Data and Weighting

Training input associates a string user ID, a string item ID, and a single-precision value. The sign expresses preference; the magnitude becomes confidence after scaling by `Alpha`.

Sources: [UserItemValue](../src/Implicit/UserItemValue.cs), [UserItemMatrix](../src/Implicit/UserItemMatrix.cs), [UserItemValueExtensions](../src/Implicit/UserItemValueExtensions.cs), and [weight implementations](../src/Implicit/Weights/).

## Building the interaction matrix

`UserItemMatrix.Build` accepts either `IEnumerable<UserItemValue>` or `Dictionary<string, Dictionary<string, float>>`. Rows represent users and columns represent items.

- Exact zero inputs are skipped before assigning indices.
- IDs receive consecutive indices in first-encounter order among nonzero records.
- Repeated user/item pairs in a record sequence are summed.
- Users and items with only zero inputs are omitted.
- Positive and negative duplicates can cancel to zero after their IDs have already been registered, leaving an indexed row or column with no nonzero entries.
- `UserItemValue` rejects null IDs, but allows empty strings and does not validate finite values. The default value of this record struct bypasses its constructor checks.

`UserItemValueExtensions.ToUserItems(records)` is a static helper despite its class name; its parameter has no `this` modifier. It aggregates duplicates into nested string dictionaries and retains zero totals. Building a matrix from that aggregated dictionary may omit IDs that direct record construction would have registered before cancellation.

Validate finite values and meaningful identifiers at the application boundary. The library does not define a useful fallback model for an empty dataset.

## Confidence semantics

For an aggregated nonzero value `v`, training uses `s = Alpha * v`:

| Value | Target preference | Confidence |
| --- | --- | --- |
| Positive | `1` | `s` |
| Negative | `0` | `abs(s)` |
| Missing or zero | `0` | `1` |

The implementation does not add one to observed confidence. For example, `v = 2` and `Alpha = 10` produce confidence `20`, not `21`. Negative feedback targets zero rather than minus one. A nonzero magnitude below one assigns less weight than a missing observation.

Choose how events become these values before fitting: purchase counts, time spent, and explicit dislikes require application-specific aggregation. Preserve that interpretation when computing temporary features.

## Fitting weights

`TFIDFWeight.Fit` and `BM25Weight.Fit` accept `Dictionary<int, Dictionary<string, float>>`: their outer keys are integers, unlike `UserItemMatrix.Build`. The integers identify fitting rows; they do not need to match the model's internal user indices.

Given an existing string-keyed `userItems` dictionary:

```csharp
var fittingRows = userItems.Values
    .Select((items, index) => new { Index = index, Items = items })
    .ToDictionary(x => x.Index, x => x.Items);

var weight = Implicit.Weights.TFIDFWeight.Fit(fittingRows);
weight.Normalize(userItems);
var matrix = UserItemMatrix.Build(userItems);
```

This fragment needs `System.Linq`. `Fit` computes statistics without normalizing the dictionaries. `Normalize` mutates values in place; clone nested dictionaries first if original values must be retained. In the `netstandard2.0` build, both transforms snapshot the row entries for .NET Framework compatibility. Other builds iterate the dictionary directly without allocating that array. Applying normalization twice applies the transform twice. Unknown item IDs are left unchanged.

Both weight implementations count dictionary entries when computing document frequency, including zero-valued entries. Remove unwanted zero entries before fitting statistics. Neither transform is automatically invoked by ALS, stored in the ALS model, or reapplied during inference.

## TF-IDF

For `N` fitting rows and `df(i)` rows containing item `i`, the implementation computes:

```text
idf(i) = ln(N / df(i))
normalized(u, i) = value(u, i) * idf(i)
```

There is no smoothing, square-root term frequency, or vector normalization. An item appearing in every row receives zero weight and may disappear when the matrix is built.

## BM25

Defaults are `k1 = 100` and `b = 0.8`. Let `L(u)` be the sum of a row's values, and `averageLength` the mean of those row sums at fitting time:

```text
idf(i) = ln((N - df(i) + 0.5) / (df(i) + 0.5))
lengthNorm(u) = 1 - b + b * L(u) / averageLength
tf(u, i) = value(u, i) * (k1 + 1) / (value(u, i) + k1 * lengthNorm(u))
normalized(u, i) = tf(u, i) * idf(i)
```

Frequent items can have negative IDF. Feeding those weights into ALS makes them zero-preference observations, which may differ from the intended meaning of frequent positive events. Review the sign distribution before training.

BM25 leaves exact zero input values unchanged and retains their dictionary entries. It skips the term-frequency division for these entries, so zeros remain zero even with `k1 = 0`, `b = 1`, or zero average fitting length. Nonzero entries in the same row are still normalized.

The constructor path rejects negative `k1` and `b`, but does not cap `b` at one or guard against nonfinite parameters, zero average length, or zero denominators. Empty fitting input fails when computing its average. BM25 computes the row sum before modifying any values, including entries whose IDs are unknown to the fitted transform.
