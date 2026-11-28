<#
.SYNOPSIS
    Packs a project template from this folder into a .tgz and optionally installs it into Unity editors.
.EXAMPLE
    ./Templates/pack.ps1
    ./Templates/pack.ps1 -Install
    ./Templates/pack.ps1 -Install -EditorsRoot "D:\Unity\Editors"
#>
param(
    [string]$Template = "com.xoggas.template.universal-2d",
    [switch]$Install,
    [string]$EditorsRoot = "C:\Program Files\Unity\Hub\Editor"
)

$ErrorActionPreference = "Stop"

$source = Join-Path $PSScriptRoot $Template
$package = Get-Content (Join-Path $source "package.json") -Raw | ConvertFrom-Json
$dist = Join-Path $PSScriptRoot "dist"
$archive = Join-Path $dist "$($package.name)-$($package.version).tgz"

# Unity expects the archive's single top-level folder to be named "package".
$staging = Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::GetRandomFileName())
try {
    New-Item -ItemType Directory -Force -Path $staging, $dist | Out-Null
    Copy-Item -Recurse -LiteralPath $source -Destination (Join-Path $staging "package")
    # Windows' bsdtar: GNU tar from Git for Windows can't take "C:\..." paths.
    & (Join-Path $env:SystemRoot "System32\tar.exe") -czf $archive -C $staging package
    if ($LASTEXITCODE -ne 0) { throw "tar failed with exit code $LASTEXITCODE" }
}
finally {
    Remove-Item -Recurse -Force -LiteralPath $staging -ErrorAction SilentlyContinue
}

Write-Host "Packed $archive"

if (-not $Install) { return }

$targets = Get-ChildItem -Directory -LiteralPath $EditorsRoot |
    Where-Object { $_.Name -like "$($package.unity)*" } |
    ForEach-Object { Join-Path $_.FullName "Editor\Data\Resources\PackageManager\ProjectTemplates" } |
    Where-Object { Test-Path -LiteralPath $_ }

if (-not $targets) {
    Write-Warning "No Unity $($package.unity) editors found under $EditorsRoot."
    return
}

# Remove older versions of this template so Hub doesn't list it twice.
$commands = foreach ($target in $targets) {
    "Get-ChildItem -LiteralPath '$target' -Filter '$($package.name)-*.tgz' | Remove-Item -Force"
    "Copy-Item -LiteralPath '$archive' -Destination '$target' -Force"
}

Start-Process powershell -Verb RunAs -Wait -ArgumentList "-NoProfile", "-Command", ($commands -join "; ")

foreach ($target in $targets) {
    if (Test-Path -LiteralPath (Join-Path $target (Split-Path $archive -Leaf))) {
        Write-Host "Installed into $target"
    }
    else {
        Write-Warning "Not installed into $target"
    }
}

Write-Host "Restart Unity Hub to see the template."
