# Adventure Log — StS2 Mod

## Project Overview

A Slay the Spire 2 mod that tracks and displays cards played during a run as a toggleable overlay (press F). Built with the [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) template.

- **Language:** C# / .NET 9.0
- **Engine:** Godot 4.5.1 (MegaDot variant)
- **Patching:** HarmonyLib (`0Harmony.dll` from game data)
- **Mod ID:** `AdventureLog`
- **affects_gameplay:** `false` (observation-only, safe for multiplayer)

## Roadmap

Before scoping new work, read `plans/roadmap.md`. It defines core goals (multiplayer-first), open questions, sequencing, and non-goals. If a new request conflicts with it, surface the conflict; don't silently drift.

## Analyze Fixes Generically

When diagnosing a bug or feature request triggered by a specific card, relic, power, or event, look for the underlying game mechanism (e.g., `EnergyNextTurnPower`, `CardPileCmd.Add`, `PowerModel.AfterEnergyReset`) and design the fix around that mechanism — not around the one card. If the same mechanism drives other cards/relics/events, the fix should cover them automatically. Only narrow to the specific case when the general fix is infeasible, and say so explicitly.

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

The build auto-deploys to: `<Sts2Path>/mods/AdventureLog/`

Game path is auto-discovered via `Sts2PathDiscovery.props` (Steam registry + common paths). Override manually in `Directory.Build.props` if needed:
```xml
<Sts2Path>C:/path/to/Slay the Spire 2</Sts2Path>
```

`Directory.Build.props` is gitignored (machine-specific paths).

## Mod Manifest (AdventureLog.json)

| Field | Value | Notes |
|-------|-------|-------|
| `has_pck` | `false` | Set to `true` only if you have a `.pck` file. Game rejects mods with missing declared assets. |
| `has_dll` | `true` | We have compiled code |
| `affects_gameplay` | `false` | Cosmetic/info mods MUST be false. Controls multiplayer connection checks. Wrong value causes desyncs. |
| `dependencies` | `[]` | No external mod dependencies |

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
GD.Print("message");
GD.PrintErr("error message");
```

Logs location:
- Windows: `%appdata%/SlayTheSpire2/logs/godot.log`
- macOS: `~/Library/Application Support/SlayTheSpire2/logs`
- Linux: `~/.local/share/SlayTheSpire2/logs`

## Dev Console

Open with any of: `~`, `` ` ``, `*`, `'`

Full reference (all 37 commands, decompiled): `docs/dev-console.md`.

Useful commands:
- `help` — list all commands
- `help <command>` — detailed help
- `card` — spawn cards for testing
- `showlog` — open live log window
- `open logs` — open log directory in file explorer

**Fast lookup:** card/relic/power IDs → grep Spire Codex (`curl -s "https://spire-codex.com/api/cards?lang=eng" | jq`). Dev console cmd syntax/args → `docs/dev-console.md` or decompile `MegaCrit.Sts2.Core.DevConsole.ConsoleCommands.*ConsoleCmd` (e.g. `CardConsoleCmd.cs` for `card`).

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

**In-tree example — `NTinyCard` for card icons:**
`AdventureLogCode/UI/Rows/TinyCardFactory.cs` instantiates `res://scenes/cards/tiny_card.tscn` and calls `SetCard` after `Ready` fires (see Gotcha #8 for the `Ready`-vs-`TreeEntered` timing).

Full inventory of reusable game list/row UIs in `docs/ui-reuse-options.md`.

## Known Gotchas

1. **`CombatManager.StartTurn` fires twice per round** — once for player, once for enemy. Use `SetupPlayerTurn` for player-only turn tracking.
2. **`has_pck: true` without a .pck file** — game silently ignores the mod. Set to `false` if not exporting a .pck.
3. **`CombatRoom` vs `NCombatRoom`** — `CombatRoom` is a model (no Godot methods). `NCombatRoom` is the Node. Use `NCombatRoom` for scene tree operations.
4. **Mod not in mod list but loaded** — mods without a config UI don't appear in the sidebar list. Check "X mods loaded" text on main menu.
5. **Early Access breakage** — game updates frequently break mods. Our Harmony patches may need manual fixes after each patch. Check decompiled sts2.dll changes.
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

Split by question type — data vs code. Pick the right tool first.

### Data lookups → Spire Codex first

Use [spire-codex.com](https://spire-codex.com) REST API when the question is **"what data exists"**: card IDs, stats, costs, rarities, localization keys, resource paths, monster HP/moves/intents, encounter comps, act placement, relic pools, event trees, ascension scaling, changelog diffs across versions.

```bash
curl -s "https://spire-codex.com/api/cards/{id}?lang=eng"
curl -s "https://spire-codex.com/api/monsters?act=2"
curl -s "https://spire-codex.com/api/relics/{id}"
```

GZip-compressed, 5-min cached. No decompile tax. Covers 14 languages.

### Code/API surface → ilspycmd

Use `ilspycmd` when the question is **"how does code run"**: method signatures, Harmony patch targets, publicizer gotchas, property accessors, hook state machines, Node class internals (`NTinyCard.SetCard`, `NCreature.Visuals`), lifecycle timing.

```bash
ilspycmd "path/to/sts2.dll" -t "MegaCrit.Sts2.Core.Nodes.Cards.NTinyCard"
```

Publicized DLL at: `.godot/mono/temp/obj/Debug/PublicizedAssemblies/sts2.*/sts2.dll`

Per gotcha #10 — when using a game component, decompile a game class that already uses it (e.g., `NDeckHistoryEntry.Reload()` for `NTinyCard`) to see exact setup pattern.

**Cross-code grep** — for "who calls X?" or "which classes implement Y?" questions, bulk-decompile once with `./Scripts/decompile-sts2.sh`. Output at `.decompiled/` (gitignored, ~3,300 files, ~18s). Re-run after game updates; script skips if fresh.

### Fallbacks

1. **Search GitHub for other STS2 mods** — community mods are the real documentation.
2. **Official wikis** — often incomplete (BaseLib's CardModel docs are "TODO").
3. **Property not accessible via publicizer** → `Traverse.Create(instance).Property<T>("PropName").Value`.

### Quick routing cheatsheet

| Question | Tool |
|---|---|
| "What's the ID/cost/damage of card X?" | Spire Codex |
| "Icon/portrait path for relic Y?" | Spire Codex |
| "All monsters in act 2 / encounter Z?" | Spire Codex |
| "Localization key for card X?" | Spire Codex |
| "How patch `CardModel.OnPlayWrapper`?" | ilspycmd |
| "What property holds block on `NCreature`?" | ilspycmd |
| "Which method fires on combat start?" | ilspycmd |
| "What args does method X take?" | ilspycmd |

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
