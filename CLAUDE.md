# Combat Log — StS2 Mod

## Project Overview

A Slay the Spire 2 mod that tracks and displays cards played during a run as a toggleable overlay (press H). Built with the [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) template.

- **Language:** C# / .NET 9.0
- **Engine:** Godot 4.5.1 (MegaDot variant)
- **Patching:** HarmonyLib (`0Harmony.dll` from game data)
- **Dependency:** BaseLib (community modding library)
- **Mod ID:** `CombatLog`
- **affects_gameplay:** `false` (observation-only, safe for multiplayer)

## Roadmap

Before scoping new work, read `plans/roadmap.md`. It defines core goals (multiplayer-first), open questions, sequencing, and non-goals. If a new request conflicts with it, surface the conflict; don't silently drift.

## Git Conventions

- Commit messages should be very concise (short single line)
- Do NOT include `Co-Authored-By` trailers

## Build & Deploy

```bash
dotnet build          # Builds and copies DLL + JSON to game's mods/ folder
dotnet publish        # Also exports .pck via Godot (requires GodotPath in Directory.Build.props)
```

**Do NOT build automatically after every change.** Only build when the user explicitly asks. When the user says "build", run `dotnet build` which compiles and auto-deploys the DLL + manifest to the game's mods folder.

**Do NOT dump build output into the conversation.** After running `dotnet build`, report only a one-line summary: `Build ok` / `Build failed: <first error>`. Do not paste warnings, "copying dll", time elapsed, or any other build noise. Check the exit code and the error count. If you need warning/error detail to fix something, grep the output file for `error CS` — don't tail the whole log.

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
  - `OnPlayWrapper(PlayerChoiceContext, Creature? target, bool isAutoPlay, ResourceInfo, bool)` — called when a card is played (we patch this)
  - `OnPlay` — the card's effect implementation
  - `CurrentTarget` — `Creature?` set during `OnPlayWrapper`
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
  - `CreatureNodes` — `IEnumerable<NCreature>` of all creatures in current combat
- `MegaCrit.Sts2.Core.Nodes.Combat.NCreature` — Godot Node for a creature (enemy/player/ally)
  - `Entity` — the `Creature` reference (has `Name`, `CombatId`, `IsEnemy`)
  - `ShowSingleSelectReticle()` / `HideSingleSelectReticle()` — game's native selection ring highlight
  - `Visuals` — `NCreatureVisuals` for shader overlays (e.g., `TryApplyLiquidOverlay(color)`)
- `MegaCrit.Sts2.Core.Entities.Creatures.Creature` — creature entity model
  - `Name` — display name, `CombatId` — `uint?` unique within a combat
  - `IsEnemy` — whether this creature is on the enemy side

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

## UI Design Principle: Reuse Game Scenes

When the user shows a screenshot of an existing game UI component and asks to match it, **immediately look for the game's scene or node that renders it** — do not try to approximate it with static image paths or manual compositing first.

- Search for `.tscn` scene paths (e.g., `res://scenes/cards/tiny_card.tscn`)
- Instantiate the scene and set its data via `Traverse` if needed
- This produces pixel-perfect results in one attempt instead of iterating through wrong approaches

**Example:** When asked to match the Compendium run history card icons, the right first move is finding and instantiating `NTinyCard` — not loading type icon PNGs.

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
7. **`TreeEntered` vs `Ready` signal** — `TreeEntered` fires when a node enters the scene tree but **before** `_Ready()` runs. If you need child node references that are initialized in `_Ready()`, use the `Ready` signal instead (fires **after** `_Ready()` completes). Order: `TreeEntered` → children's `_Ready()` → parent's `_Ready()` → `Ready` signal.
8. **Instantiated scenes need the tree for `_Ready()`** — when you instantiate a PackedScene and call methods that depend on `_Ready()` (like `NTinyCard.SetCard()`), defer the call until after the node is in the tree. Pattern:
   ```csharp
   var node = scene.Instantiate<NTinyCard>();
   parent.AddChild(node);
   node.Ready += () => node.SetCard(card); // not TreeEntered!
   ```

9. **Harmony `__N` parameters for method arguments** — In a Harmony prefix/postfix, use `__0`, `__1`, etc. to capture the original method's parameters by position. Example: `OnPlayWrapper`'s 2nd arg is `Creature? target`, captured as `Creature? __1`.
10. **Decompile the game's own UI code first** — When using a game component (e.g., `NTinyCard`), decompile a game class that already uses it (e.g., `NDeckHistoryEntry.Reload()`) to see the exact setup pattern. This avoids guessing property names and call order.

## How to Research STS2 APIs

When you need to find how a game API works (e.g., "how do I get X property from Y class"):

1. **Use `ilspycmd` to decompile the class** — this is the fastest, most reliable method. `ilspycmd` is installed globally. Always start here before guessing property names or grepping binary strings.
   ```bash
   ilspycmd "path/to/sts2.dll" -t "MegaCrit.Sts2.Core.Nodes.Cards.NTinyCard"
   ```
   The publicized DLL is at: `.godot/mono/temp/obj/Debug/PublicizedAssemblies/sts2.*/sts2.dll`
2. **Search GitHub for other STS2 mods** — community mods are the real documentation.
3. **Check the official wikis** — but note BaseLib docs are often incomplete (e.g., CardModel docs are "TODO")
4. **If a property isn't accessible via publicizer**, use Harmony's `Traverse.Create(instance).Property<T>("PropName").Value`

### Reference Mods (known-good API usage examples)

- **sts2-advisor** (QuestceSpire) — https://github.com/ebadon16/sts2-advisor
  - `GameBridge/GameStateReader.cs` is the best single file for CardModel properties and game state access
- **BetterSpire2** — https://github.com/jdr1813/BetterSpire2
  - `DamageCounter/DeckTracker.cs` shows Harmony Traverse patterns for accessing properties
- **sts-1-to-2-card** — https://github.com/rayinls/sts-1-to-2-card
  - Good example of localization file structure (`localization/eng/cards.json`)

### Localization System

- Game uses JSON files at `localization/{lang_code}/cards.json`
- Key format: `"{CARD_ID}.title"` for display name, `"{CARD_ID}.description"` for effect text
- `CardModel.Title` resolves the localized name at runtime via the game's SmartFormat system

### CardModel Properties (confirmed accessible with publicizer)

| Property | Type | Returns |
|----------|------|---------|
| `Title` | string | Localized display name |
| `Id` | ModelId | Has `.Entry` for internal ID string |
| `EnergyCost` | object | Has `.Canonical` for cost value |
| `Type` | CardType | Attack, Skill, Power, etc. |
| `Rarity` | CardRarity | Basic, Common, Uncommon, Rare |
| `IsUpgraded` | bool | Whether upgraded |
| `DynamicVars` | dict-like | Damage, Block, etc. |

## Resources

- **[Spire Codex](https://github.com/ptrlrd/spire-codex)** ⭐ — Comprehensive reverse-engineered database and REST API with structured STS2 game data (cards, relics, monsters, events, all 14 languages). **Check this first when looking up any game data, IDs, or resource paths.** Live site: [spire-codex.com](https://spire-codex.com)
- [ModTemplate Wiki](https://github.com/Alchyr/ModTemplate-StS2/wiki)
- [BaseLib Wiki](https://alchyr.github.io/BaseLib-Wiki/)
- [Harmony Docs](https://harmony.pardeike.net/)
- [Godot Docs](https://docs.godotengine.org/en/stable/getting_started/introduction/index.html)
- StS Discord `#sts2-modding` channel
