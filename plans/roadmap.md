# Adventure Log — Roadmap

## Core Goal: Multiplayer-First Adventure Log

**Multiplayer is a primary target, not an afterthought.** The mod must produce a useful, per-player combat log across all connected players in a co-op session. `affects_gameplay: false` is kept so the mod is allowed in MP without version-matching.

Implications:
- Every event (card played, damage taken, relic procced) must be attributable to a specific player by Steam/Net identity.
- The log shows activity from **all** players in the session, not just the local one.
- Data model must carry a stable `NetId` / Steam ID per event.

## TODO
- We should add card draw (e.g. soul)
- Enemy dying of doom doesnt show
- In MP, daughter of the wind relic didnt show on hover when hovering ni the panel, it just gave the tooltip (didnt hover on bar itself)
- game felt kind of laggy at combat 28 , do we have any leaks or anything? not sure if it's the mod. Think it's bc we were playing a shiv build. Could we
do the ui on a separate thread instead so it's not blocking the main game thread
- When scrolling / have scrolled, don't autoscroll up when a new entry is added (from your multiplayer)
- When hovering ove rpartners relic, dont show the card on top since it blocks the tooltip (see screenshot)
- Shiv damage didnt exactly show how much it did (bc of power that gives more shiv damage if it's first)
- +1 block from afterimage power didnt hsow that it was from afterimage in the log

## Maybe later
- Card Retained — low signal; only useful for Retain-mechanic decks. Revisit if a class makes it central.
- Creature Death event — `Hook.BeforeDeath` / `Hook.AfterDeath` are async state machines (fragile to Harmony-patch). `DamageReceivedPatch` already records `wasKilled` from `DamageResult`, so most kills surface today. Doom-specific gap needs separate diagnosis.
- Orb Channeled / Evoked — no orb-using class confirmed in StS2 yet; revisit when one lands.
- Stars / Gold / Forge — out-of-combat or cosmetic noise.

## Non-Goals (for now)
- Post-game breakdown screen
- Cross-run persistence
- Damage leaderboard / rankings
- Configurable layout system
