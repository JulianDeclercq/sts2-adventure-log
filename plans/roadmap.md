# Adventure Log — Roadmap

## Core Goal: Multiplayer-First Adventure Log

**Multiplayer is a primary target, not an afterthought.** The mod must produce a useful, per-player combat log across all connected players in a co-op session. `affects_gameplay: false` is kept so the mod is allowed in MP without version-matching.

Implications:
- Every event (card played, damage taken, relic procced) must be attributable to a specific player by Steam/Net identity.
- The log shows activity from **all** players in the session, not just the local one.
- Data model must carry a stable `NetId` / Steam ID per event.

## Landscape (related mods on Nexus)

### Mod 100 — STS2AdventureLogMod
Mature combat log mod. Known features (1.6.0):
- In-combat log on left side, resize + hide
- Post-game breakdown screen
- Current run / current combat / past combats views
- Cards used, damage sources (allies + enemies)
- Multiplayer: per-player stats, split by Steam username, stat comparison
- Block sources
- Relic trigger counts
- Healing received, cards drawn/discarded/exhausted
- Orb passive vs evoked, status effects tracker

### Adjacent
- SpireLink (387) — persistent run history across sessions, deck evolution
- Damage Meter (47), Skada (33), SlayTheStats (349)
- Deck Tracker (207), SeeEverything (159)

## Current State

- Toggle overlay (F key) — right side
- Unified `LogEvent` base + per-type subtypes (`CardPlayEvent`, `DamageReceivedEvent`, `RelicProcEvent`) under `AdventureLogCode/Events/`
- Every event carries `OwnerNetId` + `OwnerName` + `IsLocal` (resolved via `OwnerResolver` → `PlatformUtil.GetPlayerName`)
- Cards played history: card, turn, combat, owner name, target — NTinyCard scene reuse
- Damage-received tracking: blocked / HP lost / overkill / kill flag, source = card name when present else dealer name. Damage rows nested under parent card row (`DamageSubRow`)
- Relic procs tracking: relic name + id, targets, owner. Hover relic log row → fires `RelicBar.OnFocus` for native tooltip; remote procs skip hover (avoid misattribution); self-targets dropped
- Hover card row: native game tooltip + `CreatureHighlighter` reticle on owner + target
- Click card row: opens native `NInspectCardScreen`
- UI split into `UI/Rows/{CardEntryRow, DamageEntryRow, DamageSubRow, RelicEntryRow}.cs`; `AdventureLogPanel.cs` is container/refresh only (debounced)
- Skip splash logo patch
- `affects_gameplay: false` → MP-safe (mod allowed without version match)

## MP Hook Verification (non-blocker)

**Does `CardModel.OnPlayWrapper` fire on each client for every player's cards, or only for the local player's?**

Partial result (2026-04-14, host perspective only): host sees remote client cards with `Owner.NetId` populated correctly (`isLocal=False`, `owner=1000 (Test Client 1)`) alongside its own plays. Reverse direction (client seeing host plays) not captured because both instances on the same machine compete for `godot.log`.

No longer blocking: real MP testing happens across separate machines, not from here. Assume symmetric behavior until evidence says otherwise — proceed with roadmap step 2.

## Feature Additions

### 1. Per-Player Attribution — DONE (commit 3390ea1)

- `LogEvent` base carries `OwnerNetId` + `OwnerName` + `IsLocal` on every event.
- Resolved via `OwnerResolver` from `__instance.Owner.NetId` + `PlatformUtil.GetPlayerName`.

### 2. Damage Received Split — DONE (commits 98a7799, f08c824, 3c46404, f879129)

Hooked via Harmony postfix on `CombatHistory.DamageReceived(CombatState, Creature receiver, Creature? dealer, DamageResult, CardModel? cardSource)` — game's own per-damage history call, sync, gated to combat by caller at `CreatureCmd.Damage:176`. Cleaner than patching `Creature.TakeDamage` (predicted) — source attribution comes free via `cardSource`.

`DamageResult` provides `BlockedDamage`, `UnblockedDamage`, `OverkillDamage`, `WasTargetKilled`, `WasFullyBlocked`. Owner NetId derived from `receiver.Player?.NetId ?? dealer?.Player?.NetId`.

UI: damage rows nested under parent card row via `DamageSubRow`; refresh debounced to fix flicker; always-emit (target-label fallback dropped) so attacks show staggered.

### 3. Relic Procs — DONE (commits 555dea6, 5121b91, 7599839, 5f2314d)

