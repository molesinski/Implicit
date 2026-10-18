# Implicit

[![CI](https://github.com/molesinski/Implicit/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/molesinski/Implicit/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/dt/implicit.svg)](https://www.nuget.org/packages/implicit)
[![NuGet](https://img.shields.io/nuget/v/implicit.svg)](https://www.nuget.org/packages/implicit)
[![License](https://img.shields.io/github/license/molesinski/implicit.svg)](https://github.com/molesinski/implicit/blob/master/LICENSE)

Fast .NET collaborative filtering for implicit feedback datasets.

Developer documentation: [getting started, architecture, API, and implementation notes](docs/README.md).

## Highlights

Implicit learns user and item representations from interactions such as views, purchases, or listening
history. A trained model can recommend items, find similar users or items, and infer a temporary profile
from a new interaction history.

Main features are:

- Alternating Least Squares (ALS) with conjugate gradient or direct solving
- Parallel user and item updates during training
- Positive and negative feedback with configurable confidence scaling
- Recommendations and user/item similarity queries
- Feature inference for new users and items against an existing model
- Optional TF-IDF and BM25 preprocessing
- Binary persistence for models and fitted weighting transforms
- Pooled result storage for repeated queries
- MathNet.Numerics linear algebra with support for its native providers

ALS is the only implemented recommendation algorithm. TF-IDF and BM25 transform interaction values;
they are not standalone item-neighbor recommenders. Models are trained in batches, with no incremental
update or public training-resumption API. Applications supply interaction data, candidate filtering,
and recommendation-quality evaluation.

## Installation

Add the package to your application:

```shell
dotnet add package Implicit
```

The library targets .NET Standard 2.0/2.1 and .NET 8/9/10. The .NET Standard 2.0 build also supports
.NET Framework consumers; the repository tests it on .NET Framework 4.7.2.

The examples below use modern C# and the API in this checkout. To try unreleased changes, reference
[src/Implicit/Implicit.csproj](src/Implicit/Implicit.csproj) from a console application instead of using
the published package. Use the `Implicit` namespace for models and `Implicit.Weights` for preprocessing.

## A Quick Example

This complete console example builds an interaction matrix, trains a model, and recommends books that
Alice has not already read. The small dataset demonstrates the API rather than recommendation quality.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Implicit;

var interactions = new[]
{
    new UserItemValue("alice", "book-a", 3f),
    new UserItemValue("alice", "book-b", 1f),
    new UserItemValue("bob", "book-a", 2f),
    new UserItemValue("bob", "book-c", 4f),
    new UserItemValue("carol", "book-b", 2f),
    new UserItemValue("carol", "book-c", 3f),
};

var matrix = UserItemMatrix.Build(interactions);
var parameters = new AlternatingLeastSquaresParameters(
    factors: 3,
    regularization: 0.1f,
    alpha: 10f,
    iterations: 15,
    random: new Random(42));

var model = AlternatingLeastSquaresRecommender.Fit(matrix, parameters);
var seen = new HashSet<string>(
    interactions.Where(x => x.UserId == "alice").Select(x => x.ItemId));

// Dispose the result to return its list to the pool.
using (var recommendations = model.Recommend("alice"))
{
    foreach (var item in recommendations.Where(x => !seen.Contains(x.Key)).Take(5))
    {
        Console.WriteLine($"{item.Key}: {item.Value:F4}");
    }
}
```

Here `book-c` is the only unseen candidate. Result keys are item IDs and values are ranking scores,
not probabilities or predicted star ratings. The model does not retain interaction history, so keep
that history separately when filtering previously seen items.

The following examples build on `interactions`, `matrix`, `parameters`, and `model` from this example.

## Interaction Data

`UserItemMatrix.Build` accepts either a sequence of `UserItemValue` records or a
`Dictionary<string, Dictionary<string, float>>` mapping user IDs to item values. It builds a sparse
matrix with users as rows and items as columns. Duplicate records for the same pair are summed;
exact zero inputs are skipped. Users and items represented only by zero inputs are omitted.

The sign of an aggregated value expresses preference, while its magnitude controls confidence:

| Input value | Target preference | Confidence used by ALS |
| --- | --- | --- |
| Positive `v` | `1` | `Alpha * v` |
| Negative `v` | `0` | `abs(Alpha * v)` |
| Missing or zero | `0` | `1` |

Observed confidence does not include an added baseline of one: a value of `2` with `Alpha = 10`
produces confidence `20`. Negative observations target zero, not minus one. For example, an explicit
dislike can be represented by a negative value with a deliberately chosen magnitude.

Choose an event aggregation policy before training, and validate finite values and meaningful IDs in
your application. See [data and weighting](docs/data-and-weighting.md) for duplicate cancellation,
zero entries, and preprocessing details.

## Training Configuration

Pass `AlternatingLeastSquaresParameters` to `Fit` to control model size, regularization, and solving:

| Argument | Default | Purpose |
| --- | --- | --- |
| `factors` | `64` | Number of latent features per user and item. |
| `regularization` | `0.01f` | Nonnegative penalty on factor magnitudes. |
| `alpha` | `1f` | Positive multiplier for observed confidence values. |
| `iterations` | `15` | Number of complete user/item update passes. |
| `useConjugateGradient` | `true` | Use CG; set to `false` for direct solving. |
| `conjugateGradientSteps` | `3` | Maximum CG steps per row update. |
| `calculateLossAtIteration` | `false` | Calculate loss after each iteration. |
| `parallelOptions` | New `ParallelOptions` | Control parallelism and cancellation. |
| `random` | MathNet default random source | Initialize factor matrices. |

To report progress and limit training concurrency:

```csharp
var monitoredParameters = new AlternatingLeastSquaresParameters(
    factors: 32,
    regularization: 0.1f,
    alpha: 10f,
    calculateLossAtIteration: true,
    parallelOptions: new System.Threading.Tasks.ParallelOptions
    {
        MaxDegreeOfParallelism = 4,
    },
    random: new Random(42));

monitoredParameters.OnIterationCompleted = context =>
    Console.WriteLine(
        $"Iteration {context.Iteration + 1}: loss={context.Loss}, time={context.Elapsed}");

var monitoredModel = AlternatingLeastSquaresRecommender.Fit(matrix, monitoredParameters);
```

`Iteration` is zero-based, and `Loss` is null when loss calculation is disabled. Calculating loss adds
work after every iteration; it is the normalized training objective, not a ranking-quality metric.
Supply a cancellation token through `ParallelOptions.CancellationToken` to abort fitting.

A random seed stabilizes initialization, but numerical providers and parallel execution can affect
floating-point results. Treat trained or loaded models as fixed snapshots: publicly exposed maps and
factor matrices are mutable, but changing them does not invalidate cached norms or Gram matrices.
See [ALS implementation](docs/als.md) for the objective and solver behavior.

## Recommendations and Similarity

Query results are sorted by descending score. Lookup-based queries for unknown IDs return an empty
result; feature lookups return null.

| Method | Candidates |
| --- | --- |
| `Recommend(userId)` | All model items, scored against the user's factors. |
| `SimilarUsers(userId)` | All model users, including the queried user. |
| `SimilarItems(itemId)` | All model items, including the queried item. |
| `GetUserFeatures(userId)` | A known user's factors, or null. |
| `GetItemFeatures(itemId)` | A known item's factors, or null. |

Filter out the query item when displaying neighbors:

```csharp
using (var similar = model.SimilarItems("book-a"))
{
    foreach (var item in similar.Where(x => x.Key != "book-a").Take(5))
    {
        Console.WriteLine($"Similar book: {item.Key}, score={item.Value:F4}");
    }
}
```

Recommendation scores are user/item dot products. Similarity scores divide the dot product by the
candidate's norm only. For a fixed nonzero query this preserves cosine-similarity ordering, but scores
are scaled by the query norm and need not fall between minus one and one. Equal-score ordering is
unspecified. See [API behavior](docs/api.md) for scoring and query contracts.

### New Users and Items

`ComputeUserFeatures` infers a temporary user vector from interactions with known items. It does not
insert a user into the model or retrain existing factors. Unknown item IDs and zero values are skipped;
if there are no accepted interactions, it returns null.

```csharp
var history = new Dictionary<string, float>
{
    ["book-a"] = 2f,
    ["book-c"] = 1f,
};
var profile = model.ComputeUserFeatures(history);

if (profile is not null)
{
    using var recommendations = model.Recommend(profile);
    var topItems = recommendations
        .Where(x => !history.ContainsKey(x.Key))
        .Take(5)
        .ToArray();

    foreach (var item in topItems)
    {
        Console.WriteLine($"For the new reader: {item.Key}, score={item.Value:F4}");
    }
}
```

`ComputeItemFeatures` performs the symmetric operation using known user IDs. Pass inferred features
to `SimilarItems`, `SimilarUsers`, or `Recommend` as appropriate. `RecommenderFeatures.SimilarFeatures`
also scores a supplied collection of candidate features from the same latent space.

Apply the same preprocessing used for training. Feature inference applies the model's `Alpha`
internally, so do not scale input values by alpha again.

### Result Lifetime

`RecommenderResult` implements `IReadOnlyList<KeyValuePair<string, float>>` and `IDisposable`.
Always dispose results after reading them. Use `ToArray()` or `ToList()` inside the `using` scope when
data must outlive the result; returning a deferred query such as `result.Take(10)` is not sufficient.

For pooled results, accessing the count or indexer, obtaining an enumerator, or using an existing
enumerator after result disposal throws `ObjectDisposedException`. This also applies to copied
and boxed enumerators. The shared `RecommenderResult.Empty` remains readable after disposal.
Keep disposal outside active enumeration; lifetime checks do not synchronize concurrent access.

## Weighting Interactions

TF-IDF and BM25 can adjust interaction strengths before training. They are optional and are never
automatically applied by ALS. The following example aggregates the original records and fits TF-IDF:

```csharp
var userItems = UserItemValueExtensions.ToUserItems(interactions);
var fittingRows = userItems.Values
    .Select((items, index) => new { Index = index, Items = items })
    .ToDictionary(x => x.Index, x => x.Items);

var weight = Implicit.Weights.TFIDFWeight.Fit(fittingRows);
weight.Normalize(userItems);

var weightedMatrix = UserItemMatrix.Build(userItems);
var weightedModel = AlternatingLeastSquaresRecommender.Fit(weightedMatrix, parameters);
```

The fitting API uses integer outer keys; matrix construction uses string user IDs. `Fit` collects
statistics, while `Normalize` changes dictionary values in place. Clone the nested dictionaries if
you need to retain their original values. Applying normalization twice repeats the transform.

TF-IDF multiplies values by `ln(userCount / itemFrequency)`. An item present in every fitting row
receives zero weight. To use BM25 instead, replace the fitting call with
`Implicit.Weights.BM25Weight.Fit(fittingRows, k1: 100f, b: 0.8f)`.
BM25 can produce negative weights for frequent items, which ALS interprets as zero-preference feedback.
Review that behavior against your event semantics before using it.

Retain the fitted transform for inference and normalize new histories once before calling
`weightedModel.ComputeUserFeatures`. Unknown items are left unchanged by normalization. Both transforms
support `Save(Stream)` and their own static `Load(Stream)` methods; save them separately from the model.
See [data and weighting](docs/data-and-weighting.md) for formulas and edge cases.

## Saving and Loading Models

Persist a trained model to a stream and load it without fitting again:

```csharp
using (var output = System.IO.File.Create("model.bin"))
{
    model.Save(output);
}

using (var input = System.IO.File.OpenRead("model.bin"))
{
    var restored = AlternatingLeastSquaresRecommender.Load(input);
    using var recommendations = restored.Recommend("alice");
    var topItems = recommendations.Where(x => !seen.Contains(x.Key)).Take(5).ToArray();
    Console.WriteLine($"Restored model candidates: {topItems.Length}");
}
```

Saving preserves IDs, factors, regularization, and alpha. It does not preserve interaction history,
weighting transforms, or training configuration. Streams remain open and are read or written at their
current position; rewind a `MemoryStream` before loading from it.

The binary format has no version header or integrity check. Load trusted model artifacts and retain
the producing library version and preprocessing settings alongside them. See [binary persistence](docs/persistence.md)
for the format and compatibility constraints.

## Performance and Benchmarks

Training uses MathNet.Numerics and parallel row updates. MathNet's native providers can accelerate
linear algebra; the repository benchmark compares its managed provider with native Intel MKL on
Windows x64. See the [benchmark configuration](docs/development.md#benchmarks) for provider setup.

Queries score and sort the full candidate population before application filters or `Take(k)` run.
Taking ten results therefore does not reduce scoring work. Factor matrices contain roughly
`4 * (userCount + itemCount) * factors` bytes of float data, excluding object overhead, caches,
training data, and working buffers.

Result lists are pooled to reduce repeated buffer allocation. Pool misses, larger result capacities,
and result wrapper objects still allocate. Dispose results promptly to make their lists available
for reuse; retained lists keep their capacity after clearing.

Run the included training benchmarks from the repository root:

```shell
dotnet run --project src/Implicit.Benchmark/Implicit.Benchmark.csproj -c Release -- --filter '*AlternatingLeastSquares*'
```

The workload uses a synthetic 1024-by-1024 interaction matrix, 64 factors, and CG training. It measures
fitting time and allocations, not recommendation latency or relevance. Benchmark representative data
and record the runtime, numerical provider, and concurrency settings when comparing results.

## Development

Use the .NET SDK selected by `global.json`. From the repository root:

```shell
dotnet restore Implicit.slnx
dotnet build Implicit.slnx -c Release --no-restore
dotnet test src/Implicit.Tests/Implicit.Tests.csproj -c Release --no-build
```

The full test matrix requires Windows with .NET Framework 4.7.2-compatible execution and .NET 8/9/10
runtimes. For a focused run, use
`dotnet test src/Implicit.Tests/Implicit.Tests.csproj -c Release -f net10.0`.

See the [contributor guide](AGENTS.md) for coding conventions and
[development and testing](docs/development.md) for packaging, analyzers, and benchmark workflows.
The [developer documentation](docs/README.md) also covers architecture and implementation constraints.

## Based On and Planned Work

This project is based on the Python [Implicit](https://github.com/benfred/implicit) library by
[Ben Frederickson](https://github.com/benfred).

The ALS implementation follows the approaches described in
[Collaborative Filtering for Implicit Feedback Datasets](http://yifanhu.net/PUB/cf.pdf) and
[Applications of the Conjugate Gradient Method for Implicit Feedback Collaborative Filtering](https://pdfs.semanticscholar.org/bfdf/7af6cf7fd7bb5e6b6db5bbd91be11597eaf0.pdf).

Previously listed planned algorithms remain unimplemented:

- [Bayesian Personalized Ranking](https://arxiv.org/pdf/1205.2618.pdf)
- Standalone item-item nearest-neighbor models using cosine, TF-IDF, or BM25 similarity

## License

- Implicit, [MIT License](LICENSE)
