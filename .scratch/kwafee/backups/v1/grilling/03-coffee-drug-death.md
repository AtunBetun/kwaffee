# Artifact 3 — Coffee drug & death

- Beads id: `kwaffee-bgd` (open, blocked-by 02)
- Grilled scope: chug boost/overdose tension, jitter→collapse→respawn state machine, death consequences (carry/tips/respawn placement), forced-chug fairness, death-specific Vinny roasts, Barista rig visual states, and the pre-human gate evidence for "chugging is the right call sometimes; overdose laughs, never rages".

Current state (verified in repo): `VerbRules.OverdoseSeconds = 2.5`, `ChugDrinkRate = 0.35`, `ChugDuration = 3.5`, one full cup (1.0 liquid) CAN OD (2.57s to drain). `Barista.Overdose()` = `StunFor(3f)` → frozen 3s at death spot → teleport to fixed spawn; `StunFor` already drops the held cup (no spill). Jitter today = `sin(chugSeconds*18+Id)*0.06` on velocity only. Roasts = 2 generic lines in `CoreGame` ("Drinkin' coffee like it owes ya money.", "The jitters got a BODY COUNT, kehd."). No visual state on the barista body.

## Design tree (decisions resolved)

1. **What can kill you?**
   Options: (a) OD only; (b) OD + fling hits kill; (c) OD + everything.
   **DECIDED:** (a) OD only. No HP system, no alternate deaths. Fling stays 0.65s stun comedy. Alternate deaths = new art, roasts, audio, and rage vectors for zero party value.

2. **Chug tension curve — how is "the downside" staged?**
   Options: (a) current binary (safe until 2.5s, then dead); (b) 3-zone: Chill / Danger / OD with a visible warning; (c) timing mini-game (release in a moving window for best boost).
   **DECIDED:** (b) 3-zone, numeric: **Chill 0.0–1.6s** (boost ramps 1.0→~1.30, mild sway), **Danger 1.6–2.5s** (X-eye flicker + body tremble + one Vinny warning per bout; boost 1.30→1.46), **OD ≥2.5s**. *Chill taps are never punishable* — that is the "right call sometimes" mechanic: short sips are the pro move, greed is the gamble. (a) rejected: absence of staged downside tension is the gate's named blocker. (c) rejected: a timing minigame demands per-frame attention that a 4-player chaos party can't afford.

3. **Keep `OverdoseSeconds = 2.5` (one full cup can OD)?**
   **DECIDED:** Keep. 2.5s was deliberately lowered from 3s to make single-cup OD reachable; changing it again breaks the 6 green `VerbRules` tests and re-opens a settled review finding. The skill is releasing before 2.5s, taught by the Danger-zone visuals.

