# ADR-0013: Keep Unity built-in Rigidbody physics — no third-party engine

## Status

Accepted (2026-09-10). Evidence: `.scratch/kwafee/research/physics-engines.md`.

## Context

The owner asked whether we can pull a physics engine from the Asset Store and delete chunks of beads (the v1 custom physics verb stack). Research verdict (2026-09-10, primary sources): no third-party engine replaces GameObject Rigidbody in-place. NVIDIA PhysX public builds are not a drop-in for Unity's internal PhysX; "physics engines" on the store (Obi $179, Rigidbody Character, dead GGPhys, unverifiable brands) are fluid/cloth/PBD feature packs, not rigid-body replacements; DOTS Physics, BEPU, Jolt, Klotho all require porting gameplay to a different model. At our body count (30-50 Rigidbody bodies) performance is a non-issue, and WebGL client + headless server sharing one scene graph makes built-in physics the only zero-risk path.

## Decision

- **Unity built-in Rigidbody** is the physics engine. No Asset Store or open-source physics dependency.
- **Server authoritative, not lockstep:** `Physics.defaultPhysicsScene.Simulate(fixedDt)` at the ADR-0001 fixed 30Hz tick on a Dedicated Server build; clients render interpolated 60fps with own-input prediction + reconciliation (FishNet `PredictionRigidbody`, ADR-0014). Determinism claim is same-machine/same-build/pinned 6000.6.0f1 only — never bitwise cross-platform.
- **Ragdoll players/NPCs** built on Rigidbody + CharacterJoint/configurable ragdoll rigs (adequate per research).
- **What we deleted:** the v1 custom physics/verb layer (grab/throw math, machine cascade logic) is replaced by standard Rigidbody + RigidbodyInterpolation + store-driven feel. This is the "delete a bunch of beads" the owner wanted — the physics was never the differentiator, the wiring is.

## Consequences

- If bitwise cross-platform determinism ever becomes a hard requirement (rollback/lockstep), the researched escape hatch is the ECS route (Kimbatt soft-float fork or Klotho FP64) — a full rewrite, gated, never assumed.
- No per-frame allocation growth in the sim loop (steady-heap guard stays a blocker).

## Alternatives considered

- NVIDIA PhysX public builds — not swappable into Unity, rejected.
- DOTS Physics / BEPU / Jolt / Klotho — ECS or native rewrite for zero gain at 30-50 bodies, rejected.
- Asset Store "physics engines" — unverifiable or wrong problem domain, rejected.
- Custom deterministic verlet/impulse sim for gameplay-authoritative objects — rejected for v2: FishNet's PredictionRigidbody on built-in physics covers prediction; a custom sim would be a parallel physics world (the exact desync risk ADR-0001 exists to prevent).