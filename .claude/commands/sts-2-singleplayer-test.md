Launch Slay the Spire 2 for singleplayer mod testing.

All logic lives in `Scripts/singleplayer-test.ps1`. It builds the mod and launches the game windowed.

## Invoke

Run the script as a fire-and-forget background process. **Do not capture or report its stdout/stderr in the conversation.** Once the process is started, the slash command is done.

```bash
powershell -NoProfile -File Scripts/singleplayer-test.ps1 >/dev/null 2>&1 &
```

After invoking: reply with a single short line (e.g. "Launching…") and stop.

## Paths (for reference)

- **Game directory:** `C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/`
- **Script:** `Scripts/singleplayer-test.ps1`
