# kwaffee-6id — Deepen the Shift: one seam for player, bot, and sim

**Status:** open
**Labels:** ready-for-agent
**Type:** feature

## Description

# Spec: Deepen the Shift — one seam for player, bot, and sim

Feature slug: `shift-seam`.

## Problem Statement

The Unity runtime's gameplay rules are owned by a 324-line orchestrator (`CoreGame`) while `Barista`, `CoffeeCup`, `ServeZone`, and `Machine` each hold a back-reference into it and call ten-plus internal methods upward. Understanding one behaviour — a serve, a hit, a steal — requires bouncing across four or five modules. The bugs that surfaced during repair (single-cup overdose unreachable, free chug, machines not wiring into the saved scene, duplicate chug logic) were all in the caller glue (damping, distance checks, backlogs), not in the pure rule functions, and that glue is untested. The `/sim` harness deep-copies prefab references and reaches into internals (`GrabOrSpawnRackCup`) instead of driving the same commands a player sends, so it cannot prove a player's experience.

## Solution

One deep **Shift** module behind one seam. Every actor — the human's keyboard, the bot policy, and the SimHarness — sends gameplay intent through the same `Command` surface and reads round state through `Metrics`. The MonoBehaviours keep only what Unity requires of them: kinematic state, visuals, and translation of physics callbacks into Shift calls. All verdict logic lives behind the seam, in one place, testable through it.

## User Stories

1. As a player, I want every verb I press (grab, fling, chug, fix, steal, sabotage) to resolve through the same rules the sim executes, so that scripted metrics reflect what I experience.
2. As a player, I want machine health, order patience, quota, and tips to stay consistent between the HUD and the actual round state, so that the numbers I see are true.
3. As a player, I want my chug to drain the held cup through the same path as every other chug, so that greedy drinking has one predictable consequence.
4. As a bot, I want to move, aim, grab, fling, chug, fix, steal, and sabotage through the same command surface as a human, so that a 4-bot shift exercises the real verb rules.
5. As the SimHarness, I want to issue commands through the same seam a player crosses, so that `/sim` numbers measure the player-visible round.
6. As the SimHarness, I want metrics (chaos/min, serves, tips per player, machine health, backlog) read from one `Metrics` surface, so that the report cannot drift from gameplay state.
7. As a maintainer, I want to trace a full serve — tray collision to tip credited — within one module, so that fixing a serve bug changes one place.
8. As a test author, I want to drive a shift's step, command players, and assert on reported outcomes without touching MonoBehaviours or prefabs, so that tests run headless and fast.
9. As a test author, I want the machine production/backlog/breakage behaviour observable through the seam, so that a broken machine test asserts what a player sees, not an array index.
10. As a Unity engineer, I want physics callbacks (collision, trigger) to remain on scene components, so that Unity's component model keeps working (a cup collider must still exist for collisions to fire).
11. As the RATER, I want `/sim` numbers before and after this refactor to match within tolerance, so that a restructure does not silently change game feel.
12. As a maintainer, I want the roast/commentary (Big Vinny) to fire only when the Shift records the underlying event, so that voice text and gameplay cannot desync.

## Implementation Decisions

- **Seam: the Shift module interface.** One seam. Three adapters cross it (input, bot, sim); components translate physics only. No module outside Shift holds a gameplay back-reference into the orchestrator.
- **Interface shape.**
  - `Begin(seed, actors)` — spawn/bind pool, reset round state. Replaces `Initialize`, `InitializeSim`, `CoreInit`, `RestartShift` reset bodies.
  - `Step(dt)` — the fixed-sim tick: machine degrade/production/backlog, order spawn/patience/expiry, cup recycling, quota check, Big Tony, end-of-shift awards. Replaces `Update`/`FixedUpdate`/`Step`/`Simulate` bodies (the player path and sim path both call the same `Step`).
  - `Command(actor, verb, args)` — sole entry for gameplay intent: move, aim, grab, fling (charge/release), chug (begin/end), drop, fix, steal, sabotage. Human input, bot policy, and SimHarness all call it.
  - `Metrics()` — sole exit: tips, serves, hits, misses, catches, chugs, overdoses, fixes, steals, sabotages, chaos events, machine health, backlog, order board, shift clock, quota state, round summary. The HUD and the sim report read only this.
