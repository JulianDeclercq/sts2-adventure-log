# Adventure Log — Roadmap

## Core Goal: Multiplayer-First Adventure Log

**Multiplayer is a primary target, not an afterthought.** The mod must produce a useful, per-player combat log across all connected players in a co-op session. `affects_gameplay: false` is kept so the mod is allowed in MP without version-matching.

Implications:
- Every event (card played, damage taken, relic procced) must be attributable to a specific player by Steam/Net identity.
- The log shows activity from **all** players in the session, not just the local one.
- Data model must carry a stable `NetId` / Steam ID per event.

## TODO
- We should add card draw (e.g. soul)
- Cards with X energy should show their stuff. e.g.  Dirge
- Enemy dying of doom doesnt show
- Potions dont show atm. weird with explosive ampule for example where the damage gets registered
- In MP, daughter of the wind relic didnt show on hover when hovering ni the panel, it just gave the tooltip (didnt hover on bar itself)
- game felt kind of laggy at combat 28 , do we have any leaks or anything? not sure if it's the mod. Think it's bc we were playing a shiv build. Could we
do the ui on a separate thread instead so it's not blocking the main game thread
- Show POST dexterity amount of total block gained after playing a card
- Daughter of the wind doesnt show +1 block -> again make sure to solve generically first if possible rather than card speicfici
- When scrolling / have scrolled, don't autoscroll up when a new entry i sadded (from your multiplayer)
- When hovering ove rpartners relic, dont show the card on top since it blocks the tooltip (see screenshot)

## Non-Goals (for now)
- Post-game breakdown screen
- Cross-run persistence
- Damage leaderboard / rankings
- Configurable layout system
