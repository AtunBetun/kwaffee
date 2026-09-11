# Artifact 3 — Coffee drug & death — SPEC

> Beads id: `kwaffee-bgd` (open, blocked-by 02). Source of resolved decisions: `.scratch/kwafee/grilling/03-coffee-drug-death.md`.

## Problem Statement

From the player's perspective: holding SPACE to chug coffee is the game's "right call sometimes" mechanic — it should feel worth the risk, teachable, and genuinely funny when it backfires. Today it is a binary trap: safe until 2.5s, then a silent 3s freeze and a teleport, with two generic roast lines and no visible state on the clay body. There is no staged downside tension, no readable danger cue, no death consequence that reads as comedy, and no death roast that lands. The block: chugging never clearly earns its place, and an overdose reads as a cheap unfair punishment (rage) rather than a self-inflicted joke (laughs). Death must be cheap, deterministic, self-inflicted by construction, and funny — never a grief tool, never something another player can force on you.

## Solution

From the player's perspective: chugging is a 3-zone gamble you can read. Chill (0.0–1.6s) is always safe and always rewarded — short sips are the pro move. Danger (1.6–2.5s) is visible greed: X-eyes flicker, the body trembles, Big Vinny warns you once. Past 2.5s is OD — a comedic, scripted collapse: the cup drops and fully spills, one funny Vinny roast fires, a tiny screen shake, a squash-first flop into an X-eyed limp corpse frozen in place (still beanable, never extending the stun), then at exactly 3.0s a squash-drop respawn onto your own fixed spawn spot with a 1.5s stun-immune grace window. Death costs you exactly 3 seconds and your spilled cup. It never touches your tip ledger, your culprit score, or the shared quota — so an overdose is always your own fault, and nobody can weaponize it against the team. No other player can push your chug timer; baiting is legal but only you can hold SPACE. The clay body shows every state on its face and rig, and every new visible asset is authored in Blender via the bpy pipeline.

## User Stories

1. As a player, I want to hold SPACE to chug and feel a safe window where a boost is free, so that short sips are clearly the correct call and I never feel punished for a quick boost.
2. As a player, I want a visible Danger zone with X-eye flicker and body tremble before I can OD, so that the greed threshold is readable and I am taught (not trapped) to release early.
3. As a player, I want one Vinny warning per Danger bout, so that I get a fair, funny second chance before the downside.
4. As a player, I want my boost to ramp smoothly through Chill and Danger, so that the reward of holding longer is real and the gamble has stakes.
5. As a player, when I OD, I want a scripted, comedic collapse — a squash-first flop into an X-eyed limp corpse — rather than a glitchy teleport, so that my death reads as a joke, not a bug.
6. As a player, when I die holding a cup, I want that cup to drop at the collapse point and fully spill, so that the "died holding the coffee" physical joke lands and there is a cheap, visible consequence.
7. As a player, I want exactly one funny Vinny death roast at my OD, keyed to what actually happened, so that death is entertainment, not silence.
8. As a player, I want to respawn at my own fixed colored spot after exactly 3.0s, so that the death cost is predictable and cheap.
9. As a player, I want a 1.5s stun-immune grace window after respawn, so that I am not instantly re-beaned and the cheap-death contract stays fair.
10. As a player, I want my death to never change my tips, my culprit score, or the shared quota, so that an OD is never a grief tool and never feels like a real loss.
11. As a player, I want the knowledge that no other player can force my OD, so that dying is always my own fault and never someone else's rage vector.
12. As a player, I want to see the state of the game on the clay bodies around me (chugging, danger, collapsed, corpse), so that the party reads the moment at a glance.
13. As a player, I want the game to never ragdoll into a physics mess on death, so that the flop stays deterministic, replay-safe, and legible at 4-player scale.
14. As a player, I want the boost to fully decay to zero on respawn, so that death genuinely resets the gamble.
15. As a player, I want the OD and the Danger behavior to be deterministic under a fixed seed, so that replays and tests agree.

## Implementation Decisions