- **Physics adapters stay as MonoBehaviours** (Barista, CoffeeCup, ServeZone, Machine) because Unity dispatches `OnCollisionEnter`/`OnTriggerEnter` to components; they hold kinematic/visual state and forward verdict-relevant events (cup hit, serve trigger) into the Shift via the seam. No rule lives in them.
- **Pure math modules stay** (VerbRules, FlingRules, RoundRules, MachineRules): the deletion test keeps them (each has 2-3 callers; deleting re-scatters constants). What moves out of callers is the glue — steal distance, hit targeting, chug damping decay, production backlog handling — which becomes private Shift logic, covered by Shift-seam tests.
- **Determinism invariant preserved.** The 50 Hz lockstep (Shift `Step(dt)` then physics `Simulate(dt)`, seeded `System.Random`, fixed `dt = TimeManager` step) must survive unchanged; the seam does not own physics stepping, the sim adapter does.
- **`/sim` prefab-copy hack dies.** With the seam, the SimHarness binds through `Begin(seed, actors)`; it no longer reaches into prefab fields by typing. Scene authorship (which prefab objects exist) stays a scene/editor concern, not a runtime rule concern.
- **Report surface stays a plain write** (text file + log); only the numbers' source changes.
- **No schema/files reorg outside `Assets/KwaFee/Runtime`.** Blend/GLB/manifest pipeline untouched (the Blender side is a separate consumer).

## Testing Decisions

- **The interface is the test surface.** Tests drive the Shift via `Begin`/`Command`/`Step` and assert on `Metrics()` outcomes — never on internal arrays, never on MonoBehaviour internals. A test that must change when the implementation changes is testing past the interface; delete it, don't re-pin it.
- **Existing EditMode tests (21, 3 suites) are the regression guard, not the spec.** Keep pure-rule assertions that defend cross-seam behaviour; fold the rest into Shift-seam tests covering the same observable outcome. Delete, never re-pin, any test that pins implementation (constant values, wiring).
- **New seam tests cover the previously untested glue:** a fling that actually lands (hit credits plasma owner, cup spills, culprit increments), a chug that drains the cup, a steal that respects distance and empty hands, machine production with backlog draining to the rack, quota miss bringing Big Tony, order expiry walking a customer. Each is one `Step` + one `Command` + one `Metrics` assertion.
- **Determinism regression guard:** the same seed + fixed scripted policy must yield identical metrics pre- and post-refactor. This is the one test that may touch the seam shape itself (it exists only while both worlds coexist), and it is removed after the transition.
- Prior art: the current EditMode suites (`FlingRulesTests`, `RoundRulesTests`, `VerbAndMachineRulesTests`) show the pure-rule style; the new tests are their successors at a higher seam.

## Out of Scope

- Netcode / authoritative multiplayer (artifact 10, ADR-0001 world unchanged).
- WebGL build, Unity licensing, container/VM runtime work.
- The Blender art pipeline (manifest, GLB, materials).
- New gameplay verbs, balance changes, new machines, or content.
- HUD restyling; the IMGUI HUD stays, reading `Metrics()` instead of live object state.
- Deleting the pure math modules wholesale (see Implementation Decisions — they stay as Shift-internal implementation).

## Further Notes

- Kick off with review Candidate 1 (collapse into one seam); Candidates 2 and 3 land inside the same move; Candidate 4 is handled by moving caller glue, not by deleting rule modules.
- ADR-0001 (shared physics world) and ADR-0002 (two-tier review loop) are untouched; no ADR is reopened.
- The repaired runtime fixes (rack grab, CHUG-through-VerbRules, machine objects, TipLedger owner-travel, `Time.fixedDeltaTime`, `manualSim` guard) are invariants this refactor must preserve — each is already defended by a passing test today.
- After the transition, `Machine` component's only job is scene-side identity for colliders; machine health/timers/backlog live with the Shift's machine state (one module, not three parallel arrays).

## Acceptance Criteria

All gameplay intent enters via Shift.Command; all state exits via Metrics; 21 existing EditMode tests pass or are deliberately replaced at the seam; /sim numbers unchanged within tolerance; no rule lives in a MonoBehaviour

---

# kwaffee-4k0 — Full-game approval and release

**Status:** open
**Labels:** needs-triage
**Type:** task

## Description


Blocked by: 12

