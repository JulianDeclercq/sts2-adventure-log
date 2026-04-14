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

# ---------- 6. Launch + repeated MoveWindow (outlasts game's delayed restore) ----------
function Launch-AndTile {
    param(
        [string[]]$GameArgs,
        [int]$X, [int]$Y, [int]$W, [int]$H,
        [string]$Label
    )
    Write-Host "Launching $Label ($X,$Y,${W}x${H})..."
    $p = Start-Process $exe -ArgumentList $GameArgs -PassThru

    # Poll for window handle (up to 20s)
    for ($i = 0; $i -lt 40; $i++) {
        $p.Refresh()
        if ($p.MainWindowHandle -ne [IntPtr]::Zero) { break }
        Start-Sleep -Milliseconds 500
    }
    if ($p.MainWindowHandle -eq [IntPtr]::Zero) {
        Write-Warning "$Label : no MainWindowHandle after 20s, skipping tile"
        return $p
    }

    # Repeat MoveWindow for 15s to outlast Godot's delayed window_set_position
    for ($t = 0; $t -lt 15; $t++) {
        Start-Sleep -Milliseconds 1000
        $p.Refresh()
        if ($p.MainWindowHandle -ne [IntPtr]::Zero) {
            [WindowHelper]::MoveWindow($p.MainWindowHandle, $X, $Y, $W, $H, $true) | Out-Null
        }
    }
    Write-Host "$Label tiled: pid=$($p.Id)"
    return $p
}

$hostProc   = Launch-AndTile -GameArgs @("--windowed","-fastmp","host_standard") -X 0      -Y 0 -W $halfW -H $screenH -Label "HOST"
$clientProc = Launch-AndTile -GameArgs @("--windowed","-fastmp","join")          -X $halfW -Y 0 -W $halfW -H $screenH -Label "CLIENT"

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
