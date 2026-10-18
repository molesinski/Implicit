# Development and Testing

Use the repository's SDK and analyzer configuration when changing the library. [`AGENTS.md`](../AGENTS.md) contains the concise contribution guide; this chapter explains the build matrix, test coverage, benchmark workload, and packaging flow.

## Toolchain and targets

[`global.json`](../global.json) selects SDK `10.0.100` with `latestFeature` roll-forward. [`Directory.Build.props`](../Directory.Build.props) selects C# 14, nullable reference analysis, implicit usings, .NET analysis level 10, and build-time code style enforcement. All warnings are errors. StyleCop.Analyzers `1.2.0-beta.556` is shared across projects.

| Project | Targets | Principal dependencies |
| --- | --- | --- |
| Library | `netstandard2.0`, `netstandard2.1`, `net8.0`, `net9.0`, `net10.0` | MathNet.Numerics `5.0.0`; Microsoft.Bcl.Numerics `8.0.0` only for `netstandard2.0`. |
| Tests | `net472`, `net8.0`, `net9.0`, `net10.0` | xUnit `2.9.3`, runner `3.1.5`, Microsoft.NET.Test.Sdk `18.0.1`. |
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

## Existing tests

The concrete ALS test class inherits shared tests from two abstract base classes. It yields 12 cases per target:

| Source | Behavior exercised |
| --- | --- |
| [RecommenderTest](../src/Implicit.Tests/RecommenderTest.cs) | Held-out diagonal recommendations and parity-based user/item similarity. |
| [MatrixFactorizationRecommenderTest](../src/Implicit.Tests/MatrixFactorizationRecommenderTest.cs) | Reconstructed-user ranking, user inference from one item, and custom feature similarity. |
| [AlternatingLeastSquaresRecommenderTest](../src/Implicit.Tests/AlternatingLeastSquaresRecommenderTest.cs) | Four combinations of direct/CG solving and positive/negative input; model save/load ranking. |

Most behavior tests use a 50-by-50 checkerboard with diagonal entries withheld. The factorization theory uses a small explicit matrix and rejects reconstruction errors above `1e-3f`. Seeded `Random(42)` sources stabilize training. The save/load test compares ordered IDs, not exact factor values or score equality.

Use `[Fact]` for individual cases and `[Theory]` for meaningful parameter combinations. New tests should cover externally observable behavior, numerical tolerances, and regressions. Dispose results even though several older tests omit disposal. There is no coverage collector or minimum percentage configured. See [Engineering considerations](engineering-notes.md) for uncovered areas.

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

[`azure-pipelines.yml`](../azure-pipelines.yml) uses `windows-latest`, installs .NET 8/9 runtimes and the SDK from `global.json`, restores and builds the library and tests, runs tests, publishes TRX results, packs, and pushes through the `Implicit` NuGet service connection. The YAML sets both `trigger: none` and `pr: none`; automatic push/PR runs are not enabled there. It does not build or run benchmarks.

Pipeline versions use `0.7` plus a counter-derived patch value, passed consistently to build, test, and pack. Running that pipeline includes a publication step; local `dotnet pack` only creates files.

## Style and contributions

Follow `.editorconfig` and `.globalconfig`: four-space C# indentation, block-scoped namespaces, Allman braces, `var`, and `this.` qualification. Project/config files generally use two spaces. Preserve UTF-8 BOM and CRLF unless a file-specific override applies.

Keep changes compatible with every library target. Use short imperative commit subjects matching the history, and explain behavior changes, compatibility effects, validation, and relevant performance measurements in pull requests. Update these documents when API semantics, binary layouts, target frameworks, or tooling change.
