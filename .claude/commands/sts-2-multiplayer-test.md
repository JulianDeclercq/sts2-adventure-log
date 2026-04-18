Launch local multiplayer test instances of Slay the Spire 2, tiled side-by-side (host left, player 2 right).

Argument: $ARGUMENTS (optional: number of players, default 2)

All logic lives in `Scripts/multiplayer-test.ps1`. It handles: build, steam_appid, `settings.save` patch (fullscreen=false), resolution detect, launch, and MoveWindow-based tiling with a 15s retry loop to outlast Godot's delayed window-position restore.

## Invoke

Run the script as a fire-and-forget background process. **Do not capture or report its stdout/stderr in the conversation.** The user watches the game windows — the script's log is noise in Claude's context. Once the process is started, the slash command is done.

```bash
powershell -NoProfile -File Scripts/multiplayer-test.ps1 -Players ${ARGUMENTS:-2} >/dev/null 2>&1 &
```

After invoking: reply with a single short line (e.g. "Launching…") and stop. Do not tail output, do not wait for exit, do not repeat errors. If the user reports a problem, investigate on request — not proactively.

## Paths (for reference)

- **Game directory:** `C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/`
- **Settings file:** `%APPDATA%/SlayTheSpire2/steam/<SteamId>/settings.save`
- **Script:** `Scripts/multiplayer-test.ps1`

## Notes

- Tiling covers host + player 2 only. Players 3+ launch at default window position.
- Script blocks ~15s total (both launch immediately, single 15s MoveWindow retry loop covers both).
- Game rewrites `settings.save` on exit — user's fullscreen pref persists normally after testing.
