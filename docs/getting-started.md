# Getting Started

This example trains a small model, filters already observed items, computes a temporary user profile, and saves and reloads the model. Its tiny dataset demonstrates API usage rather than recommendation quality.

## Create a consumer application

Use a .NET 10 console application for the example. Reference `src/Implicit/Implicit.csproj` from your application to use this checkout, or reference the `Implicit` NuGet package for a released version. Public APIs in older package versions can differ from this checkout.

For repository build prerequisites and commands, see [Development and testing](development.md).

## Train and query

The following is a complete `Program.cs` for a console application referencing the library:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Implicit;

namespace ImplicitExample
{
    internal static class Program
    {
        private static void Main()
        {
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

            using (var recommendations = model.Recommend("alice"))
            {
                foreach (var item in recommendations.Where(x => !seen.Contains(x.Key)).Take(5))
                {
                    Console.WriteLine($"{item.Key}: {item.Value:F4}");
                }
            }

            var history = new Dictionary<string, float>
            {
                ["book-a"] = 2f,
                ["book-c"] = 1f,
            };
            var temporaryUser = model.ComputeUserFeatures(history);

            if (temporaryUser is not null)
            {
                using var recommendations = model.Recommend(temporaryUser);
                var topItems = recommendations.Take(5).ToArray();
                Console.WriteLine($"Temporary profile candidates: {topItems.Length}");
            }

            using var stream = new MemoryStream();
            model.Save(stream);
            stream.Position = 0;
            var restored = AlternatingLeastSquaresRecommender.Load(stream);

            using var similar = restored.SimilarItems("book-a");
            var neighbors = similar.Where(x => x.Key != "book-a").Take(5).ToArray();
            Console.WriteLine($"Other item candidates: {neighbors.Length}");
        }
    }
}
```

## Integration rules

- Keep interaction history separately if you need to exclude seen items. The fitted model does not retain the training interaction matrix.
- Dispose every nonempty `RecommenderResult`. Materialize deferred LINQ queries inside the `using` scope if their results must outlive it.
- Treat scores as ranking values. Recommendations return dot products; similarity queries divide by the candidate norm only.
- Handle unknown identifiers: lookup-based queries return an empty result; feature lookups return `null`.
- Apply the same preprocessing to training and inference histories. `ComputeUserFeatures` applies the model's `Alpha` internally, so do not multiply the history by it again.
- Keep maps and factor matrices unchanged after fitting. Cached values are not invalidated by external mutation.

See [API behavior](api.md) for ranking and inference details, and [Data and weighting](data-and-weighting.md) for the meaning of interaction values.
