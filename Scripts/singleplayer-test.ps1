$ErrorActionPreference = "Stop"

# ---------- 1. Build ----------
Write-Host "Building mod..."
& dotnet build | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet build failed (exit $LASTEXITCODE). Aborting."
    exit 1
}
Write-Host "Build ok"

# ---------- 2. Launch ----------
$gameDir = "C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
$exe = Join-Path $gameDir "SlayTheSpire2.exe"

Write-Host "Launching singleplayer..."
$proc = Start-Process $exe -ArgumentList @("--windowed") -PassThru
Write-Host "Launched: pid=$($proc.Id)"
