# Combat Log — StS2 Mod

## Project Overview

A Slay the Spire 2 mod that tracks and displays cards played during a run as a toggleable overlay (press H). Built with the [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) template.

- **Language:** C# / .NET 9.0
- **Engine:** Godot 4.5.1 (MegaDot variant)
- **Patching:** HarmonyLib (`0Harmony.dll` from game data)
- **Dependency:** BaseLib (community modding library)
- **Mod ID:** `CombatLog`
- **affects_gameplay:** `false` (observation-only, safe for multiplayer)

## Build & Deploy

```bash
dotnet build          # Builds and copies DLL + JSON to game's mods/ folder
dotnet publish        # Also exports .pck via Godot (requires GodotPath in Directory.Build.props)
```

The build auto-deploys to: `<Sts2Path>/mods/CombatLog/`

Game path is auto-discovered via `Sts2PathDiscovery.props` (Steam registry + common paths). Override manually in `Directory.Build.props` if needed:
```xml
<Sts2Path>C:/path/to/Slay the Spire 2</Sts2Path>
```

`Directory.Build.props` is gitignored (machine-specific paths).

## Mod Manifest (CombatLog.json)

| Field | Value | Notes |
|-------|-------|-------|
| `has_pck` | `false` | Set to `true` only if you have a `.pck` file. Game rejects mods with missing declared assets. |
| `has_dll` | `true` | We have compiled code |
| `affects_gameplay` | `false` | Cosmetic/info mods MUST be false. Controls multiplayer connection checks. Wrong value causes desyncs. |
| `dependencies` | `["BaseLib"]` | BaseLib must also be in the mods folder |

## Key Game Classes (from sts2.dll decompilation)

### Card System
- `MegaCrit.Sts2.Core.Models.CardModel` — base class for all cards
  - Each card is a concrete subclass (e.g., `Models.Cards.Bash`, `Models.Cards.Strike`)
  - `OnPlayWrapper` — called when a card is played (we patch this)
  - `OnPlay` — the card's effect implementation
  - Properties: `Id` (has `.Entry` sub-property), class name = card name
  - `Name` and `ModelId` are NOT directly accessible even with publicizer — use reflection or `GetType().Name`
- `MegaCrit.Sts2.Core.GameActions.PlayCardAction` — game action for playing a card
  - `ExecuteAction` (protected) — executes the card play
  - Has `Card` property (backing field `<Card>k__BackingField`)
- `MegaCrit.Sts2.Core.Entities.Cards` — enums: `CardType`, `CardRarity`, `TargetType`

