# Development and Testing

Use the repository's SDK and analyzer configuration when changing the library. [`AGENTS.md`](../AGENTS.md) contains the concise contribution guide; this chapter explains the build matrix, test coverage, benchmark workload, and packaging flow.

## Toolchain and targets

[`global.json`](../global.json) selects SDK `10.0.100` with `latestFeature` roll-forward. [`Directory.Build.props`](../Directory.Build.props) selects C# 14, nullable reference analysis, implicit usings, .NET analysis level 10, and build-time code style enforcement. All warnings are errors. StyleCop.Analyzers `1.3.109-beta` is shared across projects.

| Project | Targets | Principal dependencies |
| --- | --- | --- |
| Library | `netstandard2.0`, `netstandard2.1`, `net8.0`, `net9.0`, `net10.0` | MathNet.Numerics `5.0.0`; Microsoft.Bcl.Numerics `10.0.13` only for `netstandard2.0`. |
| Tests | `net472`, `net8.0`, `net9.0`, `net10.0` | xUnit `2.9.3`, runner `4.0.1`, Microsoft.NET.Test.Sdk `18.11.0`. |
| Benchmarks | `net10.0` | BenchmarkDotNet `0.15.8`, MathNet.Numerics.MKL.Win-x64 `3.0.0`. |

The full test matrix needs Windows and .NET Framework 4.7.2-compatible execution, plus .NET 8, 9, and 10 runtimes. Installing only a .NET 10 SDK does not supply every older runtime. A .NET Framework targeting pack or reference assemblies must also be available for compilation. Run a single modern target for a narrower development check.

## Commands

Run from the repository root:

```powershell
dotnet restore Implicit.slnx
dotnet build Implicit.slnx -c Release --no-restore
dotnet test src/Implicit.Tests/Implicit.Tests.csproj -c Release --no-build
```

For a focused test run that builds the selected target:

```powershell
dotnet test src/Implicit.Tests/Implicit.Tests.csproj -c Release -f net10.0
dotnet test src/Implicit.Tests/Implicit.Tests.csproj -c Release -f net10.0 --filter FullyQualifiedName~Factorize
```

`--no-build` requires matching Release outputs to exist already. Generated `bin/`, `obj/`, `TestResults/`, `artifacts/`, and `BenchmarkDotNet.Artifacts/` directories are ignored by Git.

Allocation checks have `[Trait("Category", "Performance")]` and remain part of the default test run. Use `--filter 'Category=Performance'` to run them alone, or `--filter 'Category!=Performance'` to exclude them during a focused functional check. The allocation check is available on .NET 8/9/10.

## Existing tests

The concrete ALS test class inherits shared tests from two abstract base classes, yielding 18 cases per target, including six high-confidence direct training and inference regressions. Eleven deterministic query scoring cases, fourteen feature inference cases, nine matrix construction and interaction aggregation cases, twenty weighting cases, twenty-four loss cases, three result ownership cases, nine enumerator cases, two pool cases, and one default lease case bring the total to 111 per target. A retained-lease allocation test runs on .NET 8/9/10, bringing those targets to 112 cases each:

