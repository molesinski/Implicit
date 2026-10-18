# GitHub Actions and NuGet releases

Implicit publishes one NuGet package, `Implicit`, containing all five library targets. Tests and benchmarks are non-packable.

## Workflows

| Workflow | Trigger | Result |
| --- | --- | --- |
| [CI](../.github/workflows/ci.yml) | Pull requests targeting `master`, pushes to `master`, or manual dispatch | Release build, all four test targets, and a validated inspection package at version `0.0.0-ci` |
| [Release](../.github/workflows/release.yml) | Push of a `v*` tag in `molesinski/Implicit` | Build and test the tagged commit, then publish its package using NuGet Trusted Publishing |

Both builds use Windows, the SDK selected by `global.json`, and additional .NET 8/9 SDKs for their runtimes. Tests cover `net472`, `net8.0`, `net9.0`, and `net10.0`, including allocation checks. Benchmarks are built but not run. Actions are pinned to commit SHAs.

The shared [build script](../tools/Build-Packages.ps1) passes one version, repository commit, and `ContinuousIntegrationBuild=true` through restore, build, test, and pack. The [package validator](../tools/Test-Packages.ps1) checks the package name, nuspec identity and version, repository URL and commit, all five library targets, README and license.

The release build uploads TRX results even after test failures and retains them for 14 days. Successful builds retain the package for 90 days, subject to repository retention limits. Publication downloads this artifact by ID and validates it again, without rebuilding. Only the publish job requests `id-token: write`, and it runs in the `nuget` environment. No long-lived publishing key is stored in the repository or workflow.

## GitHub and NuGet configuration

1. Merge the workflows into `master` and enable GitHub Actions. If actions are restricted, allow `actions/checkout`, `actions/setup-dotnet`, `actions/upload-artifact`, `actions/download-artifact`, and `NuGet/login`.
2. Create the GitHub environment **`nuget`**. Add its variable **`NUGET_USER`**, set to the NuGet profile name with publishing access to `Implicit` (not an email address).
3. Restrict the environment to selected tags matching **`v*`**. A branch rule does not cover tag-triggered releases. Configure reviewers if desired and supported by the repository plan.
4. Protect `master` and require CI's **Build, test and pack** check after its first run. Protect `v*` tags against updates/deletions and restrict creation to release maintainers.
5. In the NuGet account's Trusted Publishing settings, create a GitHub policy with these values:

| Setting | Value |
| --- | --- |
| Repository owner | `molesinski` |
| Repository | `Implicit` |
| Workflow file | `release.yml` (filename only) |
| Environment | `nuget` |
| Policy owner | The user or organization owning the existing `Implicit` package |
| Scope | Publish new versions of existing packages |
| Package selection | `Implicit` |

NuGet exchanges the workflow's OIDC token for a temporary API key immediately before publishing. If the policy shows a pending activation deadline, follow the instructions shown by NuGet. See [Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing), [NuGet/login](https://github.com/NuGet/login), and [GitHub environments](https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/manage-environments).

These settings must be configured separately; adding workflow files does not create environments or publishing policies.

## Create a release

Choose an unused version. Tags must use `vMAJOR.MINOR.PATCH`, optionally followed by a lowercase SemVer prerelease suffix, such as `v0.8.0-rc.1`. The workflow rejects build metadata, uppercase suffixes and numeric identifiers with leading zeroes. Versions are independent of CI run numbers.

After merging the intended commit into `master` and passing CI, create and push an annotated tag. For example:

```powershell
git switch master
git pull --ff-only
git tag -a v0.8.0-rc.1 -m "Release 0.8.0-rc.1"
git push origin v0.8.0-rc.1
```

This is an example, not a reserved version. Pushing the tag starts publication, subject to environment protection. The workflow requires the tagged commit to be an ancestor of `origin/master` and rejects versions already listed by NuGet. Never move a release tag. No GitHub Release is created automatically; release notes can be attached separately.

The release concurrency group does not cancel an active publication. Finish one release before pushing another tag; pending runs may be replaced by newer ones. NuGet indexing may lag behind an accepted upload, and the availability check does not prevent another publisher from racing this workflow.

For a local build without publishing or creating tags, use PowerShell 7 and a fresh artifact directory:

```powershell
pwsh ./tools/Build-Packages.ps1 -Version 0.0.0-local.1 -ArtifactDirectory artifacts/local-release
```

The script records `HEAD` as the source commit. Commit all intended changes before building an artifact that must correspond exactly to that commit.

## Recover a failed publication

- If the build fails, fix the cause. Source changes require a new version and tag; transient errors can be retried with **Re-run failed jobs**.
- If publication fails, check whether NuGet accepted the package. Correct configuration or transient errors and use **Re-run failed jobs** in the original run. The publish job reuses the successful build's artifact ID.
- Retries add `--skip-duplicate`; first attempts do not. This handles an accepted upload whose response was lost. It does not compare package contents, so first confirm any existing version came from the original run.
- Do not use **Re-run all jobs** to recover an accepted upload: the version check will reject it, and rebuilding would not preserve the original artifact.
- If the artifact expired or was deleted, investigate the existing version and use a new version for a corrected release instead of rebuilding under the old version.

See [`dotnet nuget push`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-push) for duplicate handling.
