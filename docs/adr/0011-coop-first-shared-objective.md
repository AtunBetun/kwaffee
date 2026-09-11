# ADR-0011: Co-op first — shared objective, shared till

## Status

Accepted (2026-09-10, owner-directed).

## Context

"Needs to be collaborative, where we are sharing the objective, money, etc. This makes it way funner" — the owner's directive for v2. The v1 design was shared-quota but individual tips and theft verbs (STEAL) as a headline mechanic. v2 inverts the priority: the shift is a **co-op shift**.

## Decision

- **Shared win condition:** the shift quota (serve N orders before the clock dies) is the same for every player; one failed shift is everyone's failure (Big Tony takes a fixture).
- **Shared economy:** one till. Tips, extortion money, and sales all land in the shared till. Upgrades (machine speed, gun rack stock, jukebox…) are bought from the shared till by any player.
- **Individual accountability stays, as fun not as loss:** per-player score keeps sanity for the awards ceremony (most tips served, most chaos, culprit of the catastrophe, junkie). No per-player currency, no player-vs-player economy.
- **Griefing is second-class:** friendly sabotage (tripping a co-worker grab, shooting a friend clean, stealing a carried tray) is allowed as comedy; it never transfers money or blocks the shared objective permanently.

## Consequences

- All economy specs (shift/till, upgrades) model one shared currency; no per-player wallet in the core loop.
- The awards/culprit system reads from player-stats events, not wallets.
- Theft/tray-stealing is a gag, not a mechanic with economic meaning.

## Alternatives considered

- Keep per-player money + STEAL verb — rejected: owner called collaborative fun "way funner"; v1's theft economy fights the co-op framing.
- Full shared everything with no stats — rejected: awards/catastrophe replay need per-player event attribution to be funny.