| Source | Behavior exercised |
| --- | --- |
| [RecommenderTest](../src/Implicit.Tests/RecommenderTest.cs) | Held-out diagonal recommendations and parity-based user/item similarity. |
| [MatrixFactorizationRecommenderTest](../src/Implicit.Tests/MatrixFactorizationRecommenderTest.cs) | Reconstructed-user ranking, user inference from one item, and custom feature similarity. |
| [MatrixFactorizationRecommenderScoreTest](../src/Implicit.Tests/MatrixFactorizationRecommenderScoreTest.cs) | Fixed-factor recommendation and similarity scores, ID/feature overloads, custom candidates, negative and zero scores, zero queries, and descending ordering without imposing tie order. |
| [AlternatingLeastSquaresFeaturesTest](../src/Implicit.Tests/AlternatingLeastSquaresFeaturesTest.cs) | Both inference directions with empty/unknown/zero histories, ignored entries, mixed-sign duplicate aggregation, cancellation, analytical solutions, and history isolation across A-B-A calls. |
| [AlternatingLeastSquaresRecommenderTest](../src/Implicit.Tests/AlternatingLeastSquaresRecommenderTest.cs) | Four combinations of direct/CG solving and positive/negative input; model save/load parameters, exact factors by ID, recommendation and similarity scores, and stream lifetime and position. |
| [UserItemMatrixTest](../src/Implicit.Tests/UserItemMatrixTest.cs) | Both input formats, ID-to-value mappings, missing values, omitted zero-only users/items, duplicate aggregation, cancellation, and empty matrices. |
| [UserItemValueExtensionsTest](../src/Implicit.Tests/UserItemValueExtensionsTest.cs) | Aggregation of signed duplicate interactions while preserving explicit zeros and cancelled totals. |
| [AlternatingLeastSquaresLossTest](../src/Implicit.Tests/AlternatingLeastSquaresLossTest.cs) | Reported loss against an independent objective calculation, including missing entries, alpha scaling, regularization, direct/CG training, one/two workers, negative-only input, and pair counts exceeding the 32-bit integer range. |
| [WeightTest](../src/Implicit.Tests/WeightTest.cs) | TF-IDF and BM25 normalization of single rows and multiple users, expected numerical values, unchanged unknown items, empty rows, and preservation of zero BM25 weights with boundary parameters and zero average fitting length; save/load equivalence with nondefault BM25 parameters, known and unknown items, zeros, and stream lifetime and position. |
| [RecommenderResultTest](../src/Implicit.Tests/RecommenderResultTest.cs) | Pool reuse and clearing after builder exceptions, original exception identity, and storage ownership until successful result disposal. |
| [RecommenderResultEnumeratorTest](../src/Implicit.Tests/RecommenderResultEnumeratorTest.cs) | Enumerator lifetime checks before/after pool reuse, copies and interfaces, disposal at different iteration stages, deferred LINQ, live iteration/reset, and shared empty results. |
| [ObjectPoolSlimTest](../src/Implicit.Tests/ObjectPoolSlimTest.cs) | Synchronized concurrent returns with four workers, retained instance identity and reset counts, and clearing before reuse. |
| [ObjectPoolSlimLeaseTest](../src/Implicit.Tests/ObjectPoolSlimLeaseTest.cs) | Default lease disposal and allocation-free retained leases after warmup on .NET 8/9/10. |

Recommendation integration tests use a 50-by-50 checkerboard with diagonal entries withheld. The factorization theory uses a small explicit matrix and rejects reconstruction errors above `1e-3f`. Seeded `Random(42)` sources stabilize training. The save/load test uses fixed factors, nondefault parameters, a Unicode ID, and dictionary iteration orders different from matrix row orders. It compares factors by ID and complete query results, allowing internal row indices to change on load.

Deterministic score tests use distinct user/item matrices and explicit expected scores; similarity normalizes only the candidate vector. Feature inference tests use different user/item populations and hand-solved two-factor systems with nondefault alpha and positive regularization. These tests use `1e-6` absolute tolerances, reject nonfinite results, and do not call private solver methods or inspect pool internals.

Use `[Fact]` for individual cases and `[Theory]` for meaningful parameter combinations. New tests should cover externally observable behavior, numerical tolerances, and regressions. Dispose results after consuming any deferred LINQ queries, as the recommendation tests do with scoped `using` declarations. Result pool tests use a collection with parallelization disabled so that other test collections cannot interfere with shared pool identity assertions. There is no coverage collector or minimum percentage configured. See [Engineering considerations](engineering-notes.md) for uncovered areas.

## Benchmarks

```powershell
dotnet run --project src/Implicit.Benchmark/Implicit.Benchmark.csproj -c Release -- --filter '*AlternatingLeastSquares*'
```

