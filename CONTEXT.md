# KWA FEE — Context

## What this is

A raucous online **first-person co-op** party game (Unity + Blender + Boston-accent satire) built by an adversarial-loop agent studio. 3-4 friends run a barely-legal ghetto coffee shop together, serve ballistic coffee under gunfire, shake down customers, shoot each other clean, and try to make quota before Big Tony takes the sign. Full v2 design: `.scratch/kwafee/v2-design.md` (17 specs listed there in section 13; the v1 design is archived under `.scratch/kwafee/backups/v1/` and superseded).

## Vocabulary

**KWA FEE** — the game's title; Boston-pronunciation of "coffee".

**Shop** — the single first-person play space: counters, 4 machines (Espresso, Grinder, Steam Wand, Ice Machine), the gun rack under the counter, the shared till, chairs, the crooked neon sign, the shop cat, the window out to the ghetto street.

**Shift** — one round, 3-4 minutes, party pacing. The Match / The Round. Shared quota + shared clock.

**The till** — the ONE shared wallet. Tips, extortion cash, and sales land there; upgrades (machine speed, gun stock, jukebox, beans) are bought by anyone from it. No per-player currency.

**Quota** — the shared win condition: serve N orders before the clock dies. Miss it → Big Tony takes a fixture.

**Big Tony** — the Landlord. Takes furniture on quota fail. Runs a shame list. Never compliments.

**Big Vinny** — the Manager, the game's deadpan Boston voice and narrator. Roasts everything, especially casualties.

**The verbs (v2)** — the player's ACTIONS, not speech: MOVE/LOOK (FPS), INTERACT (E raycast), FLING (hold-E charge throw), SHOOT (LMB; RMB = aim = intimidate), WORK (the shift itself). Voice chat is out of v2 scope.

**Intimidation** — aim a gun at a customer → they surrender cash or their coffee into the shared till. The pressure verb.

**Shootout** — a random ghetto event (drive-by or walk-in) where everyone in the shop can die; the biggest catastrophe source.

**Ragdoll** — death mode; animator off, limb rigidbodies drop with impulse; players + NPCs share one controller.

**The Shop Cat** — unkillable physics gremlin; autonomously knocks trays, sits on machines, helps (badly).

**Customers** — the job's heart: order, patience, fear states (surrender/flee/return fire if armed), regulars, street traffic.

**A catastrophe** — the shift's funniest/fastest mass-death or chaos moment — replayed in slow-mo at shift end (CATASTROPHE REPLAY), then awards + tabloid.

**The ghetto** — the setting: the shop sits on a rough street; ambient violence texture, drive-by lane, Big Tony's truck.

## Decisions

Every decision that matters (v2 pivot in ADR-0010: first-person co-op, guns + comedy violence ADR-0012, shared till ADR-0011, built-in Rigidbody physics ADR-0013, FishNet netcode ADR-0014, asset-store supply ADR-0015, minimal UI ADR-0016; authoritative server sharing client physics ADR-0001; Blender bpy pipeline ADR-0006 — machinery/cat/sign only, everything else store-sourced) is recorded as an ADR in `docs/adr/`. Read those before working on the relevant subsystem.

## Working agreements

- Expectation on entry: read `.scratch/kwafee/DECISIONS.md` and `.scratch/kwafee/EVIDENCE.md` first; resume from them, never re-derive settled decisions.
- v2 spine: one movement authority (Starter Assets behind PlayerMotor), one netcode framework (FishNet behind a seam), built-in Rigidbody physics behind ADR-0013 — no third-party engine, no competing frameworks glued.
- Third-party assets NEVER own the architecture: our seams (PlayerMotor, PlayerLook, PlayerInteraction, HealthComponent, WeaponController, PhysicsGrabber, ShiftManager, ShootoutDirector, RagdollController, AudioManager, LobbyUI, HUD) sit above adapters (ADR-0015).
- A party game that stutters is a dead game: performance regressions are blockers; no per-frame allocation in sim loops.
- Games without measurements don't close gates: each spec's Refined Gate names evidence (EditMode test, PlayMode probe via MCP, seeded-shift campaign report, screenshot sequence).
- Nobody compliments: neither Big Vinny, the RATER, nor the developers. Results speak.