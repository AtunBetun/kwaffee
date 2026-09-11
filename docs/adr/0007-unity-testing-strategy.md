# ADR-0007: Unity unit and integration testing strategy

## Status

Accepted.

## Context

Agents pick up Beads tickets and implement them, then must prove the work before closing. The acceptance bar is currently implicit: "run your tests and type checks." This ADR fixes what a test is in this repo, where it lives, and how agents run it — without which agents either skip testing or invent ad hoc harnesses.

Two hard constraints shape the answer:

- **Batchmode test runs are blocked.** `unity -batchmode -runTests` fails until a human activates the Unity Personal license in Hub; AGENTS.md forbids retrying licensing. The only run path today is the connected editor via Unity MCP (127.0.0.1:8080, `mcpforunity://tests`, `run_tests` job), which the owner authorized 2026-09-10 for in-editor execution including SimHarness.
- **Physics is the product.** Cup tossing, chug overdoses, machine sabotage — all lean on the engine's physics simulation. Pure unit tests cannot cover it; that gap is exactly what the existing `SimHarness` was built to close.

Current state (grounded): `com.unity.test-framework` 1.8.0 is installed. One test assembly exists — `kwaffee/Assets/KwaFee/Tests/EditMode/KwaFee.EditModeTests.asmdef` (`optionalUnityReferences: ["TestAssemblies"]`, references `KwaFee.Runtime`) with pure-rule NUnit tests: `FlingRulesTests`, `VerbAndMachineRulesTests`, `RoundRulesTests`. No PlayMode assembly exists yet. `SimHarness` (Runtime) drives the same `CoreGame.Step` and real engine physics (`Physics.defaultPhysicsScene.Simulate`) at the fixed 50 Hz step, seeded `System.Random`, `manualSim` guard, writing metrics to `Application.persistentDataPath/kwafee-sim/sim-report.txt`.

## Decision

Two tiers. Rule code is unit-tested in the existing EditMode assembly; anything touching scene state or physics is integration-tested in a new PlayMode assembly wired to the SimHarness.

### Tier 1 — Unit (EditMode, `KwaFee.EditModeTests`)

- **What:** pure static rule functions — business logic with no `GameObject`, no scene, no clock: currently `FlingRules`, `VerbRules`, `MachineRules`, `RoundRules`, `TipLedger`. New rule code with ticket acceptance criteria must land here.
- **Where:** `kwaffee/Assets/KwaFee/Tests/EditMode/*.cs` in the existing `KwaFee.EditModeTests` assembly.
- **Style (existing pattern, keep):** `using NUnit.Framework;` namespace `KwaFee.Tests`, `[Test]` methods, `Assert.That(actual, Is.EqualTo(expected).Within(0.001f))`. Boundary cases (clamps, thresholds, negatives), never implementation details, deterministic, no sleeps.
- **Rule for authors:** rules must stay pure and static. If a test forces you to touch `Time.time`, `Random`, or scene state — the function does not belong in EditMode; move the seam (pass values in) or classify it Tier 2.

### Tier 2 — Integration (PlayMode, `KwaFee.PlayModeTests`, new)

- **What:** end-to-end behavior reached through real scene + physics: cup flight and splatter, organic order spawn, machine degradation to breakdown, chug-OD, tip ledger flow across a short play window.
- **Where:** new `kwaffee/Assets/KwaFee/Tests/PlayMode/` with `KwaFee.PlayModeTests.asmdef` (`optionalUnityReferences: ["TestAssemblies"]`, reference `KwaFee.Runtime`, no Editor-only constraint — PlayMode). Assembly does not exist yet; creating it is part of implementing this ADR.
- **How:** these tests drive the **real `SimHarness`** — not a reimplementation. Instantiate it, run `Run(seed, seconds)`, then assert on the produced metrics (`chaos/min`, serves, per-player tips, hits/misses, chug/OD tallies, machine health). Bounded-range asserts (e.g. serves within a seeded corridor, tips all on players 0-3) rather than exact values: physics under a real simulation is not a fixed point.
- **Determinism:** seeded `System.Random` (CoreGame and harness seeds fixed per test), `manualSim` guard so `FixedUpdate` no-ops, fixed 50 Hz dt — same guarantees SimHarness already provides.

### Run path (all of it, both tiers)

- **In-editor only.** Test Runner launch via Unity MCP (`run_tests` → `get_test_job`), or `manage_editor` Play Mode for PlayMode tests. The editor is already connected and owner-authorized.
- **Never batchmode `-runTests`**, never retry licensing (AGENTS.md). If the editor is not connected, say so and stop — do not invent a host-side runner.
- **Before claiming `<promise>DONE</promise>`** on a code-changing ticket: the affected tier's tests must have actually run green in that session, and the result noted in `.scratch/kwafee/EVIDENCE.md` (including seed + minutes for Tier 2). One-line result, not a paste.

### What is not a test here

- Checking the compiled manifest against `core-meshes.json` is the manifest validator's job in the build script, not a Unity test.
- No tests that assert on implementation (field copies, forwarders, source text) — assert observable behavior only.
- No golden files of generated meshes; re-export drift is caught by the validator + disciplined re-generation (ADR-0006).

## Consequences

- New ticket acceptance criteria become explicit test targets: unit-level rules land in `EditMode`, play-level behavior in `PlayMode`, each with a named test that must pass before closing.
- First PlayMode assembly is new work; existing EditMode suite (3 files, pure rules) stays exactly where it is.
- The `/sim` harness (ADR-0002's numbers-first bedrock) now doubles as the integration test engine — investment already made gets reused, not re-built.
- Cost: PlayMode tests are slow (seconds of simulated time per run) and need the licensed editor; that is why the tier split exists — keep EditMode fast and license-free, spend PlayMode deliberately.

## Alternatives considered

- Single EditMode-with-physics tier — rejected: engine physics needs PlayMode; shoehorning it into EditMode yields flaky tests.
- Test everything through SimHarness — rejected: pure rules are far faster and more precise in EditMode; the harness can't direct-test a clamp boundary.
- A third visual/VR tier — rejected (YAGNI): SimHarness metrics + PlayMode asserts cover the gates; a human-fun pass stays manual per ADR-0002.