[`AlternatingLeastSquaresBenchmark`](../src/Implicit.Benchmark/AlternatingLeastSquaresBenchmark.cs) trains with 64 factors and CG on a 1024-by-1024 checkerboard. It compares managed MathNet and native MKL providers; the bundled native package is Windows x64-specific. The workload creates 523,264 interactions, so it is roughly half dense rather than representative of a very sparse production dataset.

Setup sets MathNet's `Control.MaxDegreeOfParallelism` to one. This does not set the ALS `ParallelOptions.MaxDegreeOfParallelism`, so training still uses its own parallel loops. `FitModel` measures fitting after input construction, including initialization; it does not measure recommendation latency or data ingestion.

The benchmark enables memory diagnostics and GitHub Markdown export. Keep runtime, provider, processor, and concurrency settings with performance comparisons. Use representative datasets and separate serving benchmarks before drawing conclusions about production throughput.

## Packaging and pipeline

To build a local package without publishing:

```powershell
dotnet pack src/Implicit/Implicit.csproj -c Release -o artifacts/packages
```

[`Package.props`](../Package.props) supplies metadata and includes the root README and license in the package. Documentation under `docs/` is repository content and is not currently included in the NuGet package.

The [GitHub Actions CI workflow](../.github/workflows/ci.yml) runs on pushes and pull requests targeting `master`, and on manual dispatch. It uses `windows-latest`, the SDK from `global.json`, and additional .NET 8/9 SDKs for their runtimes. It builds the complete solution, runs all four test targets including allocation checks, and creates one validated `Implicit` package at version `0.0.0-ci`. Benchmarks are built but not run.

The workflow has read-only repository permissions, disables persisted checkout credentials, and pins actions to commit SHAs. New runs cancel older runs for the same ref. TRX results are uploaded even when tests fail; successful builds also upload the inspection package. Both artifacts are retained for 14 days. CI does not publish to NuGet or create releases.

The shared [build script](../tools/Build-Packages.ps1) passes the same version, repository commit, and `ContinuousIntegrationBuild=true` to restore, build, test, and pack. The [package validator](../tools/Test-Packages.ps1) requires exactly one correctly named package, checks its nuspec identity, version, repository URL and commit, and verifies the five library targets, README and MIT license metadata and files.

To run the same process locally, use PowerShell 7 from the repository root:

```powershell
pwsh ./tools/Build-Packages.ps1 -Version 0.0.0-ci -ArtifactDirectory artifacts/local-ci
```

Choose a fresh artifact directory for each run. The script rejects a nonempty package directory so stale packages cannot pass validation. Local runs record `HEAD` as the repository commit; commit changes first when producing an artifact that must correspond exactly to a commit.

The [release workflow](../.github/workflows/release.yml) uses a `v*` tag as the version source, checks that its commit belongs to `master` and its version is unused, then runs the same build script. A separate publish job revalidates the downloaded artifact and obtains a temporary NuGet key using Trusted Publishing. See [GitHub Actions and releases](github-actions-and-releases.md) for environment configuration, tag rules, and recovery instructions.

## Style and contributions

Follow `.editorconfig` and `.globalconfig`: four-space C# indentation, block-scoped namespaces, Allman braces, `var`, and `this.` qualification. Project/config files generally use two spaces. Use UTF-8 without BOM and LF line endings. `.gitattributes` enforces LF for text files in every checkout.

Keep changes compatible with every library target. Use short imperative commit subjects matching the history, and explain behavior changes, compatibility effects, validation, and relevant performance measurements in pull requests. Update these documents when API semantics, binary layouts, target frameworks, or tooling change.

Naming conventions, block-scoped namespaces, and required braces are enforced during builds through IDE1006, IDE0160, and IDE0011. Work on dedicated branches (`codex/` for agent-created branches) and merge into `master` through pull requests after required CI checks pass. Write repository content in English, preserving language-specific fixtures and upstream content. During 0.x releases, design improvements may include breaking API changes when repository references and migration guidance are updated together.
