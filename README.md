# Combat Log

A Slay the Spire 2 mod that shows a scrollable log of every card played, damage dealt, power applied, relic procced, energy gained, and card recalled during a run. Press **F** in combat to toggle the panel.

**Works in multiplayer.** Tracks every player's actions — not just yours. No version matching required; only the person who wants the log needs to install it.

## Install

Either use Vortex (automatic) or drop the files in manually.

### Via Vortex (recommended)

1. Install [Vortex](https://www.nexusmods.com/about/vortex/) if you don't already have it.
2. On the CombatLog Nexus page, click **Mod Manager Download** (→ opens in Vortex).
3. In Vortex, click **Enable** on the mod.
4. Launch the game — you should see "X mods loaded" on the main menu.
5. Press **F** in combat to toggle the log panel.

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

Fully supported. Everything the log tracks — card plays, damage, powers, relics, energy gains, recalls — is captured for every player in the lobby, labeled with the owner's name.

- **No version matching required.** Manifest is marked `affects_gameplay: false`, so CombatLog is an observation-only mod and the game won't block lobbies where only some players run it.
- **Only one player needs the mod.** Install it yourself to see the log; teammates don't have to.
- **Safe to mix versions.** Because the mod never changes gameplay state, running a different version from teammates can't cause desyncs.

## Build from source

```bash
dotnet build
```

Auto-deploys the DLL + manifest to the game's `mods/CombatLog/` folder. Game path is auto-discovered from the Steam registry; override in `Directory.Build.props` if needed.

## License

MIT — see [LICENSE](./LICENSE).
