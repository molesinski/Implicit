#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,
    [string]$ArtifactDirectory = 'artifacts/ci'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

function Invoke-Checked {
    param([string]$Command, [string[]]$Arguments)
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE." }
}

Push-Location $root
try {
    $packages = Join-Path $ArtifactDirectory 'packages'
    if ((Test-Path -LiteralPath $packages) -and (Get-ChildItem -LiteralPath $packages -Force)) {
        throw "Package directory must be empty: $packages. Choose a new ArtifactDirectory."
    }
    $commit = & git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve the source commit.' }
    $properties = @("-p:Version=$Version", '-p:ContinuousIntegrationBuild=true', "-p:RepositoryCommit=$commit")
    Invoke-Checked dotnet (@('restore', 'Implicit.slnx') + $properties)
    Invoke-Checked dotnet (@('build', 'Implicit.slnx', '-c', 'Release', '--no-restore') + $properties)
    Invoke-Checked dotnet (@(
        'test', 'src/Implicit.Tests/Implicit.Tests.csproj', '-c', 'Release', '--no-build', '--no-restore',
        '--logger', 'trx', '--results-directory', (Join-Path $ArtifactDirectory 'test-results')
    ) + $properties)
    Invoke-Checked dotnet (@(
        'pack', 'src/Implicit/Implicit.csproj', '-c', 'Release', '--no-build', '--no-restore', '-o', $packages
    ) + $properties)
    & (Join-Path $PSScriptRoot 'Test-Packages.ps1') -Version $Version -PackageDirectory $packages -RepositoryCommit $commit
}
finally {
    Pop-Location
}