### Staged chug tension (Chill / Danger / OD)
- Keep `VerbRules.OverdoseSeconds = 2.5` and `ChugDrinkRate = 0.35` **unchanged** (one full cup can OD; changing re-opens a settled review and breaks 6 green verb tests).
- Three numeric zones over `chugSeconds`: **Chill 0.0–1.6s** (boost ramps 1.0 → ~1.30, mild sway), **Danger 1.6–2.5s** (X-eye flicker + full-body tremble + one Vinny warning per bout; boost 1.30 → 1.46), **OD ≥2.5s**. Chill taps are never punishable.
- Tension staging is implemented in the existing chug rules path; only new zone boundaries, boost curve, and visual/warning hooks are added. No timing-minigame (rejected: per-frame attention a 4-player party can't afford); no binary no-staging (rejected: that is the gate blocker).

### Death state machine (jitter → collapse → corpse → respawn)
- Total inoperable = **exactly 3.0s** from OD trigger to controls live. Staged table:
  - t=0.0 OD trigger: cup drops at collapse point and **fully spills** (existing `Spill(remaining)`); controls dead; one Vinny roast fires; tiny global shake (0.02 amp, 0.2s); scripted flop animation 0.6s.
  - t=0.6–3.0 Corpse: X-eye variant + limp pose, frozen; physics zeroed (existing stun semantics); corpse body still beanable with **no stun extension** (`StunFor` takes max).
  - t=3.0 Respawn: teleport to own fixed color spawn spot + squash-drop entrance beat; **1.5s stun-immune grace**.
  - t=3.0+ Normal: controls live, boost decayed to zero, `chugSeconds = 0`.
- Runtime: keep `StunFor(3f)` + teleport; additions are the flop window, grace timer, and state exposure. No sink-and-spin, no longer cycles (rejected: downtime > 3s and more netcode-sync surface).

### Cup loss on death
- If dying while carrying: the cup **drops AND fully spills** at the collapse point (not clean `Drop()`). Cup stays in the world and rejoins the poolable lifecycle; a serve-ready cup becomes a floor cup. Zero grief (own cup, pool has 16).

### No penalty — the anti-grief decision
- Death mutates **nothing** in `TipLedger`, culprit score, or shared quota. Cost = 3s + spilled cup only. Any shared-resource penalty would let a rager chug-spam to sink the shared quota; this is the single anti-rage decision.

### Respawn placement
- Fixed player-colored spawn spots (current behavior). Comedy lives on the **entrance** (squash-drop + Vinny welcome-back variant), not placement. No random/funny spots (rejected: reads as teleport glitch, can re-bean instantly, feels unfair). Spawn-camp closed by the 1.5s grace.

### No forced OD (fairness by construction)
- `BeginChug` requires your own SPACE press + a held cup. **No verb or event raises another player's `chugSeconds`.** Baiting stays legal (hand/gift a full cup, fling one at a friend) — they must still hold SPACE. A friend-forced OD is the one path where death stops being "your own fault" — that is the rage the gate forbids.

### `DeathRoasts` deterministic module
- Standalone deterministic module keyed by death context; **not** the full Vinny narrator (that is artifact 6). One roast per OD at trigger, single-slot stomp semantics (two ODs same frame → latest wins).
- Buckets, priority order: `repeat_od` (≥2 deaths same player this shift) > `od_with_cup` > `od_near_customer` > `od_at_machine` > `first_od`; plus a `danger_warning` line once per bout at 1.6s.
- ≥2 Boston lines per bucket, cycling by death index, **zero compliments** (enforced by a test scan).
- Interface: `DeathRoasts.Pick(int deathIndex, DeathContext ctx, long seed)` — pure, deterministic, TDD-able.

### Scripted flop, not physics ragdoll
- Collapse is an authored flop animation + tilt tween, deterministic. `Barista.freezeRotation = true` stays load-bearing (capsule stability, netcode determinism); a physics-joint corpse is a netcode/replay risk with zero party win at 4-player scale. The How-To-Fish squash-first language **is** the ragdoll joke — reads as ragdoll without touching physics. Upgrade path: revisit post-netcode (artifact 10). Laughter comes from the wobble, not kinematics.

### `Barista.VisualState` + render layer
- New public enum `Barista.VisualState`: **Normal / Chugging / Danger / Collapsed / Corpse** (Respawn reuses Normal + spawn-grace flag). `Chug01` anim exists.
- Render layer swaps the X-eye submesh material and plays the named rig animation per state; tremble / collapse tilt are **code-driven transforms keyed on the same sim clock** (deterministic, zero physics mutation).

### Blender rig animation export (with fallback gate)
- Target: **5 bpy-authored animation sets** exported in the .glb (`barista_jitter_tremble`, `barista_dizzy_bobble`, `barista_od_collapse`, `barista_od_corpse`, `barista_od_respawn`).
- **Fallback if the exporter cannot carry keyed pose animations** (unproven at this commit — prior artifacts shipped static meshes + materials only): X-eye submesh/material swap (proven) + code-driven tremble/squash/tilt transforms on the existing rig.
- Either way every visible asset stays bpy-authored in-repo; no external assets, no new textures. Artifact does **not** block on animation export.

### Sim harness (ChugPolicy + metrics)
- SimHarness gains a scripted ChugPolicy block and per-OD asserts, producing new report lines. No /sim numbers recorded yet — this artifact records the first.

### Runtime contract changes
- `Barista.VisualState` enum + field; `CoreGame.TriggerOverdose` captures `DeathContext`; SimHarness ChugPolicy block + per-OD asserts + new report lines.

### Shared-file coordination
- `Barista.cs`, `CoreGame.cs`, `SimHarness.cs` are busy files. Implementer must claim via Beads and coordinate before editing (ADR-0005: one owner per file at a time).

### External dependency
- None. No pre-existing `shift-seam`/`kwaffee-6id` refactor is referenced by this artifact's memo; if one appears later, record it here rather than absorbing its work.

## Testing Decisions

A good test proves the artifact gate, not plumbing. Focus areas and prior art (repo already has 21 green EditMode tests, 6 `VerbRules` verb tests that must stay green — no rule-value changes).

- **`DeathStateRules`** — transition timing (0.0 collapse / 0.6 corpse / 3.0 respawn), cup-spill-once on death, no-tip-ledger / no-culprit / no-quota mutation, spawn-grace window, boost decay to zero, `chugSeconds` reset.
- **`DeathRoasts`** — `Pick` determinism (same seed/context/index → same line), every bucket ≥2 lines, one roast per OD, zero-compliment wordlist scan test.
- **Chug zone boundaries** — 1.6s / 2.5s transitions, boost curve at Chill/Danger/OD, Chill taps never punishable.
- **Sim metrics** — scripted 4-bot runs assert the numeric gate (A1–A7): chug rate, OD rate, short-sip dominance, cheapness bound, cup consequence, no forced OD property, replay hook.
- Existing 6 `VerbRules` tests stay green (no rule-value changes).
- Blender assets manifest-validated: required-name list (X-eye submesh + 5 animation names), ≤35,000-triangle cap (current 17,112), origins grounded y=0, per-submesh material indices.

## Out of Scope

- Any HP/damage system or any second death cause (fling stays 0.65s stun comedy).
- Death camera / death screen / respawn timer UI.
- Tip / culprit / quota penalty of any kind.
- Physics-joint ragdoll swap (scripted flop only).
- Random/funny respawn placement.
- New grief verb for forcing another player's OD.
- Per-player cameras.
- Death replay (artifact 8 — this artifact only records `DeathContext`).
- Puddle/filth spill visuals (artifact 4 — uses the existing spill system only).
- OD audio sting / death rattle (artifact 9 — OD sting deferred; roast text banner + visuals cover in-editor, no silent hole).
- Full Vinny narrator (artifact 6 — `DeathRoasts` merges there).

## Further Notes

- **Roast slot is single-stomp** — two same-frame ODs collapse to the latest line; accepted, noted for artifact 6's narrator queue.
- **Human gate note:** "overdose makes friends laugh, never rage" is a human claim — `/sim` proves cheapness and no-grief structure (A2/A4/A6); laughter/rage-freedom is evidenced at the human playtest gates (artifacts 8, 11, 13). Loud assumption, recorded.
- **Audio gap owner:** OD sting / death rattle deferred to artifact 9; no silent hole in-editor (roast banner + visuals).
- **Blender rig animation export is unproven** at this commit; decision 12 fallback closes the gap with pure code — no gate-block if the exporter only does static meshes + materials.
- **Unity license + WebGL module still human-pending:** desktop/editor Play Mode is the test surface; WebGL perf proof deferred to artifact 10. Never retry batchmode.
- **Anticipated RATER objections, pre-answered:** "ragdoll isn't physics" → reads as ragdoll via squash language, determinism win; "respawn spots boring" → comedy on entrance, never rage; "death has no punishment" → that is the point: no shared-resource penalty is the anti-grief structure.

## Refined Gate

Artifact 3 passes when all of the following hold with evidence (from the memo's refined acceptance criteria; pre-human `/sim` evidence, in-editor run authorized 2026-09-10):

- **A1 Chug is used:** mean chug bouts per player per shift across scripted 4-bot sim runs: `1.5 ≤ μ ≤ 4.0`.
- **A2 Danger is real, not a trap:** OD rate = overdoses/chug bouts across runs: `8% ≤ rate ≤ 30%`.
- **A3 The right call exists:** ≥40% of sim chug bouts end before Danger (<1.6s) under a mixed short-sip/greedy-hold policy; median bout 0.9–1.8s (taps dominate).
- **A4 Cheapness bound:** every OD: downtime OD-trigger → controls-live = `3.0s ± 0.05s`; zero delta asserted on TipLedger balance, culprit score, quota counter for that player and team.
- **A5 Cup consequence:** every OD-while-holding: cup leaves hand at collapse point, `Liquid = 0`, cup re-grabbable within 1s of physics settle.
- **A6 No forced OD (property):** fuzz interleaving of FLING/STEAL/SABOTAGE/FIX against a non-chugging bot yields `chugSeconds == 0` throughout (no cross-player chug accumulation exists).
- **A7 Replay hook:** each OD records a deterministic `DeathContext` per player + timestamp, readable by artifact 8's replay registrar (a "died holding a full cup in front of a customer" fail is a CATASTROPHE REPLAY candidate).
- **B1–B6 runtime/tuning:** `OverdoseSeconds = 2.5`, `ChugDrinkRate = 0.35` unchanged; Danger at 1.6s (X-eye flicker + tremble + one Vinny warning per bout), OD at 2.5s; 3.0s respawn + 1.5s grace; exactly the 5 visual states with A4-asserted timing; `DeathRoasts.Pick` deterministic with ≥2 lines/bucket and zero-compliment scan; all new visible assets bpy-authored and manifest-validated.
- **C1 EditMode suites:** `DeathStateRules`, `DeathRoasts`, chug zone boundaries green; existing 6 verb tests stay green.
- **RATER gate:** ≥2 consecutive passes with ≥8/10 on target dimensions, zero blockers, zero majors; roast lines are actually funny, and chugging is genuinely the right call sometimes.
