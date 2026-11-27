<#
.SYNOPSIS
  Builds RSP CX Preflight for each Revit version, and optionally deploys it.

.EXAMPLE
  ./scripts/build.ps1                          # build 2024-2027 (Debug)
  ./scripts/build.ps1 -Versions 2025 -Deploy   # build 2025 and install it for this Windows user
  ./scripts/build.ps1 -Test                    # also run the Core unit tests
  ./scripts/build.ps1 -Configuration Release -Package   # Release build zipped per version in artifacts/dist

.NOTES
  Revit locks the add-in files while it runs. Close that Revit version before -Deploy.
#>
param(
    [int[]]  $Versions = @(2024, 2025, 2026, 2027),
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',
    [switch] $Deploy,
    [switch] $Test,
    [switch] $Package
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

if ($Test) {
    dotnet test (Join-Path $root 'tests/CxPreflight.Core.Tests') -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
}

foreach ($v in $Versions) {
    Write-Host "`n== Revit $v ==" -ForegroundColor Cyan
    dotnet build (Join-Path $root 'src/CxPreflight.Addin') -c $Configuration "-p:RevitVersion=$v"
    if ($LASTEXITCODE -ne 0) { throw "Build failed for Revit $v." }

    $out = Join-Path $root "artifacts/bin/CxPreflight.Addin/R$v/$Configuration"

    if ($Package) {
        $dist = Join-Path $root 'artifacts/dist'
        New-Item -ItemType Directory -Force $dist | Out-Null
        $stage = Join-Path $dist "R$v"
        Remove-Item -Recurse -Force $stage -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Force (Join-Path $stage 'CxPreflight') | Out-Null
        Copy-Item (Join-Path $out 'CxPreflight.addin') $stage
        Get-ChildItem $out -Exclude 'CxPreflight.addin' | Copy-Item -Destination (Join-Path $stage 'CxPreflight') -Recurse
        Compress-Archive -Force -Path (Join-Path $stage '*') -DestinationPath (Join-Path $dist "RSP-CX-Preflight-R$v.zip")
    }

    if ($Deploy) {
        if (Get-Process Revit -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*Revit $v*" }) {
            Write-Warning "Revit $v is running and has the add-in files locked. Close it, then deploy again."
            continue
        }
        $addins = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$v"
        $target = Join-Path $addins 'CxPreflight'
        New-Item -ItemType Directory -Force $target | Out-Null
        Copy-Item (Join-Path $out 'CxPreflight.addin') $addins -Force
        Get-ChildItem $out -Exclude 'CxPreflight.addin' | Copy-Item -Destination $target -Recurse -Force
        Write-Host "Deployed to $addins" -ForegroundColor Green
    }
}
