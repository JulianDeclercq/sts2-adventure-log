Toggle between modded and unmodded Slay the Spire 2 mode.

Argument: $ARGUMENTS (optional: "on" or "off". If omitted, toggle based on current state.)

## Paths

- **Mods folder:** `C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods/`
- **Save root:** `$APPDATA/SlayTheSpire2/steam/76561198094628058/`
- **Unmodded saves:** `$APPDATA/SlayTheSpire2/steam/76561198094628058/profile1/`, `profile2/`, `profile3/`
- **Modded saves:** `$APPDATA/SlayTheSpire2/steam/76561198094628058/modded/profile1/`, `modded/profile2/`, `modded/profile3/`
- **Backup destination:** `$HOME/Downloads/sts2-save-backup/`

## Steps

1. **Determine current state:** Check if the mods folder has any mod subfolders. If it does, mods are currently ON. If empty or doesn't exist, mods are OFF.

2. **Determine desired state:** If argument is "on" or "off", use that. Otherwise toggle (if currently on → turn off, if currently off → turn on).

3. **Create timestamped backup** of BOTH save directories (modded and unmodded) to `$HOME/Downloads/sts2-save-backup/{timestamp}/`:
   - Copy `modded/` folder → backup
   - Copy `profile1/`, `profile2/`, `profile3/` → backup

4. **Toggle mods:**
   - **Turning OFF:** Move all mod folders from the mods directory to `$HOME/Downloads/sts2-mods-stash/`. Do NOT delete them.
   - **Turning ON:** Move all mod folders from `$HOME/Downloads/sts2-mods-stash/` back to the mods directory. If the stash doesn't exist, run `dotnet build` in the project directory to redeploy AdventureLog, and warn that other mods (like BaseLib) may need to be manually restored.

5. **Report** what was done: which mods were moved, where backups were saved, and the new state (modded/unmodded).

IMPORTANT: Always back up saves BEFORE moving any mods. Never delete save files or mod files — always move them.
