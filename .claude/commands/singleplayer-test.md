Launch Slay the Spire 2 straight into the current singleplayer continue-run, bypassing the main menu.

All logic lives in `Scripts/singleplayer-test.ps1`. It builds the mod and launches the game with `-fastcontinue`, a custom flag handled by `CombatLogCode/Patches/FastContinuePatch.cs` (Harmony postfix on `NMainMenu.CheckCommandLineArgs` — auto-clicks the Continue button).

## Invoke

Run the script as a fire-and-forget background process. **Do not capture or report its stdout/stderr in the conversation.** Once the process is started, the slash command is done.

```bash
powershell -NoProfile -File Scripts/singleplayer-test.ps1 >/dev/null 2>&1 &
```

After invoking: reply with a single short line (e.g. "Launching…") and stop.

## Requirements

- A valid run save must exist (otherwise the Continue button is hidden and `-fastcontinue` is a no-op — game stays on main menu).
- Mod must be enabled (the `-fastcontinue` handler lives inside the mod DLL).

## Paths (for reference)

- **Game directory:** `C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/`
- **Script:** `Scripts/singleplayer-test.ps1`
- **Harmony patch:** `CombatLogCode/Patches/FastContinuePatch.cs`
