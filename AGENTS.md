# Repository Guidelines

## Project Structure & Module Organization

Implicit is a .NET collaborative filtering library built on MathNet.Numerics. `Implicit.slnx` contains three projects:

- `src/Implicit/`: recommender APIs, alternating least squares training, and user/item matrices. `Weights/` contains weighting strategies; `Utils/` contains pooling helpers.
- `src/Implicit.Tests/`: xUnit tests and shared recommender test bases.
- `src/Implicit.Benchmark/`: BenchmarkDotNet performance tests and generated datasets.

Shared compiler and analyzer settings live in `Directory.Build.props`, `Directory.Build.targets`, `.editorconfig`, and `.globalconfig`. NuGet metadata lives in `Package.props`; GitHub Actions CI is in `.github/workflows/ci.yml`, with shared build and package validation scripts under `tools/`.

## Build, Test, and Development Commands

Run commands from the repository root. Use the SDK selected by `global.json` (10.0.100 with `latestFeature` roll-forward).

- `dotnet restore Implicit.slnx`: restore all project dependencies.
- `dotnet build Implicit.slnx -c Release --no-restore`: build the solution with analyzers enabled.
- `dotnet test src/Implicit.Tests/Implicit.Tests.csproj -c Release`: run all test targets. The full matrix includes .NET Framework 4.7.2 and .NET 8/9/10; use Windows with the required runtimes.
- `dotnet test src/Implicit.Tests/Implicit.Tests.csproj -c Release -f net10.0`: run only .NET 10 tests during development.
- `dotnet run --project src/Implicit.Benchmark/Implicit.Benchmark.csproj -c Release -- --filter '*AlternatingLeastSquares*'`: run ALS benchmarks, including the Windows x64 native MKL provider.
- `dotnet pack src/Implicit/Implicit.csproj -c Release`: create the NuGet package locally.
- `pwsh ./tools/Build-Packages.ps1 -Version 0.0.0-ci -ArtifactDirectory artifacts/local-ci`: run the CI build, all test targets, packaging, and package validation locally. Use a fresh output directory; this command does not publish.

## Coding Style & Naming Conventions

Follow `.editorconfig`: four-space C# indentation, two-space project/configuration indentation, LF endings, and UTF-8 without BOM. Use block-scoped namespaces, Allman braces, `var`, and `this.` for instance members. Use PascalCase for types and methods, camelCase for parameters and private fields, and an `I` prefix for interfaces. Match filenames to their primary types.

Nullable references, .NET analyzers, and StyleCop are enabled; warnings fail builds. Keep changes compatible with the library's .NET Standard 2.0/2.1 and .NET 8/9/10 targets.

## Testing Guidelines

Use `*Test.cs` classes, `[Fact]` for individual cases, and `[Theory]` with `[InlineData]` for variants. Follow existing method names such as `Factorize` and `SaveLoad`. Seed randomized inputs, as existing tests do with `new Random(42)`, and use explicit numerical tolerances. Add regression coverage for behavior changes; no numeric coverage threshold is configured.

## Commit & Pull Request Guidelines

Make repository changes on a dedicated branch and merge them into `master` through a pull request. Do not commit or push directly to `master`. Use the `codex/` prefix for agent-created branches. Merge only after the required CI checks pass.

Write repository documentation, code comments, commit messages, PR titles and descriptions, issue text, and release notes in English. Preserve language-specific test data and upstream content in their original language. Conversation with the user may follow the user's preferred language.

Use short imperative commit subjects, such as `Support negative confidence values` or `Rename ObjectPool to ObjectPoolSlim`. Keep commits focused. PR descriptions should explain the behavior change, link relevant issues, and record validation commands and results. Include benchmark comparisons for performance changes and compatibility notes for public API changes.

During 0.x releases, breaking public API changes are acceptable when they improve the design. Update all repository references, tests, and examples, and document the migration for consumers. Do not preserve an inconsistent API solely for backward compatibility.
