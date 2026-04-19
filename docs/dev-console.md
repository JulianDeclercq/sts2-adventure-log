# StS2 Dev Console — Commands Reference

Decompiled from `sts2.dll` (publicized). Every command here lives in `MegaCrit.Sts2.Core.DevConsole.ConsoleCommands.*ConsoleCmd` and is registered at runtime.

## Opening the Console

Press one of: `` ` `` (backtick) · `~` (tilde) · `*` · `'`

Type `help` inside the console for the in-game list. Tab autocompletes.

## Syntax Conventions

- `<arg>` — required
- `[arg]` — optional
- `type:name` — expected type
- `a|b` — choose one literal
- **Screaming snake case** for IDs: `BODY_SLAM`, `ENTROPIC_BREW` (not `Body Slam`).
- **Networked** commands sync to all MP peers; non-networked run on the local instance only.

## Built-ins (handled by `NDevConsole` itself)

| Command | Effect |
|---|---|
| `clear` | Clears console buffer |
| `exit` | Closes the console overlay |

## Commands

### Combat / State Mutation

| Cmd | Args | Networked | Description |
|---|---|:-:|---|
| `block` | `<amount:int> [target-index:int]` | ✓ | Gives block to player (default) or target at index (`0` = player, `1+` = creatures in combat). |
| `damage` | `<amount:int> [target-index:int]` | ✓ | Damages all enemies, or target at index (`0` = player). |
| `heal` | `<amount:int> [index:int]` | ✓ | Heals player (or indexed creature) by `amount` HP. |
| `energy` | `<amount:int>` | ✓ | Adds energy to the player. |
| `draw` | `<count:int>` | ✓ | Draws `count` cards. |
| `kill` | `<target-index:int>\|'all'` | ✓ | Kills one target by index, all with `all`, or the first if no args. |
| `die` | — | ✓ | Kills the player. |
| `win` | — | ✓ | Wins current combat. |
| `godmode` | — | ✓ | Toggles player invincibility. |
| `power` | `<id:string> <amount:int> <target-index:int>` | ✓ | Applies a power to creature at index. IDs are power model IDs. |
| `upgrade` | `<hand-index:int>` | ✓ | Upgrades the card at hand index (`0` = leftmost). |
| `enchant` | `<id:string> [amount:int] [hand-index:int]` | ✓ | Enchants a card in hand with the given enchantment ID. |
| `afflict` | `<id:string> [amount:int] [hand-index:int]` | ✓ | Applies an affliction to a card in hand. |
| `remove_card` | `<id:string> [pileName:string]` | ✓ | Removes a card from Hand or Deck (ID in screaming snake case). |
| `instant` | — | — | Turns instant mode on (skips animations). |

### Run / Navigation

| Cmd | Args | Networked | Description |
|---|---|:-:|---|
| `travel` | — | ✓ | Enables jumping to any room on the map. |
| `room` | `<id:string>` | ✓ | Jumps to a specific room. |
| `fight` | `<id:string>` | ✓ | Jumps to a specific encounter. |
| `event` | `<id:string>` | ✓ | Jumps to a specific event. |
| `ancient` | `<id:string> <choice:string>` | ✓ | Opens an ancient event with the chosen option pre-selected. |
| `act` | `<int\|string:act>` | ✓ | Jumps to an act (by number or replaces current with named act). |

### Economy / Rewards

| Cmd | Args | Networked | Description |
|---|---|:-:|---|
| `gold` | `<amount:int>` | ✓ | Adds (or with negative, removes) gold. |
| `stars` | `<amount:int>` | ✓ | Adjusts player stars. |
| `potion` | `<id:string>` | ✓ | Adds a potion to the belt (e.g. `ENTROPIC_BREW`). |
| `relic` | `[add\|remove] <relic-id:string>` | ✓ | Adds (default) or removes a relic. |

### Debug / Dev-Only

| Cmd | Args | Networked | Description |
|---|---|:-:|---|
| `dump` | — | — | Dumps the Model ID database to console & logs. |
| `log` | `[type:string] <level:string>` | — | Sets log level per log type. See "Enum values" below. |
| `art` | `<type:string>` | — | Lists content of given type missing art. |
| `open` | `logs\|saves\|root\|build-logs\|loc-override` | — | Opens a common path in the OS file browser. |
| `getlogs` | `<name:string>` | — | Zips logs into a file tagged with `name` and opens the folder. |
| `cloud` | `delete` | — | Deletes all save files from Steam Cloud. |
| `trailer` | — | — | Toggles UI-element show/hide via `0`–`9` and `+/-` keys. |
| `multiplayer` | — | — | Opens the multiplayer menu (or test scene with `test` arg). |
| `unlock` | `<type:string>` | — | Marks all cards/potions/relics/monsters/events/epochs/ascensions as discovered, or `all` to unlock everything. |
| `achievement` | `<operation:string> [id:string]` | — | Unlocks or revokes an achievement; no ID = all. |
| `leaderboard` | `[option:string] [name:string] <score:int> [count:int]` | — | Uploads score(s) to leaderboard. Option: `upload` (one score) or `random` (`count` random scores). |
| `sentry` | `<test\|message\|exception\|crash\|status> [text]` | — | Sentry error-reporting tests. `crash confirm` **terminates the game**. |

### `log` Enum Values (source of truth: decompile output)

`log <type> <level>` — the valid types are `LogType` enum members; levels are `LogLevel` enum members. Run `log` with no args or a wrong value to see both lists printed by the game.

## Targeting Cheatsheet for `block` / `damage` / `kill` / `power`

Target indices are positions in `CombatState.Creatures`:
- `0` — player
- `1+` — enemies in spawn order

Example:
```
block 30 1       # 30 block on first enemy
damage 9999 1    # murder first enemy
power WEAK 3 1   # apply 3 Weak to first enemy (ID must exist)
```

## Useful Combos for Adventure Log Mod Testing

Test damage-received UI:
```
block 30 1       # enemy gets 30 block
draw 5           # more cards to burn
energy 10        # more plays
```
Then attack. Adventure Log should show `-HP` + `(30 blocked)` + skull on kill.

Test sourceless damage:
```
damage 5 0       # 5 damage to player, no dealer
```
Should render without source prefix in the row.

## Source

All metadata (CmdName, Args, Description, IsNetworked) pulled from `MegaCrit.Sts2.Core.DevConsole.ConsoleCommands.*ConsoleCmd` at `.godot/mono/temp/obj/Debug/PublicizedAssemblies/sts2.*/sts2.dll`.

Regenerate with:
```bash
ilspycmd <sts2.dll> -t MegaCrit.Sts2.Core.DevConsole.ConsoleCommands.BlockConsoleCmd
```