### Combat System
- `MegaCrit.Sts2.Core.Combat.CombatManager`
  - `StartTurn` — fires for BOTH player AND enemy turns (don't use for player turn counting!)
  - `SetupPlayerTurn` — fires only for player turns (use this instead)
  - `StartCombatInternal` — fires when combat begins
  - `DoTurnEnd`, `EndEnemyTurn`, `EndPlayerTurnPhaseOneInternal`
  - `TurnsTaken` property (with backing field)
- `MegaCrit.Sts2.Core.Rooms.CombatRoom` — model class (NOT a Godot Node)
  - `StartCombat` — initiates combat
- `MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom` — Godot Node version of combat room
  - Use this for scene tree operations (UI injection, etc.)

### Hook System
`MegaCrit.Sts2.Core.Hooks.Hook` — async event hooks (state machine-based):
- `BeforeCardPlayed` / `AfterCardPlayed`
- `BeforeCombatStart` / `AfterCombatEnd`
- `BeforeTurnEnd` / `AfterTurnEnd`
- `AfterPlayerTurnStart`
- `BeforeSideTurnStart` / `AfterSideTurnStart`
- `AfterCardDrawn`, `AfterCardDiscarded`, `AfterCardRetained`
- `BeforeAttack` / `AfterAttack`
- `BeforeDeath` / `AfterDeath`
- `AfterDamageGiven`, `AfterBlockGained`, `AfterBlockBroken`
- `AfterEnergySpent`, `AfterEnergyReset`
- `AfterGoldGained`, `AfterStarsGained`, `AfterStarsSpent`
- `AfterRoomEntered`, `AfterMapGenerated`
- `AfterShuffle`, `AfterHandEmptied`

Note: These are async state machines — Harmony patching them is complex. Prefer patching concrete methods on Manager/Model classes.

### Model Lifecycle Hooks (from Commands Cookbook)
Available on card/relic/power models via override:
- `OnPlay` — when card is played
- `OnUpgrade` — when card is upgraded
- `BeforeCardPlayed` / `AfterCardPlayed`
- `AfterSideTurnStart` / `AfterTurnEnd`
- `AfterCardDrawn`
- `AfterPowerAmountChanged`

### Commands (for future expansion)
- `DamageCmd.Attack(...)` — deal damage
- `CreatureCmd.GainBlock(...)` — gain block
- `PowerCmd.Apply<T>(...)` — apply buff/debuff
- `CardPileCmd.Draw(...)` — draw cards

## Multiplayer

- `affects_gameplay: false` means the mod is allowed in multiplayer without version matching
- **Local testing:** Create `steam_appid.txt` with `2868840` in game directory, then:
  - Host: `SlayTheSpire2.exe -fastmp host_standard`
  - Client: `SlayTheSpire2.exe -fastmp join`
  - Extra clients: add `-clientId 1001`, `-clientId 1002`, etc.
- **Open question:** Whether `CardModel.OnPlayWrapper` fires for all players' cards or just the local player's needs testing

## Save Files

Modded and unmodded gameplay use **separate save files**. Disabling all mods restores the unmodded save. This is game behavior, not a bug.

## Logging

```csharp
// Preferred: game's logger (appears in game log files)
MainFile.Logger.Info("message");

// Also works: Godot's built-in (appears in console/stdout)
GD.Print("message");
GD.PrintErr("error message");
```

Logs location:
- Windows: `%appdata%/SlayTheSpire2/logs/godot.log`
- macOS: `~/Library/Application Support/SlayTheSpire2/logs`
- Linux: `~/.local/share/SlayTheSpire2/logs`

## Dev Console

Open with any of: `~`, `` ` ``, `*`, `'`

Useful commands:
- `help` — list all commands
- `help <command>` — detailed help
- `card` — spawn cards for testing
- `showlog` — open live log window
- `open logs` — open log directory in file explorer

## Publicizer Settings

In `.csproj`:
```xml
<Publicize Include="sts2" IncludeVirtualMembers="true" IncludeCompilerGeneratedMembers="false" />
```

- `IncludeVirtualMembers="true"` is needed to access protected/virtual members
- Even with publicizer, some properties on `CardModel` (like `Name`, `ModelId`) aren't accessible at compile time — use reflection as fallback

## Known Gotchas

1. **`CombatManager.StartTurn` fires twice per round** — once for player, once for enemy. Use `SetupPlayerTurn` for player-only turn tracking.
2. **`has_pck: true` without a .pck file** — game silently ignores the mod. Set to `false` if not exporting a .pck.
3. **`CombatRoom` vs `NCombatRoom`** — `CombatRoom` is a model (no Godot methods). `NCombatRoom` is the Node. Use `NCombatRoom` for scene tree operations.
4. **Mod not in mod list but loaded** — mods without a config UI don't appear in the sidebar list. Check "X mods loaded" text on main menu.
5. **Early Access breakage** — game updates frequently break mods. BaseLib usually updates within a day. Custom Harmony patches may need manual fixes.
6. **Godot scene scripts** — if creating `.tscn` scenes with mod scripts, add to initialization:
   ```csharp
   var assembly = Assembly.GetExecutingAssembly();
   Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);
   ```

## Resources

- [ModTemplate Wiki](https://github.com/Alchyr/ModTemplate-StS2/wiki)
- [BaseLib Wiki](https://github.com/Alchyr/BaseLib-StS2/wiki)
- [Harmony Docs](https://harmony.pardeike.net/)
- [Godot Docs](https://docs.godotengine.org/en/stable/getting_started/introduction/index.html)
- StS Discord `#sts2-modding` channel