Artifact 13: three consecutive complete-session RATER evaluations with no blockers/majors and scores at least 8 on Fun, Feel, Coherence, Boston Voice, NPC Aliveness and Visual Identity. All preceding gates, real human ladder, 60 fps four-player performance, WebGL/desktop builds and truthful evidence must pass before release. Resolve the prompt's three-pass/two-review-cap contradiction explicitly; never silently count self-review as RATER review.


## Acceptance Criteria

-

---

# kwaffee-8nh — Balance and pacing

**Status:** open
**Labels:** needs-triage
**Type:** task

## Description


Blocked by: 11

Artifact 12: actual 20-shift four-bot simulation. Shifts 3–4 minutes, downtime below 15%, approximately 60% quota success and 30–50% close calls, every player's session includes theft, visible vendettas and party-rate cascades. Persist seed, measured counters, reproducibility evidence and self-review before RATER.


## Acceptance Criteria

-

---

# kwaffee-vau — Lobby and onboarding

**Status:** open
**Labels:** needs-triage
**Type:** task

## Description


Blocked by: 10

Artifact 11: room codes, name selection, one-screen tutorial with a funny first action. Gate: a real fresh player follows URL and laughs within 60 seconds with zero instruction. Do not fabricate or impersonate a human test.


## Acceptance Criteria

-

---

# kwaffee-7xn — Authoritative multiplayer

**Status:** open
**Labels:** needs-triage
**Type:** task

## Description


Blocked by: 09

Artifact 10: dedicated Unity server with shared scene and 30 Hz physics, prediction/reconciliation, interpolation, room codes and reconnect. Browser-compatible transport must be justified. Gate: three machines LAN plus two remote, packet-loss tests, no visible rubber-banding, server authority and 60 fps evidence. Run isolated; no fake networking or runtime LLM.


## Acceptance Criteria

-

---

# kwaffee-tbw — Original music and audio

**Status:** open
**Labels:** needs-triage
**Type:** task

## Description


Blocked by: 08

Artifact 9: authored code synthesis for shift loop, heat layer and every specified cue. No external samples or stock loops. Gate: no silent state, sane mix, heat supports comedy without fighting it.


## Acceptance Criteria

-

---

# kwaffee-8iu — Juice and delivery axis

**Status:** open
**Labels:** needs-triage
**Type:** task

## Description


Blocked by: 07

Artifact 8: hitstop, biggest-catastrophe slow-motion replay, shake, confetti, narration, awards, tabloids and Boston copy. No checkpoints or replay restarts. Gate requires an actual friend's laugh, recorded evidence and a retest; cannot be replaced by simulated human feedback. Produce playtest build and ten-question feedback card.


## Acceptance Criteria

-

---

# kwaffee-x0i — Visual identity

**Status:** open
**Labels:** needs-triage
**Type:** task

## Description


Blocked by: 06

Artifact 7: every model, prop and screen checked against all eight LOOK rules. Blender-authored clay bodies, candy accents, felt cat/aprons, readable distinct silhouettes and spring/squash motion. Gate: any five characters and five props trace to named parent rules; no duplicate character silhouette or generic style.


## Acceptance Criteria

-

---

# kwaffee-1x1 — NPC cast

**Status:** open
**Labels:** needs-triage
**Type:** task

## Description


Blocked by: 05

Artifact 6: seeded utility/state behavior, NavMesh movement, persistent cafe memory and event reactions for customers, inspector, dealers, cat, Vinny and street life. No runtime LLM. Gate: reactions within one second; zero clips, stuck loops or ignored flames; at least two real-event gossip/reaction lines per shift; personalities respond differently to identical stimulus.


## Acceptance Criteria

-

---

# kwaffee-026 — The Mule and dealers

**Status:** open
**Labels:** needs-triage
**Type:** task

## Description


Blocked by: 04

Artifact 5: fixed-route physics drive, three dealers, coin-bag haggle and per-player grudge prices. Bean run must pressure without consuming the shift; shady outcomes funny at least 70%; Ma Paddy chase verified as a highlight.


## Acceptance Criteria

-

---

# kwaffee-kvu — Machines and sabotage

**Status:** open
**Labels:** needs-triage
**Type:** task

## Description


Blocked by: 03

Artifact 4: degradation, wrench repair, overpressure, steam and geysers. Actual machine cascades must produce replay-worthy moments at least 80% of the time. Interaction should teach itself.


