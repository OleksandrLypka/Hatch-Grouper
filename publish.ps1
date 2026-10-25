# Builds a release of HatchGrouper into the "releases" folder.
#
#   .\publish.ps1                      interactive prompt (asks 1 or 2)
#   .\publish.ps1 -SelfContained       self-contained single .exe (works without .NET installed, bigger)
#   .\publish.ps1 -FrameworkDependent  small build, needs the .NET 8 Desktop Runtime on the target PC

param(
    [switch]$FrameworkDependent,
    [switch]$SelfContained
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'src\HatchGrouper.csproj'

# If no switch was explicitly passed via command line, prompt interactively
if (-not $PSBoundParameters.ContainsKey('FrameworkDependent') -and -not $PSBoundParameters.ContainsKey('SelfContained')) {
    Write-Host ""
    Write-Host "=== HatchGrouper: Release Build ===" -ForegroundColor Cyan
    Write-Host "Select build type:"
    Write-Host "  [1] Self-Contained      — runs on any PC without .NET installed (~150 MB) [default]" -ForegroundColor Green
    Write-Host "  [2] Framework-Dependent — requires .NET 8 Desktop Runtime installed (~1-2 MB)" -ForegroundColor Yellow
    Write-Host ""
    $choice = Read-Host "Your choice (1 or 2, Enter = 1)"
    if ($choice -eq '2') {
        $FrameworkDependent = $true
    } else {
        $FrameworkDependent = $false
    }
    Write-Host ""
}

[xml]$csproj = Get-Content $project
$version = @($csproj.Project.PropertyGroup.Version) | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw "No <Version> found in $project" }

$name = "HatchGrouper-v$version-win-x64"
if ($FrameworkDependent) { $name += '-framework-dependent' }
$releases = Join-Path $root 'releases'
$out = Join-Path $releases $name
$zip = "$out.zip"

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }

$isSelfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
$modeText = if ($FrameworkDependent) { "Framework-Dependent" } else { "Self-Contained" }
Write-Host "Building release v$version ($modeText)..." -ForegroundColor Cyan

dotnet publish $project -c Release -r win-x64 --self-contained $isSelfContained `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o $out
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit code $LASTEXITCODE)" }

# The license requires the credit to travel with every copy
Copy-Item (Join-Path $root 'LICENSE') $out
Copy-Item (Join-Path $root 'README.md') $out

Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip
Write-Host "Done! Archive created:" -ForegroundColor Green
Write-Host "  $zip"
