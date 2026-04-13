Launch local multiplayer test instances of Slay the Spire 2.

Argument: $ARGUMENTS (optional: number of players, default 2)

## Paths

- **Game directory:** `C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/`
- **Game exe:** `C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.exe`
- **steam_appid.txt:** `C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/steam_appid.txt`

## Steps

1. **Parse player count** from $ARGUMENTS. Default to 2 if not specified.

2. **Build the mod** by running `dotnet build` in the project directory. Abort if build fails.

3. **Ensure `steam_appid.txt` exists** in the game directory containing `2868840`. Create it if missing.

4. **Launch the host instance** in the background:
   ```
   "C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.exe" -fastmp host_standard
   ```

5. **Launch client instance(s)** in the background:
   - Player 2: `SlayTheSpire2.exe -fastmp join`
   - Player 3+: add `-clientId 1001`, `-clientId 1002`, etc.

6. **Report** how many instances were launched (1 host + N clients).

IMPORTANT: All game instances must be launched in the background so they don't block the terminal. Use `&` or equivalent.
