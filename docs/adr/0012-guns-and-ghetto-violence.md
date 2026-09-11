# ADR-0012: Guns and ghetto violence — comedy violence in the loop

## Status

Accepted (2026-09-10, owner-directed).

## Context

Owner directives: "I DO want guns in the game, you can shoot customers and your friends, maybe you can intimidate them to take the coffees. The NPCs can also shoot you back, and because the coffee shop is in the ghetto then sometimes you might just generate a shoot out and everyone dies."

The tone target is **comedy violence** in the How To Fis/ Gamble mold: violent acts are cartoon-physics funny (ragdolls, hitstop, over-the-top blood-free reactions), never grim, never tactical. The shop is in the ghetto, so violence is ambient, part of the texture of the neighborhood.

## Decision

- **Guns are core toys** (pistol/SMG/shotgun, from free low-poly packs): shoot friends, customers, machines, and the ceiling.
- **Intimidation mechanic:** aiming a gun at a customer makes them hand over money or their coffee (extortion tip into the shared till) instead of being served — an actual gameplay pressure tool, not just a visual.
- **NPCs fight back:** customers/NPCs can shoot players; deaths are ragdoll comedy and respawn at the counter.
- **Ghetto shootout event (scripted chaos):** a random chance per shift that a drive-by or walk-in shootout erupts; everyone in the shop (players, customers) can die; shop takes damage. This is the "catastrophe" generator for the replay system.
- **Violence is never required for progression** — serving coffee is always the win path; guns are the fun/emergent path.

## Consequences

- Health/damage system must handle bullet damage, extortion, and shootout events uniformly (see the fight spec).
- Customer AI needs fear states (flee, surrender, return fire).
- Audio/VFX need gunshots, impacts, and chaos cues (store packs).
- Content safety: keep it cartoon — ragdolls, no gore; Boston satire voice keeps it absurd.

## Alternatives considered

- Guns as pure cosmetic/LARP — rejected: owner explicitly wants mechanical intimidation and shootouts.
- Realistic shooter treatment — rejected: destroys the How To Fish comedy tone; not the game.