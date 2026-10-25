# Builds the mod and zips a release into dist/.
# Usage: ./tools/package.ps1 [-HavenDir "D:\SteamLibrary\steamapps\common\Haven"]
param(
    [string]$HavenDir = $(if ($env:HAVEN_DIR) { $env:HAVEN_DIR } else { "C:\Program Files (x86)\Steam\steamapps\common\Haven" })
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\HavenLifeOnSource\HavenLifeOnSource.csproj"

dotnet build $project -c Release "-p:HavenDir=$HavenDir"
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

[xml]$csproj = Get-Content $project
$version = $csproj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

$staging = Join-Path $root "dist\staging"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
$plugins = New-Item -ItemType Directory -Force (Join-Path $staging "BepInEx\plugins")

Copy-Item (Join-Path $root "src\HavenLifeOnSource\bin\Release\net472\HavenLifeOnSource.dll") $plugins
Copy-Item (Join-Path $root "README.md"), (Join-Path $root "LICENSE"), (Join-Path $root "THIRD-PARTY-NOTICES.md"), (Join-Path $root "CHANGELOG.md") $staging

$zip = Join-Path $root "dist\HavenLifeOnSource-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zip
Remove-Item $staging -Recurse -Force
Write-Host "Created $zip"