## Acceptance Criteria

-

---

# kwaffee-bgd — Coffee drug and death

**Status:** open
**Labels:** needs-triage
**Type:** task

## Description


Blocked by: 02

Artifact 3: caffeine advantage, jitter tension, overdose, three-second respawn, event-specific Vinny roasts. Gate requires evidence that chugging is useful sometimes and overdose is funny without frustrating punishment.


## Acceptance Criteria

-

---

# kwaffee-zkm — Five verbs and griefing

**Status:** open
**Labels:** ready-for-human
**Type:** task

## Description


Blocked by: 01

Implement artifact 2 only after core-loop gate passes. All five verbs and AFK-cat handling. Each verb must be fun alone; actual scripted simulation must show at least 8 chaos events/minute, STEAL most used, no grief style above 50%. No invented metrics. Preserve per-player tips and shared quota.

2026-09-10: Verb command layer is implemented for CHUG, FIX, STEAL, and SABOTAGE alongside the existing FLING path. Pure rule tests pass 6/6 in Unity EditMode. The artifact gate remains open pending isolated `/sim` metrics, AFK-cat behavior, and human fun evidence; no gate score is claimed.


## Acceptance Criteria

-

---

# kwaffee-uq2 — Core loop prototype

**Status:** open
**Labels:** ready-for-human
**Type:** task

## Description


Implement artifact 1 of ../PROMPT.md. Gate remains pending until actual Unity simulation and observed play support charge readability, floaty flight, spill legibility, self-teaching controls, and consequential hits.

## Implementation plan

Goal: One playable local core-loop scene with a charge/fling/catch/serve cycle, authored clay/candy props, real Rigidbody impacts, and a seeded CLI harness exercising the same gameplay components.

Architecture: Unity 6000.6.0f1, URP, Input System already installed. Separate gameplay components from input and presentation. Simulation drives the same commands and physics used by the player at 30 Hz. Artifact 1 is explicitly local; netcode remains artifact 10.

Spec: ../PROMPT.md, artifact 1 and global art, security, evidence, and budget constraints.

- [x] Cheap BUILDER route was attempted, then replaced with reviewed local implementation after provider tool-isolation failures. Source, tests, reproducible Blender assets, and setup instructions are present under the approved workspace paths.
- [x] Inspected generated paths and scripts. Generated code has no automatic editor execution, external downloads, or credential access.
- [x] Ran the authored Blender script headlessly and recorded actual asset measurements in `../art-builder-report.md` and `../EVIDENCE.md`.
- [ ] Compile C# and assemble assets/scene through the connected editor. EditMode rule tests pass, but gameplay tests and deterministic `/sim` remain blocked until a suitable container or VM runtime is available. Host Play Mode is intentionally not used.
- [ ] BUILDER self-review fixes implementation findings before RATER review.
- [ ] Submit to separate RATER only when /sim evidence exists. Do not award feel/fun scores from source inspection.

## Comments

2026-09-09: Session started. No game code existed in the active Unity project. Unity MCP identifies the active root as /Users/albertodesaintmalo/work/kwaffee/kwaffee. Docker 29.7.2 is reachable, but listed local images contain no Unity runtime. WebGL module remains absent. Core-loop gate is pending.

2026-09-09: Builder readiness probe returned BUILDER_READY, but automatic approval review rejected the subsequent project-bearing invocation: explicit authorization is required to transmit the private prompt, context, decisions, issue, and implementation brief to OpenRouter. No project-bearing invocation ran. Await that authorization before retrying; do not route the payload indirectly. Brief is saved at ../core-builder-request.md. No source, assets, test results, or RATER scores were generated.

2026-09-09: User instructed continuation after the transfer question. Tool-free cheap builder invocation resumed on codex/core-loop. Implementation output pending. Runtime gate remains blocked by missing verified Unity isolation and WebGL module; asset authoring and compilation can proceed.

2026-09-10: Core implementation is present. Blender build and Unity authored-art import completed; CoreLoop scene references all eight generated prefabs and is the only enabled build scene. Unity EditMode rule tests pass 3/3 and project-filtered console is clean after refresh. This ticket remains `ready-for-human` because `/sim`, Play Mode physics, player build, and feel gates require the documented Unity license/WebGL setup and an authorized isolated runtime.


## Acceptance Criteria

-

---

