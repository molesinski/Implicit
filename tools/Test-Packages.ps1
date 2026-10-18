#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,
    [string]$PackageDirectory = 'artifacts/ci/packages',
    [Parameter(Mandatory)]
    [string]$RepositoryCommit
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$files = @(Get-ChildItem -LiteralPath $PackageDirectory -File -Filter '*.nupkg')
$expectedName = "Implicit.$Version.nupkg"
if ($files.Count -ne 1 -or $files[0].Name -cne $expectedName) {
    throw "Expected exactly one package named $expectedName."
}

$archive = [System.IO.Compression.ZipFile]::OpenRead($files[0].FullName)
try {
    $specs = @($archive.Entries | Where-Object { $_.FullName -like '*.nuspec' })
    if ($specs.Count -ne 1) { throw 'Implicit must contain exactly one nuspec.' }
    $reader = [System.IO.StreamReader]::new($specs[0].Open())
    try { [xml]$spec = $reader.ReadToEnd() }
    finally { $reader.Dispose() }
    $metadata = $spec.package.metadata
    if ($metadata.id -cne 'Implicit' -or $metadata.version -cne $Version) {
        throw 'Unexpected package identity or version.'
    }
    if ($metadata.repository.commit -cne $RepositoryCommit) {
        throw 'Unexpected repository commit.'
    }
    if ($metadata.repository.url -cne 'https://github.com/molesinski/Implicit') {
        throw 'Unexpected repository URL.'
    }
    if ($metadata.readme -cne 'README.md' -or $metadata.license.type -cne 'expression' -or $metadata.license.InnerText -cne 'MIT') {
        throw 'Unexpected package readme or license metadata.'
    }
    foreach ($name in @('README.md', 'LICENSE')) {
        $entries = @($archive.Entries | Where-Object { $_.FullName -ceq $name })
        if ($entries.Count -ne 1 -or $entries[0].Length -eq 0) { throw "Missing or empty $name." }
    }
    $expectedLibraries = @('netstandard2.0', 'netstandard2.1', 'net8.0', 'net9.0', 'net10.0') |
        ForEach-Object { "lib/$_/Implicit.dll" }
    $libraries = @($archive.Entries | Where-Object { $_.FullName -like 'lib/*' } | ForEach-Object { $_.FullName })
    if (Compare-Object -ReferenceObject $expectedLibraries -DifferenceObject $libraries -CaseSensitive) {
        throw 'Unexpected library target frameworks or files.'
    }
    foreach ($entry in $archive.Entries | Where-Object { $_.FullName -in $expectedLibraries }) {
        if ($entry.Length -eq 0) { throw "Empty library: $($entry.FullName)." }
    }
    Write-Host "Validated Implicit $Version ($RepositoryCommit), all five library targets, README and license."
}
finally {
    $archive.Dispose()
}
