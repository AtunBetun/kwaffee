# KWA FEE — Context

## What this is

A raucous online party game: 3-4 friends run a barely-legal Boston coffee shop (the shop), flinging physics coffee, serving demanding customers, and absolutely wrecking each other for tips. Built in Unity + Blender, authored entirely by an autonomous BUILDER/RATER agent loop.

## Vocabulary

**KWA FEE** — the game's title; Boston-pronunciation of "coffee".

**Shop** — the single play space: counters, 4 machines (Espresso, Grinder, Steam Wand, Ice Machine), chairs, the crooked neon sign, the shop cat.

**Shift** — one round. 3-4 minutes, party pacing. The Match / The Round.

**Quota** — the shared goal: serve every order before the clock dies. Miss it → Big Tony takes a fixture.

**Big Tony** — the Landlord. "Partially" takes furniture on quota fail. Runs a shame list.

**The five verbs** — FLING (throw cup), CHUG (coffee drug), FIX (wrench repairs), STEAL (take a friend's carried tray), SABOTAGE (overpressure/steam/bean-sack/trip-wire).

**The Mule** — the delivery van; physics-funny fixed-route drive to dealers for beans.

**Big Vinny** — the Manager, the game's deadpan Boston voice and narrator. Never compliments.

**The Shop Cat** — unkillable physics gremlin; autonomously knocks trays, sits on machines, helps (badly).

**The dealer** — Big Sal's Specialty Beans (good, priced), Rosie's Clean Beans (reliable, boring), Ma Paddy's Pantry (pre-war beans: comedy).
**The T** — chowdah — the accent's verbal tics, strongly KWA FEE.

**Dealer** — one of three bean suppliers, each with memory and grudges.

**Ma Paddy** — the shady dealer; prices rise for you specifically after you short her.

**A catastrophe** — a shift's biggest fail moment (geyser, chain-drop, van incident) — replayed in the CATASTROPHE REPLAY.

**The rest** — see the full spec at `.scratch/kwafee/spec.md`.

## Decisions

Every decision that matters (architecture: Unity 6 + Blender bpy scripts; authoritative server sharing client physics; two-tier BUILDER/RA soundtrack; etc.) is recorded as an ADR in `docs/adr/`. Read those before working on the relevant subsystem.

## Working agreements

- Expectation on entry: read `/DECISIONS.md` and `/EVIDENCE.md` in `.scratch/kwafee/` first.
- A party game that stutters is a dead game: performance regressions are blockers.
- Nobody compliments: neither Big Vinny, the RATER, nor the developers. Results speak.