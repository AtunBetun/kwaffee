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