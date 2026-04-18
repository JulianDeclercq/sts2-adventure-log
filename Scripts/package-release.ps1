<#
.SYNOPSIS
    Builds CombatLog in Release config and produces dist/CombatLog-vX.Y.Z.zip
    with the Nexus-ready layout (mods/CombatLog/*.dll,*.json inside the zip).

.PARAMETER Publish
    If set, also creates a GitHub Release with the zip attached (requires gh CLI auth).

.EXAMPLE
    pwsh Scripts/package-release.ps1
    pwsh Scripts/package-release.ps1 -Publish
#>

param(
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

$manifest = Get-Content (Join-Path $repoRoot 'CombatLog.json') -Raw | ConvertFrom-Json
$version = $manifest.version
if (-not $version) { throw "version missing from CombatLog.json" }

Write-Host "Packaging CombatLog $version" -ForegroundColor Cyan

Push-Location $repoRoot
try {
    & dotnet build -c Release --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
} finally {
    Pop-Location
}

$dll  = Join-Path $repoRoot '.godot/mono/temp/bin/Release/CombatLog.dll'
$json = Join-Path $repoRoot 'CombatLog.json'
if (-not (Test-Path $dll))  { throw "DLL not found at $dll" }
if (-not (Test-Path $json)) { throw "Manifest not found at $json" }

$dist  = Join-Path $repoRoot 'dist'
$stage = Join-Path $dist 'stage/CombatLog'
Remove-Item (Join-Path $dist 'stage') -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $stage -Force | Out-Null

Copy-Item $dll  (Join-Path $stage 'CombatLog.dll')
Copy-Item $json (Join-Path $stage 'CombatLog.json')

$zip = Join-Path $dist "CombatLog-$version.zip"
Remove-Item $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path $stage -DestinationPath $zip

Remove-Item (Join-Path $dist 'stage') -Recurse -Force

Write-Host "Zip -> $zip" -ForegroundColor Green

if ($Publish) {
    Write-Host "Creating GitHub Release $version"
    & gh release create $version $zip --title "CombatLog $version" --generate-notes
    if ($LASTEXITCODE -ne 0) { throw "gh release create failed" }
    Write-Host "Release published." -ForegroundColor Green
}
