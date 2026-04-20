#!/usr/bin/env bash
# macOS equivalent of Scripts/multiplayer-test.ps1
# Builds the mod, patches settings, launches N instances, tiles host+client side-by-side.
# First run may prompt for Accessibility permission (System Events needs it to move windows).

set -euo pipefail

PLAYERS="${1:-2}"

# ---------- 1. Build ----------
echo "Building mod..."
if ! dotnet build >/dev/null; then
  echo "dotnet build failed. Aborting." >&2
  exit 1
fi
echo "Build ok"

GAME_DIR="$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2"
APP="$GAME_DIR/SlayTheSpire2.app"
BIN_DIR="$APP/Contents/MacOS"
BIN="$BIN_DIR/Slay the Spire 2"
APPID_PATH="$BIN_DIR/steam_appid.txt"

if [[ ! -x "$BIN" ]]; then
  echo "Game binary not found at: $BIN" >&2
  exit 1
fi

# ---------- 2. steam_appid.txt ----------
if [[ ! -f "$APPID_PATH" ]]; then
  printf "2868840" > "$APPID_PATH"
  echo "Created steam_appid.txt"
fi

# ---------- 3. Patch settings.save: fullscreen = false ----------
SETTINGS="$(ls "$HOME/Library/Application Support/SlayTheSpire2/steam/"*/settings.save 2>/dev/null | head -1 || true)"
if [[ -n "$SETTINGS" ]]; then
  if command -v jq >/dev/null 2>&1; then
    tmp="$(mktemp)"
    jq '.fullscreen=false' "$SETTINGS" > "$tmp" && mv "$tmp" "$SETTINGS"
    echo "Patched settings.save: fullscreen=false"
  else
    echo "jq not installed; skipping settings patch (brew install jq)." >&2
  fi
else
  echo "settings.save not found; game may start fullscreen." >&2
fi

# ---------- 4. Detect primary monitor (points, not pixels — Retina-safe) ----------
BOUNDS="$(osascript -e 'tell application "Finder" to get bounds of window of desktop')"
SCREEN_W="$(echo "$BOUNDS" | awk -F', ' '{print $3}')"
SCREEN_H="$(echo "$BOUNDS" | awk -F', ' '{print $4}')"
HALF_W=$(( SCREEN_W / 2 ))
echo "Screen: ${SCREEN_W}x${SCREEN_H}, halfW=${HALF_W}"

# ---------- 5. Launch host + client ----------
# cd to BIN_DIR so Steam finds steam_appid.txt in CWD
echo "Launching HOST + CLIENT..."
( cd "$BIN_DIR" && exec "./Slay the Spire 2" --windowed -fastmp host_standard >/dev/null 2>&1 ) &
HOST_PID=$!
( cd "$BIN_DIR" && exec "./Slay the Spire 2" --windowed -fastmp join >/dev/null 2>&1 ) &
CLIENT_PID=$!

# ---------- 6. Tile loop — retry 15s to outlast Godot's delayed window restore ----------
move_window() {
  local pid="$1" x="$2" y="$3" w="$4" h="$5"
  osascript <<EOF >/dev/null 2>&1 || true
tell application "System Events"
  set procs to (every process whose unix id is $pid)
  if (count of procs) > 0 then
    tell (item 1 of procs)
      if (count of windows) > 0 then
        set position of window 1 to {$x, $y}
        set size of window 1 to {$w, $h}
      end if
    end tell
  end if
end tell
EOF
}

deadline=$(( $(date +%s) + 15 ))
while [[ $(date +%s) -lt $deadline ]]; do
  move_window "$HOST_PID"   0         0 "$HALF_W" "$SCREEN_H"
  move_window "$CLIENT_PID" "$HALF_W" 0 "$HALF_W" "$SCREEN_H"
  sleep 1
done
echo "HOST tiled: pid=$HOST_PID"
echo "CLIENT tiled: pid=$CLIENT_PID"

# ---------- 7. Extra clients (no tiling) ----------
extras=()
for (( i=3; i<=PLAYERS; i++ )); do
  cid=$(( 1000 + i - 2 ))
  ( cd "$BIN_DIR" && exec "./Slay the Spire 2" -fastmp join -clientId "$cid" >/dev/null 2>&1 ) &
  extras+=("$!")
  echo "Launched player $i (clientId=$cid, pid=$!)"
done

# ---------- 8. Summary ----------
echo ""
echo "=== Done ==="
echo "Host:   pid=$HOST_PID"
echo "Client: pid=$CLIENT_PID"
if (( ${#extras[@]} > 0 )); then
  echo "Extras: pids=${extras[*]}"
fi
echo "Total: 1 host + $((PLAYERS-1)) client(s)"
