# Combat Log

A Slay the Spire 2 mod that shows a scrollable log of every card played, damage dealt, power applied, relic procced, energy gained, and card recalled during a run. Press **F** in combat to toggle the panel.

## Install

### Manual

1. In Steam, right-click **Slay the Spire 2** → Properties → Installed Files → Browse.
2. Create a `mods` folder next to `SlayTheSpire2.exe` if it doesn't exist.
3. Grab the latest `CombatLog-vX.Y.Z.zip` from [Releases](https://github.com/JulianDeclercq/sts2-combat-log/releases) and extract so your layout looks like:
   ```
   Slay the Spire 2/
     mods/
       CombatLog/
         CombatLog.dll
         CombatLog.json
   ```
4. Launch the game. You should see "X mods loaded" on the main menu.
5. Press **F** in combat to toggle the log panel.

### Mod manager

Drop the zip into [STS2 Mod Manager](https://www.nexusmods.com/slaythespire2/mods/461) and let it handle placement.

### First-time modding note

Modded and unmodded runs use separate save files. On your first mod install, copy your existing save from the unmodded save folder into the modded folder to keep your run:
- Windows: `%AppData%\SlayTheSpire2\`
- macOS: `~/Library/Application Support/SlayTheSpire2/`
- Linux: `~/.local/share/SlayTheSpire2/`

## Usage

- **F** — toggle panel.
- Drag the panel by its empty background to move it.
- Drag any edge or corner to resize.
- Hover a row to see a tooltip and highlight the creatures involved.
- Click a card row to open the inspect screen.

## Multiplayer

Marked `affects_gameplay: false`, so the mod is allowed in multiplayer without version matching. Tracks cards played by all players in a run.

## Build from source

```bash
dotnet build
```

Auto-deploys the DLL + manifest to the game's `mods/CombatLog/` folder. Game path is auto-discovered from the Steam registry; override in `Directory.Build.props` if needed.

## License

MIT.