4. **Jitter→collapse→respawn state machine (precise table).**
   Options: (a) 3s frozen in place then teleport (current code); (b) staged: collapse 0.6s → X-eye corpse freeze 2.4s → respawn at 3.0s with squash-drop; (c) sink-and-spin transition + longer cycle.
   **DECIDED:** (b). Total inoperable **exactly 3.0s** measured OD-trigger → controls live (spec's "3-second respawn"). Table:

   | t | state | behavior |
   |---|---|---|
   | 0.0 | OD trigger | cup drops at collapse point and **fully spills** (existing `Spill(remaining)`); controls dead; one Vinny roast fires; tiny global shake (0.02 amp, 0.2s); `ragdoll flop` scripted animation 0.6s |
   | 0.6–3.0 | Corpse | X-eye variant + limp pose, frozen in place; physics zeroed (current stun semantics); corpse body can still be beaned by cups (no stun-extension — `StunFor` takes max, so no lock) |
   | 3.0 | Respawn | teleport to own fixed color spawn spot + `squash-drop` entrance beat; **1.5s stun-immune grace window** |
   | 3.0+ | Normal | controls live, boost decayed to zero, `chugSeconds = 0` |

   Runtime cost vs current code: `StunFor(3f)` + teleport stays; additions are the flop animation window, the grace timer, and state exposure. (c) rejected: longer cycles raise downtime above the spec's 3s and add netcode-sync surface.

5. **Death while carrying — cup loss rules.**
   Options: (a) drop clean with coffee intact (current `Drop()`); (b) drop AND full-spill at collapse point; (c) cup launched with physics spin.
   **DECIDED:** (b). The "died holding the coffee" physical joke; the cup stays in the world (rejoins poolable lifecycle) but its coffee is gone — a serve-ready cup becomes a floor cup. Cheap real cost, zero grief (it is your own cup, and the pool has 16).

6. **Tip (score) penalty on death?**
   Options: (a) flat tip loss; (b) reduced tips; (c) none.
   **DECIDED:** (c) none — zero mutation to `TipLedger`, culprit score, or shared quota, ever. Death costs 3s + the spilled cup, period. A flat penalty fails "death is comedy, cheap, never the point"; more importantly, any shared-resource penalty turns self-OD into a grief tool (a rager could chug-spam to sink the shared quota). This is the single anti-rage decision.

7. **Respawn placement — funny spots vs safe.**
   Options: (a) fixed player-colored spawn spots (current); (b) funny random spots (countertop, customer slot, machine front, booth); (c) chaos-indexed spots.
   **DECIDED:** (a) fixed spawn spots; comedy lives on the **entrance** (squash-drop + Vinny welcome-back variant), not the placement. Random/funny spots read as teleport glitches, can respawn you into an instant re-bean, and make the 3s cheap-death contract feel unfair. Spawn-camp is closed by the 1.5s grace (decision 4).

8. **Overdose fairness — can a friend force your OD?**
   Options: (a) yes — add lacing/heating verbs or passives; (b) no — chug is self-input-gated.
   **DECIDED:** (b) NO. `BeginChug` requires your own SPACE press + a held cup; no verb or event raises another player's `chugSeconds`. Baiting stays legal (hand/gift a full cup, fling one at a friend) — they must still hold SPACE themselves. Rationale: a friend-forced OD is the one path where death stops being "your own fault", and that is precisely the rage the gate forbids.

9. **Vinny roast event table scope for deaths.**
   Options: (a) build the full stat-driven narrator here; (b) standalone deterministic `DeathRoasts` module keyed by death context; (c) keep the 2 generic lines.
   **DECIDED:** (b). Full Vinny narrator = artifact 6; this artifact must ship *funny death roasts* (gate blocker "roasts not funny" otherwise). One roast per OD, at trigger, single-slot stomp semantics (two ODs same frame → latest wins — accepted). Buckets, priority order: `repeat_od` (≥2 deaths same player this shift) > `od_with_cup` > `od_near_customer` > `od_at_machine` > `first_od`; plus a `danger_warning` line once per bout at 1.6s. ≥2 Boston lines per bucket, cycling by death index, zero compliments (enforced by a test scan). Interface: `DeathRoasts.Pick(int deathIndex, DeathContext ctx, long seed)` — pure, deterministic, TDD-able. Note: warning line example "Easy, kehd. That's Russian roulette ya sippin'."; OD examples: "The jitters got a BODY COUNT, kehd." (kept), "AGAIN? Third one's free, kehd.", "Had coffee in ya hand. Left it on the floor for the cat."

10. **Ragdoll collapse — physics swap vs scripted flop.**
    Options: (a) unfreeze rotation and drive a real physics ragdoll; (b) authored flop animation + tilt tween, deterministic.
    **DECIDED:** (b) scripted flop. `Barista.freezeRotation = true` is load-bearing (capsule collider stability, netcode determinism); a physics-joint corpse is a netcode/replay risk with zero party win at 4-player scale. The How-To-Fish squash language (squish-first) IS the ragdoll joke — reads as ragdoll without touching physics. Upgrade path: revisit post-netcode (artifact 10). *Proof that collapse was never the gate: player laughs at the wobble, not the kinematics.*

11. **How does the clay body show state?**
    Options: (a) per-player full body swap; (b) one barista model, runtime-swapped eye variant + rig animations + code transforms.
    **DECIDED:** (b). New public `Barista.VisualState` enum (Normal/Chugging/Danger/Collapsed/Corpse — Respawn reuses Normal + spawn-grace flag) + `Chug01` (exists); the render layer swaps the X-eye submesh material and plays the named rig animation per state; tremble/collapse tilt are code-driven transforms keyed on the same sim clock (deterministic, zero physics mutation).

12. **Blender rig animation export — proven or not?**
    **DECIDED (with fallback gate):** target = 5 bpy-authored animation sets exported in the .glb (names in Blender art needs). IF the exporter cannot carry keyed pose animations (unproven at this commit — prior artifacts shipped static meshes + materials only), fallback: X-eye submesh/material swap (proven pattern) + code-driven tremble/squash/tilt transforms on the existing rig. Either way every visible asset stays bpy-authored in-repo; no external assets, no new textures. Artifact does NOT block on animation export: fallback closes the gap with pure code.

**Ships:** staged chug tension (Chill/Danger/OD), Danger-zone visuals + warning, OD collapse→corpse→respawn at exactly 3.0s, cup drop+spill on death, zero score/quota penalty, fixed-spot spawn + 1.5s grace, no forced OD by construction, `DeathRoasts` module, `Barista.VisualState`, X-eye variant + 5 rig animation sets (or code-transform fallback), sim ChugPolicy + metrics, EditMode suites.

**Explicitly OUT (friendslop cuts):** HP/damage system or any second death cause; death camera/death screen/respawn timer UI; tip/culprit/quota penalty of any kind; physics-joint ragdoll swap; random/funny respawn placement; new grief verb for forcing OD; per-player cameras; death replay (artifact 8 — this artifact only records `DeathContext`); puddle/filth visuals (artifact 4); OD audio sting (artifact 9); full Vinny narrator (artifact 6 — DeathRoasts merges there).

## Refined acceptance criteria (additions to the artifact 3 gate)

Pre-human evidence (`/sim` harness, in-editor run authorized 2026-09-10; SimHarness already tallies chug/overdose — gains a scripted ChugPolicy block + per-OD asserts):

- **A1 Chug is used:** mean chug bouts per player per shift across scripted 4-bot sim runs: `1.5 ≤ μ ≤ 4.0` (used, never spam).
- **A2 Danger is real, not a trap:** OD rate = overdoses/chug bouts across runs: `8% ≤ rate ≤ 30%`.
- **A3 The right call exists:** ≥40% of sim chug bouts end before Danger (<1.6s) under a mixed short-sip/greedy-hold policy; median bout 0.9–1.8s (taps dominate).
- **A4 Cheapness bound:** every OD: downtime OD-trigger → controls-live = `3.0s ± 0.05s`; zero delta asserted on TipLedger balance, culprit score, quota counter for that player and team.
- **A5 Cup consequence:** every OD-while-holding: cup leaves hand at collapse point, `Liquid = 0`, cup re-grabbable within 1s of physics settle.
- **A6 No forced OD (property):** fuzz interleaving of FLING/STEAL/SABOTAGE/FIX against a non-chugging bot yields `chugSeconds == 0` throughout (no cross-player chug accumulation exists).
- **A7 Replay hook:** each OD records a deterministic `DeathContext` per player + timestamp, readable by artifact 8's replay registrar (a "died holding a full cup in front of a customer" fail is a CATASTROPHE REPLAY candidate).

Runtime/tuning:

- **B1** `OverdoseSeconds = 2.5`, `ChugDrinkRate = 0.35` unchanged (one full cup can OD).
- **B2** Danger-zone entry 1.6s: X-eye flicker + tremble + one Vinny warning per bout; OD at 2.5s.
- **B3** Respawn: 3.0s total inoperable; 1.5s stun-immune grace post-respawn.
- **B4** Visual state machine: exactly the 5 states of the decision-4 table; transitions timing asserted by A4.
- **B5** Roasts: `DeathRoasts.Pick` deterministic (same seed/context/index → same line); every bucket ≥2 lines; zero-compliment wordlist scan test; one roast per OD.
- **B6** Blender/ART RULE: all new visible assets **authored via the Blender bpy pipeline** (headless `blender -b -P`, manifest-validated: required-name list, ≤35,000-triangle cap, origins grounded y=0, per-submesh material indices); imported via "Import Authored Art". Named assets: `barista_eyes_x`, animation sets in Blender art needs.

EditMode tests (repo pattern; host Play Mode + EditMode jobs authorized):

- **C1** New suites: `DeathStateRules` (transition timing, cup-spill-once, no-tip-mutation, spawn grace), `DeathRoasts` (determinism, buckets non-empty, no-compliment), chug zone boundaries (1.6s/2.5s). Existing 6 verb tests stay green (no rule-value changes).

Human gate note: "overdose makes friends laugh, never rage" is a human claim — `/sim` proves *cheapness and no-grief structure* (A2/A4/A6); laughter/rage-freedom is evidenced at the human playtest gates (artifact 8 card, 11, 13). Loud assumption, recorded.

## Blender art needs (all via bpy pipeline, this artifact's required-name additions)

1. `barista_eyes_x` — X-eye variant puck, glossy candy plastic (palette rule 3), runtime-swappable via submesh/material index on the shared Barista model. Traces to parent rule 2 (eyes carry the soul — squash, cross, bulge).
2. Animation sets on the Barista rig (exported in the .glb; fallback to code transforms per decision 12):
   - `barista_jitter_tremble` — Danger-zone full-body tremble (parent: spring-and-bobble rule 6, amplified).
   - `barista_dizzy_bobble` — head spring-over dizzy loop at collapse moment (rules 1 + 6).
   - `barista_od_collapse` — squash-first flop (rule 7, How-To-Fish squish language).
   - `barista_od_corpse` — limp X-eyed hold pose (rules 1 + 2).
   - `barista_od_respawn` — squash-land entrance beat (rule 7).
3. Manifest updates: add the 5 animation names + X-eye submesh to the required-name list. Triangle budget: current 17,112; X-eye puck + pose bones must keep total ≤35,000. Grounding y=0 per non-floor meshes. 2D HUD (chug bar) stays code/SVG — no new textures.

## Dependencies + risks

- **Inputs:** Artifact 1 (CoffeeCup `Spill/Drop/Liquid`, Rack/Pool lifecycle); Artifact 2 (VerbRules chug rules landed + tested; Barista carry/stun; SimHarness tallies). Beads `kwaffee-bgd` blocked-by 02 — matches.
- **Runtime contract changes this lands:** `Barista.VisualState` enum + field; `CoreGame.TriggerOverdose` captures `DeathContext`; SimHarness ChugPolicy block + per-OD asserts + new report lines.
- **Shared-file coordination:** Barista.cs / CoreGame.cs / SimHarness.cs are busy files; implementer must claim via Beads and coordinate before editing (ADR-0005, one owner per file at a time).
- **Blender rig animation export unproven** at this commit — decision 12 fallback covers it; no gate-block if static-mesh path is all the exporter does.
- **Unity license + WebGL module still pending** (human prereq): desktop/editor Play Mode is the test surface; WebGL perf proof deferred to artifact 10. Never retry batchmode.
- **Audio:** OD sting / death rattle deferred to artifact 9; artifact 3 leaves no silent hole in-editor (roast text banner + visuals cover) — flagged, owned by 9.
- **Spill puddle / filth visual** deferred to artifact 4 (machines/sabotage); artifact 3 uses the existing spill system only.
- **Roast slot is single-stomp** — two same-frame ODs collapse to the latest line; accepted for scope, noted for artifact 6's narrator queue.
- **Anticipated RATER objections, pre-answered:** "ragdoll isn't physics" → reads as ragdoll via squash language, determinism win (decision 10); "respawn spots boring" → comedy on entrance, never rage (decision 7); "death has no punishment" → that is the point: no shared-resource penalty is the anti-grief structure (decision 6).