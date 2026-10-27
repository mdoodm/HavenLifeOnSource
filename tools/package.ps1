# Builds the mod and zips two releases into dist/:
#   HavenLifeOnSource-<version>.zip               plugin only (for players who already have BepInEx)
#   HavenLifeOnSource-<version>-with-BepInEx.zip  plugin + unmodified BepInEx 5.4.23.2, ready to extract
# Usage: ./tools/package.ps1 [-HavenDir "D:\SteamLibrary\steamapps\common\Haven"]
param(
    [string]$HavenDir = $(if ($env:HAVEN_DIR) { $env:HAVEN_DIR } else { "C:\Program Files (x86)\Steam\steamapps\common\Haven" })
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\HavenLifeOnSource\HavenLifeOnSource.csproj"
$dist = Join-Path $root "dist"

# BepInEx is downloaded at packaging time and checked against the official release; it is never committed.
$bepinexVersion = "5.4.23.2"
$bepinexUrl = "https://github.com/BepInEx/BepInEx/releases/download/v$bepinexVersion/BepInEx_win_x64_$bepinexVersion.zip"
$bepinexSha256 = "f752ce4e838f4c305b9da1404b6745f2cff23b8bfd494f79f0c84d0a01f59b46"

dotnet build $project -c Release "-p:HavenDir=$HavenDir"
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

[xml]$csproj = Get-Content $project
$version = $csproj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

New-Item -ItemType Directory -Force $dist | Out-Null
$docs = "README.md", "LICENSE", "THIRD-PARTY-NOTICES.md", "CHANGELOG.md" | ForEach-Object { Join-Path $root $_ }
$dll = Join-Path $root "src\HavenLifeOnSource\bin\Release\net472\HavenLifeOnSource.dll"

function New-Release([string]$name, [scriptblock]$addExtra) {
    $staging = Join-Path $dist "staging"
    if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
    $plugins = New-Item -ItemType Directory -Force (Join-Path $staging "BepInEx\plugins")
    Copy-Item $dll $plugins
    $modDocs = New-Item -ItemType Directory -Force (Join-Path $staging "HavenLifeOnSource")
    Copy-Item $docs $modDocs
    & $addExtra $staging
    $zip = Join-Path $dist $name
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zip
    Remove-Item $staging -Recurse -Force
    Write-Host "Created $zip"
}

# Plugin only
New-Release "HavenLifeOnSource-$version.zip" { param($staging) }

# Plugin + BepInEx
$bepinexZip = Join-Path $dist "BepInEx_win_x64_$bepinexVersion.zip"
if (-not (Test-Path $bepinexZip)) { Invoke-WebRequest $bepinexUrl -OutFile $bepinexZip }
$hash = (Get-FileHash $bepinexZip -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -ne $bepinexSha256) { Remove-Item $bepinexZip; throw "BepInEx download hash mismatch ($hash)" }

New-Release "HavenLifeOnSource-$version-with-BepInEx.zip" {
    param($staging)
    Expand-Archive $bepinexZip -DestinationPath $staging -Force
    $licenses = New-Item -ItemType Directory -Force (Join-Path $staging "HavenLifeOnSource\licenses")
    Copy-Item (Join-Path $root "third_party\licenses\*") $licenses
}
