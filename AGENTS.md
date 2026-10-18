# Repository Guidelines

## Project Structure & Module Organization

Implicit is a .NET collaborative filtering library built on MathNet.Numerics. `Implicit.slnx` contains three projects:

- `src/Implicit/`: recommender APIs, alternating least squares training, and user/item matrices. `Weights/` contains weighting strategies; `Utils/` contains pooling helpers.
- `src/Implicit.Tests/`: xUnit tests and shared recommender test bases.
- `src/Implicit.Benchmark/`: BenchmarkDotNet performance tests and generated datasets.

Shared compiler and analyzer settings live in `Directory.Build.props`, `Directory.Build.targets`, `.editorconfig`, and `.globalconfig`. NuGet metadata lives in `Package.props`; Azure Pipelines configuration is in `azure-pipelines.yml`.

## Build, Test, and Development Commands

Run commands from the repository root. Use the SDK selected by `global.json` (10.0.100 with `latestFeature` roll-forward).

- `dotnet restore Implicit.slnx`: restore all project dependencies.
- `dotnet build Implicit.slnx -c Release --no-restore`: build the solution with analyzers enabled.
- `dotnet test src/Implicit.Tests/Implicit.Tests.csproj -c Release`: run all test targets. The full matrix includes .NET Framework 4.7.2 and .NET 8/9/10; use Windows with the required runtimes.
- `dotnet test src/Implicit.Tests/Implicit.Tests.csproj -c Release -f net10.0`: run only .NET 10 tests during development.
- `dotnet run --project src/Implicit.Benchmark/Implicit.Benchmark.csproj -c Release -- --filter '*AlternatingLeastSquares*'`: run ALS benchmarks, including the Windows x64 native MKL provider.
- `dotnet pack src/Implicit/Implicit.csproj -c Release`: create the NuGet package locally.

## Coding Style & Naming Conventions

Follow `.editorconfig`: four-space C# indentation, two-space project/configuration indentation, CRLF endings, and UTF-8 BOM unless overridden. Use block-scoped namespaces, Allman braces, `var`, and `this.` for instance members. Use PascalCase for types and methods, camelCase for parameters and private fields, and an `I` prefix for interfaces. Match filenames to their primary types.

Nullable references, .NET analyzers, and StyleCop are enabled; warnings fail builds. Keep changes compatible with the library's .NET Standard 2.0/2.1 and .NET 8/9/10 targets.

## Testing Guidelines

Use `*Test.cs` classes, `[Fact]` for individual cases, and `[Theory]` with `[InlineData]` for variants. Follow existing method names such as `Factorize` and `SaveLoad`. Seed randomized inputs, as existing tests do with `new Random(42)`, and use explicit numerical tolerances. Add regression coverage for behavior changes; no numeric coverage threshold is configured.

## Commit & Pull Request Guidelines

Use short imperative commit subjects, such as `Support negative confidence values` or `Rename ObjectPool to ObjectPoolSlim`. Keep commits focused. PR descriptions should explain the behavior change, link relevant issues, and record validation commands and results. Include benchmark comparisons for performance changes and compatibility notes for public API changes.
