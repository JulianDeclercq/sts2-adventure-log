# Adventure Log — Roadmap

## Core Goal: Multiplayer-First Adventure Log

**Multiplayer is a primary target, not an afterthought.** The mod must produce a useful, per-player combat log across all connected players in a co-op session. `affects_gameplay: false` is kept so the mod is allowed in MP without version-matching.

Implications:
- Every event (card played, damage taken, relic procced) must be attributable to a specific player by Steam/Net identity.
- The log shows activity from **all** players in the session, not just the local one.
- Data model must carry a stable `NetId` / Steam ID per event.

## Non-Goals (for now)
- Post-game breakdown screen
- Cross-run persistence
- Damage leaderboard / rankings
- Configurable layout system
