# Combat Log — Roadmap

## Core Goal: Multiplayer-First Combat Log

**Multiplayer is a primary target, not an afterthought.** The mod must produce a useful, per-player combat log across all connected players in a co-op session. `affects_gameplay: false` is kept so the mod is allowed in MP without version-matching.

Implications:
- Every event (card played, damage taken, relic procced) must be attributable to a specific player by Steam/Net identity.
- The log shows activity from **all** players in the session, not just the local one.
- Data model must carry a stable `NetId` / Steam ID per event.

## Landscape (related mods on Nexus)

### Mod 100 — STS2CombatLogMod
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
- Cards played history: card, turn, combat, player name, target
- NTinyCard scene reuse for authentic card art
- Hover: native game tooltip + target creature reticle highlight
- Click: opens native NInspectCardScreen
- `affects_gameplay: false` → MP-safe (mod allowed without version match)
- `PlatformUtil.GetPlayerName` used for owner resolution (resolves to Steam name if available)

## Critical Open Question (BLOCKS MP story)

**Does `CardModel.OnPlayWrapper` fire on each client for every player's cards, or only for the local player's?**

- If it fires on each client for every card played anywhere → we get full MP log via Harmony locally, no net sync needed.
- If it fires only for local plays → we must either patch a networked entry point (e.g. `PlayCardAction.ExecuteAction` on the receiving side) or sync events ourselves.

Test plan: see `docs/mp-test-onplaywrapper.md` (TODO) — run `/test-multiplayer 2`, play cards on both instances, compare `godot.log` entries per instance. Diagnostic log line added to `CardPlayPatch.Prefix`.

## Feature Additions

### 1. Per-Player Attribution (foundational, must work in MP)

- Replace `playerName` string with `(NetId, DisplayName)` pair on every event.
- Derive from `__instance.Owner.NetId` (current) plus `RunManager.Instance.NetService.LocalPlayer` for "is this me?" flag.

### 2. Damage Received Split (blocked / unblocked)

Goal: per incoming hit, record pre-block damage, block absorbed, HP lost. Attribute to source (enemy + intent card/attack name if derivable) AND to the *player* taking damage.

Research needed:
- Decompile `Creature.TakeDamage` / `ApplyDamage` flow
- Hook `DamageCmd` or patch on `Creature` HP delta
- Check `BeforeAttack` / `AfterAttack` hooks (may be async state machines — prefer concrete method patch)
- Verify the patched method runs on all clients or only one — mirror MP question above
- Source attribution: attacker creature + action context; victim attribution: which player's `Creature` is the target

Tracker addition:
```csharp
public record DamageReceivedEntry(
    ulong VictimNetId, string VictimName, uint? VictimCombatId,
    string SourceName, uint? SourceCombatId,
    int RawDamage, int Blocked, int HpLost,
    int TurnNumber, int CombatNumber);
```

UI: interleave with card plays in same timeline, different row style (red accent). Hover source → highlight attacker creature. Hover victim → highlight victim creature.

### 3. Relic Procs (per-player)

Goal: record each relic trigger with context (which relic, which player's relic, when, what it did if derivable).

Research needed:
- `RelicModel` lifecycle hooks — `AfterPowerAmountChanged`, `OnPlay` style hooks per relic subclass
- Prefer patching `RelicModel` virtual methods or a common base trigger point
- Enumerate active relics via `RunManager.Instance` or per-player model
- Confirm MP behavior: do all clients see remote players' relic procs?

Tracker addition:
```csharp
public record RelicProcEntry(
    ulong OwnerNetId, string OwnerName,
    string RelicName, string RelicId,
    string TriggerContext,
    int TurnNumber, int CombatNumber);
```

UI: relic icon (reuse game scene if exists — `NTinyRelic` or similar; research). Subtle gold glow accent. Owner color stripe.

### 4. Visual Polish Pass

- Animated entry fade-in on new events (short tween, 150ms)
- Header tabs: All / Cards / Damage / Relics (event-type filter)
- Combat collapse/expand (click combat header)
- Resize handle on panel left edge
- Settings: keybind rebinding, position (left/right), opacity slider
- Font consistency: use game's theme font where possible
- Per-combat summary footer: totals per player + combined

## Architecture Changes

### Tracker unification
`CombatLogTracker` becomes hub for multiple event streams.
- **B:** Single `LogEvent` base + subtypes (card/damage/relic), one list, every event carries `OwnerNetId` + `OwnerName`.

### Patches to add
- `CombatLogCode/Patches/DamagePatch.cs` — hook damage pipeline
- `CombatLogCode/Patches/RelicPatch.cs` — hook relic trigger

### UI refactor
`CombatLogPanel.cs` at 343 lines. Extract:
- `CardEntryRow.cs` — existing card row logic
- `DamageEntryRow.cs` — new
- `RelicEntryRow.cs` — new
- `CombatLogPanel.cs` — container + refresh only

## MP Fallback Strategy

If the open test shows `OnPlayWrapper` fires local-only (and we cannot find a networked alternative):
- Option 1: patch the send/receive action on the net layer (find where PlayCard is serialized across the wire).
- Option 2: opt-in P2P broadcast of our own event records over the game's net service. Risk: order/timing drift, MP-unsafe behavior. Avoid unless option 1 fails.
- Option 3: ship MP as "your own log only, with 'other players played cards' marker from net signals." Degraded but safe.

Decision gate: run the `OnPlayWrapper` MP test before committing to damage/relic work.

## Sequencing

1. **MP hook verification** (diagnostic log + `/test-multiplayer` run) — must complete first
2. Architecture refactor — unified event timeline, split UI files, per-player attribution everywhere
3. Damage received tracking (research + patch + row type, verify MP again)
4. Relic procs (research + patch + row type, verify MP again)
5. Visual polish (tabs, collapse, animation)
6. Settings (keybind, position)

Each step: build, manual test (solo AND 2-player MP), commit. Keep `affects_gameplay:false` throughout.

## Non-Goals (for now)

- Post-game breakdown screen
- Cross-run persistence
- Damage leaderboard / rankings
- Configurable layout system