Hooked via Harmony postfix on `RelicModel.Flash(IEnumerable<Creature>)` — single patch catches both overloads since parameterless `Flash()` delegates here. Cleaner than the predicted `AfterPowerAmountChanged` route.

Self-targets dropped (parameterless `Flash()` targets `Owner.Creature` redundantly). Hover relic row → fires `RelicBar.OnFocus` for native tooltip. Remote-player procs skip hover wiring to avoid misattribution to local relic bar.

### 4. Visual Polish Pass

- Animated entry fade-in on new events (short tween, 150ms)
- Header tabs: All / Cards / Damage / Relics (event-type filter)
- Combat collapse/expand (click combat header)
- Resize handle on panel left edge
- Settings: keybind rebinding, position (left/right), opacity slider
- Font consistency: use game's theme font where possible
- Per-combat summary footer: totals per player + combined

## Architecture Changes — DONE

### Tracker unification — done
`AdventureLogTracker` is hub for multiple event streams. Single `LogEvent` base + subtypes (`CardPlayEvent`, `DamageReceivedEvent`, `RelicProcEvent`) under `Events/`, one list, every event carries `OwnerNetId` + `OwnerName` + `IsLocal`.

### Patches added
- `Patches/CardPlayPatch.cs`
- `Patches/DamageReceivedPatch.cs` (uses `CombatHistory.DamageReceived`)
- `Patches/RelicProcPatch.cs` (uses `RelicModel.Flash`)
- `Patches/OwnerResolver.cs` — shared NetId → display name helper
- `Patches/CombatPatch.cs`, `UiInjectionPatch.cs`, `SkipSplashPatch.cs`

### UI refactor — done
- `UI/Rows/CardEntryRow.cs`
- `UI/Rows/DamageEntryRow.cs` + `DamageSubRow.cs` + `DamageColors.cs`
- `UI/Rows/RelicEntryRow.cs`
- `UI/AdventureLogPanel.cs` — container + debounced refresh only
- `UI/CreatureHighlighter.cs` — shared reticle helper

## MP Fallback Strategy

If the open test shows `OnPlayWrapper` fires local-only (and we cannot find a networked alternative):
- Option 1: patch the send/receive action on the net layer (find where PlayCard is serialized across the wire).
- Option 2: opt-in P2P broadcast of our own event records over the game's net service. Risk: order/timing drift, MP-unsafe behavior. Avoid unless option 1 fails.
- Option 3: ship MP as "your own log only, with 'other players played cards' marker from net signals." Degraded but safe.

Decision gate: run the `OnPlayWrapper` MP test before committing to damage/relic work.

## Sequencing

1. ~~Architecture refactor — unified event timeline, split UI files, per-player attribution everywhere~~ — DONE
2. ~~Damage received tracking (research + patch + row type, verify MP again)~~ — DONE
3. ~~Relic procs (research + patch + row type, verify MP again)~~ — DONE
4. **Visual polish (tabs, collapse, animation)** — NEXT
5. Settings (keybind, position)

Each step: build, manual test (solo AND 2-player MP), commit. Keep `affects_gameplay:false` throughout.

**Outstanding MP verification:** cross-machine 2-player test of damage + relic events still pending (same-machine `godot.log` contention blocked it during card-play check). Run before step 4 commits.

## Bugfixes (from 2026-04-16 MP test)

- Die For You and Territorial cards have no tooltip on hover
- Cards that recall from discard (e.g., resurrect/recall effects) should show which card was brought back
- Vulnerable hover shows no status effect tooltip, but Doom/Weak do — likely because Vulnerable displays post-total stacks
- ~~Last hit that kills a creature isn't logged~~ — fixed (c7a3885): patched `CreatureCmd.Damage` postfix instead of `CombatHistory.DamageReceived` (the latter is gated behind `!IsEnding`, which flips true on the killing hit)
- Potions that give cards should log which card was taken
- Card rewards taken should be shown in log
- Teammate relics should show their tooltip on hover (not in own RelicBar, so native `RelicBar.OnFocus` misses them)
- ~~Strength changes like "+-8 Strength" should render as "-8 Strength", colored red for negative~~ — fixed: omit the "+" sign for negative deltas, render in red instead of buff/debuff color
- Relic hover for own relics appears broken in MP — investigate whether `RelicBar.OnFocus` targets wrong bar

## Non-Goals (for now)

- Post-game breakdown screen
- Cross-run persistence
- Damage leaderboard / rankings
- Configurable layout system
