# KWA FEE — Artifact 1: Core Loop Prototype — Spec

Ticket: `kwaffee-uq2` (status `open`, label `ready-for-human`, 0 deps).
Artifact-1 is MOSTLY BUILT (playable 3-min shift, 4 machines, cups, SimHarness, 21 EditMode tests, 12 Blender meshes, live Play Mode probe recorded in EVIDENCE). This spec captures what ships, what gate evidence remains, and the `/sim` metrics contract every later artifact inherits. It is the artifact's definition of done, not a build plan.

---

## Problem Statement (from the user's perspective)

When the player picks up the game, the very first shift must already be the whole game: grab a cup, charge a throw, fling coffee at a serving window, feed a machine, serve a customer, and get a tip — all without being told, and all funny. Right now the structure exists (machines run, cups are physical, a round plays out, 21 EditMode tests pass, 12 Blender meshes land), but the gate was never honestly closed: feel is unmeasured, charge readability is unproven, flight comedy is unverified, and no recorded numbers exist from the `/sim` harness. The player's problem is that we do not yet *know* the loop is legible, floaty-funny, self-teaching, and measurable — and we must not claim a pass on structure alone.

## Solution (user perspective)

A playable, measurable core loop where the player grabs a cup, holds to charge (0.35–1.2 s, wind-up squash-stretch plus a code-drawn candy ring on the cup), releases to fling, and every outcome is visible and funny within about a second: a correct serve tips the named carrier, hitting a barista stuns them, a miss bounces and stains the floor. Demand lines and the HUD teach the loop with no tutorial screen. The `/sim` harness now records real, reproducible numbers into a committed report, so the gate closes on measurement — not on code existing. No new Blender meshes are needed; every charge/spill/hit feedback is code/2D.

## User Stories

1. As a fresh player, I want to grab a cup and hold to charge so that the cup visibly winds up (squash-stretch) and shows a candy ring on it, so that I understand I am charging without any instructions.
2. As a fresh player, I want a release before 0.35 s hold to simply drop the cup, so that sloppy throws never launch and I learn the minimum charge.
3. As a fresh player, I want launch speed to grow with hold time up to ~14 m/s, so that my throw feels in my control and stronger holds travel further.
4. As a fresh player, I want to see "TOO WEAK" / "TOO HOT" in the demand banner when I fling a bad serve, so that the game itself teaches me the charge curve.
5. As a fresh player, I want a correctly flung cup to tip whoever's name is on the tray at landing, so that a good serve is rewarded instantly.
6. As a player, I want a flung cup that hits a barista to stun them, so that misplacing a cup is comedy, not a dead input.
7. As a player, I want a missed cup to bounce and leave a floor stain, so that every fling has a legible consequence and physics is the joke.
8. As a player, I want a full-charge fling to stay airborne at least 0.8 s before landing or hitting something, so that flight reads as floaty-funny rather than an air-pop or sink-through.
9. As the developer, I want `/sim` (in-editor via `SimHarness.Run(seed, seconds)`) to exit clean and write a canonical report to a committed file, so that the gate closes on reproducible numbers.
10. As the developer, I want two same-machine `/sim` runs to produce a bit-identical `determinism_hash`, so that I can prove the harness is reproducible on the pinned engine.
11. As the developer, I want `chaos_per_min`, `downtime_pct`, `serves`, and `tips_by_player` present and finite in the committed report, so that the recording contract is real before later artifacts gate on thresholds.
12. As the developer, I want the `/sim` metric schema frozen (fields + the eight additions) so that Artifacts 2–13 gate on the exact same numbers.
13. As the developer, I want an observed self-teach session (owner host play, provisional) to complete grab→charge→fling→serve using only demand lines and HUD, so that I have provisional evidence the loop self-teaches before the real human test.
14. As the developer, I want charge/spill/hit feedback to be code/2D only, so that the Artifact-1 gate does not absorb new authored art and the ART RULE stays clean.

## Implementation Decisions

