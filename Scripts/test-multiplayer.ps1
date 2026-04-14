param(
    [int]$Players = 2
)

$ErrorActionPreference = "Stop"

# ---------- 1. Build ----------
Write-Host "Building mod..."
& dotnet build | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet build failed (exit $LASTEXITCODE). Aborting."
    exit 1
}
Write-Host "Build ok"

# ---------- 2. steam_appid.txt ----------
$gameDir = "C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
$appidPath = Join-Path $gameDir "steam_appid.txt"
if (-not (Test-Path $appidPath)) {
    Set-Content -Path $appidPath -Value "2868840" -NoNewline
    Write-Host "Created steam_appid.txt"
}

# ---------- 3. Patch settings.save: fullscreen = false ----------
$settingsPath = Get-ChildItem "$env:APPDATA\SlayTheSpire2\steam\*\settings.save" |
                Select-Object -First 1 -ExpandProperty FullName
if ($settingsPath) {
    $s = Get-Content $settingsPath -Raw | ConvertFrom-Json
    $s.fullscreen = $false
    $s | ConvertTo-Json -Depth 20 | Set-Content $settingsPath -NoNewline
    Write-Host "Patched settings.save: fullscreen=false"
} else {
    Write-Warning "settings.save not found; game may start fullscreen."
}

# ---------- 4. Detect primary monitor ----------
Add-Type -AssemblyName System.Windows.Forms
$bounds  = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$halfW   = [int]($bounds.Width / 2)
$screenH = $bounds.Height
Write-Host "Screen: $($bounds.Width)x$screenH, halfW=$halfW"

# ---------- 5. user32 bindings ----------
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WindowHelper {
    [DllImport("user32.dll")]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int W, int H, bool bRepaint);
}
"@

$exe = Join-Path $gameDir "SlayTheSpire2.exe"

# ---------- 6. Launch both processes upfront, then tile them in parallel ----------
Write-Host "Launching HOST + CLIENT..."
$hostProc   = Start-Process $exe -ArgumentList @("--windowed","-fastmp","host_standard") -PassThru
$clientProc = Start-Process $exe -ArgumentList @("--windowed","-fastmp","join")          -PassThru

# Rect per process
$targets = @(
    @{ Proc = $hostProc;   X = 0;      Y = 0; W = $halfW; H = $screenH; Label = "HOST"   },
    @{ Proc = $clientProc; X = $halfW; Y = 0; W = $halfW; H = $screenH; Label = "CLIENT" }
)

# Interleaved loop: ~15s total, MoveWindow both each tick
$deadline = (Get-Date).AddSeconds(15)
while ((Get-Date) -lt $deadline) {
    foreach ($t in $targets) {
        $t.Proc.Refresh()
        if ($t.Proc.MainWindowHandle -ne [IntPtr]::Zero) {
            [WindowHelper]::MoveWindow($t.Proc.MainWindowHandle, $t.X, $t.Y, $t.W, $t.H, $true) | Out-Null
        }
    }
    Start-Sleep -Milliseconds 1000
}
Write-Host "HOST tiled: pid=$($hostProc.Id)"
Write-Host "CLIENT tiled: pid=$($clientProc.Id)"

# ---------- 7. Additional clients (no tiling) ----------
$extraPids = @()
for ($i = 3; $i -le $Players; $i++) {
    $clientId = 1000 + ($i - 2)
    $extra = Start-Process $exe -ArgumentList "-fastmp","join","-clientId",$clientId -PassThru
    $extraPids += $extra.Id
    Write-Host "Launched player $i (clientId=$clientId, pid=$($extra.Id))"
}

# ---------- 8. Summary ----------
Write-Host ""
Write-Host "=== Done ==="
Write-Host "Host:   pid=$($hostProc.Id)"
Write-Host "Client: pid=$($clientProc.Id)"
if ($extraPids.Count -gt 0) {
    Write-Host "Extras: pids=$($extraPids -join ',')"
}
Write-Host "Total: 1 host + $($Players - 1) client(s)"
