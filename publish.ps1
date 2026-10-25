# Builds a release of HatchGrouper into the "releases" folder.
#
#   .\publish.ps1                      self-contained single .exe (works without .NET installed, bigger)
#   .\publish.ps1 -FrameworkDependent  small build, needs the .NET 8 Desktop Runtime on the target PC
#
# Result: releases\HatchGrouper-v<version>-win-x64[-framework-dependent]\  and the same as a .zip
# The version is taken from <Version> in src\HatchGrouper.csproj.

param([switch]$FrameworkDependent)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'src\HatchGrouper.csproj'

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

$selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
dotnet publish $project -c Release -r win-x64 --self-contained $selfContained `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o $out
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit code $LASTEXITCODE)" }

# The license requires the credit to travel with every copy
Copy-Item (Join-Path $root 'LICENSE') $out
Copy-Item (Join-Path $root 'README.md') $out

Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip
Write-Host "Done: $zip"