- **The gate is a measurement gate, not a code gate.** It stays OPEN until B5's closing evidence exists. No "gate passed" claim on structure + tests + live probe alone.
- **In-editor `/sim` invocation is the evidence-equivalent for the CLI criterion.** `SimHarness.Run(seed, seconds)` via MCP `execute_code` drives `CoreGame.Step` plus real engine physics at fixed 50 Hz with `manualSim` guard; it is headless-shaped (no input, no presentation). Do NOT retry Unity batchmode — it is license-blocked and deferred to the Artifact-10 headless server.
- **`/sim` metric contract (inherited by Artifacts 2–13), exactly one canonical gate run per artifact:**
  - Seed `20260910` (pinned artifact-1 date), 180 s (one 3-min shift), 4 bots, 50 Hz fixed step, `CoreGame.InitializeSim(seed, botMode: true)`.
  - Existing fields kept verbatim (no renames — they are EVIDENCE's language): `chaos_per_min`, `chaos_total`, `serves`, `tips_total`, `tips_by_player`, `shots`, `hits`, `misses`, `catches`, `chugs`, `overdoses`, `fixes`, `sabotages`, `steals`, 4× `machine_health`, `broken`, `rack_cups`, `expired_orders`, `grab_tries`/`grab_success`, bot positions, per-bot verb attempts.
  - Added (one-line, zero renames): `seed=`, `determinism_hash=` (FNV-1a over step-count, serves, chaos, tips per player, broken machines), `quota_served=`, `downtime_seconds=` + `downtime_pct=`, `machine_cascades=` (events where ≥2 machines broken within a 20 s window).
  - Chaos/min is **recorded, not gated**, at Artifact 1 (≥8/min is the Artifact-2 blocker), so Artifact 1 does not silently absorb a later gate. No metric without a gate consumer — the eight additions all have consumers in Artifacts 2/4/12.
- **Charge curve contract:** hold linear 0→1.2 s; release below 0.35 s hold = drop (no launch); launch speed monotonic in hold to ~14 m/s. **No trajectory arc** — wind-up carries readability: cup squash-stretch (THE LOOK rule 7, fling winds up like Gamble) + hold-time ring on the cup (flat candy UI, rule 8, code-drawn 2D, NOT a Blender mesh). Miss feedback is the teacher ("TOO WEAK"/"TOO HOT" in the Boston demand banner).
- **Flight & hit consequences (keep):** spill = bounce + floor stain decal (code); hit barista = stun; serve = tip to carrier; miss = cup carries on (physics is the joke).
- **Blender art: none remaining.** The 12 meshes (8 + 4 machines, 17,112 tris ≤ 35,000 cap, grounded, manifest-validated) satisfy the ART RULE for everything Artifact 1 renders. Any prop added before gate close MUST enter via the bpy pipeline + manifest (required-name, ≤35k, y=0 grounded, per-submesh material indices) and the `Import Authored Art` step — never a shortcut.
- **Determinism is same-machine, same-build, pinned engine 6000.6.0f1, fixed 50 Hz via TimeManager + manualSim ONLY.** `determinism_hash` evidence always comes from two same-machine runs. Cross-machine consistency is deliberately NOT claimed (Artifact-10's server is the single authority); stated loudly so no later artifact reads reproducibility into it.
- **Risky-assumption handling (do NOT inherit as "fixed"):** machine colliders are non-trigger and FIX/SABOTAGE target by position math — proximity highlight is deferred to Artifact 4; owner-as-human datapoint stays provisional, the formal human ladder sits at Artifacts 8/11.

## Testing Decisions

- **What makes a good test here:** observable contract behavior a plausible bug would break — charge guard, monotonic launch, airborne minimum, hit consequence, heap stability. Do not assert wiring, field copies, or source text.
- **Prior art:** the 21 existing EditMode tests are green and are the base suite; new tests are added only where the ACs add genuinely uncertain edge cases.
- **AC-3 guard in EditMode:** hold < 0.35 s never launches; launch speed strictly monotonic in hold 0.35→1.2 s.
- **AC-4 in EditMode:** full-charge flight stays airborne ≥ 0.8 s and lands or hits something (no air-pop, no sink-through).
- **AC-8 steady-heap probe:** no per-frame allocation in `CoreGame.Step` / `SimHarness` loop beyond pooled objects — a regression here is a blocker per standards.
- **AC-1 / AC-2 via the live `/sim` run:** in-editor invocation (authorized 2026-09-10), commit report `sim/gate-1/20260910.txt`, run twice same-machine, assert `determinism_hash` matches bit-identical; confirm `chaos_per_min`, `downtime_pct`, `serves`, `tips_by_player` present and finite (recording only, no thresholds at Artifact 1).
- **AC-5 / AC-6 via host Play Mode observation:** charge readability, flight fun, spill legibility, hit consequence, and the self-teach grab→charge→fling→serve cycle, recorded as live-state probes + screenshots (the black-screen render artifact is known — probes, not pixels). Self-teach evidence is provisional (owner's own host session); the full naive-human laugh test is explicitly NOT in Artifact-1 scope.

## Out of Scope

- **CUT:** per-cup liquid volume state machine (spill is binary: intact/stained); charge power tiers beyond linear hold; combo/streak scoring; wobble physics beyond Rigidbody (visual wobble only).
- **CUT to later artifacts:** hitstop/slow-mo/camera shake → Artifact 8; Shop Cat AFK → Artifact 2; machine degrade/cascade depth → Artifact 4; tabloid/upgrades → round-structure artifacts. The awards summary is already built — keep, it's free.
- **Arc overlay** is a candidate Artifact-8 juice addition, not Artifact-1 scope.
- **Full naive-human laugh test** is not in Artifact-1 scope (spec's gold-standard human test sits after Artifact 8 and at the end).
- **No new Blender meshes** for any Artifact-1 gate evidence.
- **Unity Personal license + WebGL Build Support** are human-pending; NOT needed for the Artifact-1 gate (in-editor play only), but blocking WebGL evidence and the Artifact-10 headless server. Never retry batchmode.
- **The 3-RATER-pass contradiction (spec B2):** ops caps reviews at 2/artifact. Preserve the 2-review ceiling; record the contradiction in EVIDENCE; 2 consecutive ≥8/10 with 0 blockers/0 majors in the 2 allowed reviews = "gate-ready pending policy correction". Never burn astra on a 3rd review to satisfy a contradictory clause. The human must correct the policy before ANY artifact formally closes.

## Further Notes

- **External dependency — none from prior artifacts** (Artifact 1 is first). Inputs already landed: 12 meshes + scene (EVIDENCE 2026-09-10), `CoreGame`/`VerbRules`/`RoundRules`/`SimHarness`, 21 EditMode tests green, owner authorization for in-editor `/sim` + host Play Mode.
- **Risky assumption 3:** owner-as-human datapoint is weak (design-aware, self-interested) — recorded as provisional; never promoted to stronger evidence without the real human test.
- **SimHarness constraint:** built under the authorization that container/VM stays the ceiling for anything network-facing; it steps the real engine physics scene, so its numbers are the same numbers Play Mode sees — no separate feature needed.
- **Chaos/min is deliberately a later gate** (Artifact 2 blocker ≥8/min) — Artifact 1 records, never gates, it.

## Refined Gate

The Artifact-1 gate closes ONLY when all of the following hold, verified and recorded in EVIDENCE:

- **AC-1** — `/sim` seed `20260910`, 180 s, 4 bots exits clean and writes all contract fields (B4); a second same-machine run reproduces `determinism_hash` bit-identical. Report committed at `sim/gate-1/20260910.txt`.
- **AC-2** — `chaos_per_min`, `downtime_pct`, `serves`, `tips_by_player` present and finite in the committed report (recording only; no thresholds at Artifact 1).
- **AC-3** — charge hold < 0.35 s never launches (EditMode guard); launch speed strictly monotonic in hold 0.35→1.2 s.
- **AC-4** — full-charge flight stays airborne ≥ 0.8 s and lands or hits something (no air-pops, no sink-through).
- **AC-5** — every hit has a visible consequence within ~1 s: serve → tip to last carrier; barista hit → stun; miss → bounce + stain. No silent outcomes.
- **AC-6** — self-teach: an observed session (owner host play, provisional) performs grab→charge→fling→serve without instruction, using only demand lines + HUD.
- **AC-7** — charge/spill/hit visual feedback is code/2D, zero new Blender meshes.
- **AC-8** — no per-frame allocation in `CoreGame.Step` / `SimHarness` loop beyond pooled objects (steady-heap probe; regression = blocker).

Plus: BUILDER self-review vs the five criteria, then ≤2 RATER reviews (B2). Until the human corrects the 3-pass contradiction, the ceiling status is **"gate-ready, policy-pending"** — never "gate passed."
