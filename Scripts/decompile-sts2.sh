#!/usr/bin/env bash
# Bulk-decompile the publicized sts2.dll into .decompiled/ for fast grep.
#
# Use this when you need to cross-reference game code across many classes
# (e.g. "who calls NCreature.Visuals?"). For one-off class lookups, prefer
# `ilspycmd -t <FQN>` directly — it's faster than a bulk decompile.
#
# Output: .decompiled/ (gitignored)
# Staleness: skips if output is newer than sts2.dll. Pass --force to rebuild.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

OUTPUT_DIR=".decompiled"
DLL_GLOB=".godot/mono/temp/obj/Debug/PublicizedAssemblies/sts2.*/sts2.dll"
ILSPYCMD="${ILSPYCMD:-$HOME/.dotnet/tools/ilspycmd}"

FORCE=0
if [[ "${1:-}" == "--force" || "${1:-}" == "-f" ]]; then
  FORCE=1
fi

# Resolve DLL (glob handles the build hash in the path).
shopt -s nullglob
DLLS=($DLL_GLOB)
shopt -u nullglob

if [[ ${#DLLS[@]} -eq 0 ]]; then
  echo "error: publicized sts2.dll not found. Run 'dotnet build' first." >&2
  exit 1
fi

if [[ ${#DLLS[@]} -gt 1 ]]; then
  echo "warning: multiple publicized sts2.dll found, using newest:" >&2
  printf '  %s\n' "${DLLS[@]}" >&2
fi

# Pick newest (handles stale hash dirs).
DLL="$(ls -t "${DLLS[@]}" | head -n1)"

if [[ ! -x "$ILSPYCMD" ]]; then
  echo "error: ilspycmd not found at $ILSPYCMD" >&2
  echo "install with: dotnet tool install -g ilspycmd" >&2
  exit 1
fi

# Staleness check: skip if any .cs file in output is newer than the DLL.
if [[ $FORCE -eq 0 && -d "$OUTPUT_DIR" ]]; then
  NEWEST_CS="$(find "$OUTPUT_DIR" -name '*.cs' -type f -print0 2>/dev/null \
    | xargs -0 ls -t 2>/dev/null | head -n1 || true)"
  if [[ -n "$NEWEST_CS" && "$NEWEST_CS" -nt "$DLL" ]]; then
    echo "up to date: $OUTPUT_DIR is newer than $DLL (use --force to rebuild)"
    exit 0
  fi
fi

echo "decompiling $DLL -> $OUTPUT_DIR/ ..."
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Rough expected file count, based on spire-codex README ("~3,300"). Used only
# for the progress %; real count may differ per game version. We clamp to 99%
# until ilspycmd actually finishes, so an under-estimate won't show 100% early.
EXPECTED=3300
START=$(date +%s)

# -p: emit as compilable project (one .cs per class, namespace dirs)
# --nested-directories: expand dotted namespaces into nested dirs for grep-ability
"$ILSPYCMD" -p --nested-directories -o "$OUTPUT_DIR" "$DLL" >/tmp/ilspycmd.$$.log 2>&1 &
PID=$!

# Poll every 2s. \r overwrites the same line so output stays one line.
while kill -0 "$PID" 2>/dev/null; do
  COUNT="$(find "$OUTPUT_DIR" -name '*.cs' -type f 2>/dev/null | wc -l | tr -d ' ')"
  ELAPSED=$(( $(date +%s) - START ))
  PCT=$(( COUNT * 100 / EXPECTED ))
  (( PCT > 99 )) && PCT=99
  printf '\r  [%3d%%] %5d files, %3ds elapsed' "$PCT" "$COUNT" "$ELAPSED"
  sleep 2
done

wait "$PID"
STATUS=$?
if [[ $STATUS -ne 0 ]]; then
  printf '\n'
  echo "error: ilspycmd failed (exit $STATUS). Log:" >&2
  cat /tmp/ilspycmd.$$.log >&2
  rm -f /tmp/ilspycmd.$$.log
  exit $STATUS
fi
rm -f /tmp/ilspycmd.$$.log

FILE_COUNT="$(find "$OUTPUT_DIR" -name '*.cs' -type f | wc -l | tr -d ' ')"
ELAPSED=$(( $(date +%s) - START ))
printf '\r  [100%%] %5d files, %3ds elapsed\n' "$FILE_COUNT" "$ELAPSED"
echo "done: $FILE_COUNT .cs files in $OUTPUT_DIR/"
