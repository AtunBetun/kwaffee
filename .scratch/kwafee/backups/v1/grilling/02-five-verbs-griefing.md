# Grilling memo — Artifact 2: The five verbs + griefing

- **Beads:** `kwaffee-zkm` (blocked by `01`)
- **Grilled scope:** all five verbs FUN alone; AFK-cat coverage; grief-safety rails; /sim gate evidence (≥8 chaos/min, STEAL most-used, no grief style >50%); the A2/A3 boundary on chug consequences.

---

## Design tree (decisions resolved)

### 1. What ships in A2 vs what is explicitly OUT
- **Options:** (a) deep build of all five verbs incl. sabotage forms and death; (b) five verbs at vertical-cut depth + AFK cat + grief rails + evidence, deferring depth to A3/A4; (c) verbs only, cat later.
- **DECIDED:** (b). A2 ships: FLING (exists, verified), CHUG = drink→boost ramp→overdose *trigger* with a wobble-flop (NOT death), FIX = wrench heal on 4 machines, STEAL = cup transfer with traveling owner (exists) + dominance metrics, SABOTAGE = machine-health reduction ONLY (no geyser/steam/bean-sack/trip-wire), AFK-cat station coverage, grief-style taxonomy + counters, /sim metric extensions, grief rails, 8 new bpy animation sets. Explicitly OUT: overdose death/jitter ragdoll/3s respawn/roast table (A3); machine death, stall, overpressure, steam, geysers, bean-sack, trip-wire (A4); full cat personality NFSM (A6); vote-kick/removal (A11). Rationale: each deferral keeps A2's grief bounded so the funny-not-rageful rail holds with zero new punishment systems.

### 2. What "FUN alone" means measurably, pre-human
- **Options:** (a) wait for humans (violates gate order — human ladder is A8's job per spec); (b) pure mechanics evidence (compiles, state changes); (c) two-layer evidence: mechanical viability + /sim behavior + recorded Play Mode demo, with human laughter explicitly deferred to A8.
- **DECIDED:** (c). "FUN alone" = three measurable, non-negotiable layers + one recorded showpiece:
  1. **Viability:** every verb input produces a distinct, named world-state transition within ≤0.5s, asserted in EditMode (state-tag transition tests, not compile-only). No verb may press-and-nothing-happen; the existing roast hook fires on every verb use as the comedy frame (already in CoreGame).
  2. **Behavioral:** /sim 20-seed sweep: every verb exercised ≥1× per bot per shift; grief denial per event ≤1.5s; no permanent-loss invariant (below, branch 8); chaos rate and STEAL dominance pass.
  3. **Showpiece:** one recorded in-editor Play Mode demo per verb (host Play Mode is authorized) — bots playing, HUD counters live, Vinny lines visible. RATER judges the recording + numbers; the *human laugh* gate is A8's binding test, stated loudly so nobody later claims A2's human-fun gap was skipped.
- **DECIDED (anti-gaming):** "fun" is not asserted by self-report or vibes; it is asserted by denial-bounded behavior + chaos rate (branch 5 taxonomy) + one pass of the A8 human ladder later. A2 records its gap honestly in EVIDENCE: *human fun evidence pending A8 by design*.

### 3. Chaos event taxonomy (what counts toward ≥8/min)
- **Options:** (a) keep current `ChaosEvents++` on every RecordShot/serve (trivially ≥8/min — every bot throw counts; meaningless); (b) count only consequential disruptions; (c) hybrid with weights.
- **DECIDED:** (b), precisely. `ChaosEvents` = the count of: cup-hit-stun on another player, any spill, steal, sabotage applied, overdose trigger, expired order, cat knockover, machine stall. Benign fling shots and successful serves are NOT chaos (they are the conveyor). Every chaos event must carry a `src` tag {PlayerCupHit, PlaySpill, Steal, Sabotage, Overdose, ExpiredOrder, CatSpill, MachineStall} for the grief-style guard. This is a contract extension to A1's /sim metric surface — A1's base harness stays, A2 owns the classifier (SimHarness.Build v6+). 8/min over a 180s shift = ≥24 consequential events; with 4 players + cat that is loud but reachable and honest.

### 4. STEAL "most-used verb" — metric definition
- **Options:** (a) raw activation count across all five (FLING wins every shift because every serve requires a throw — trivially rigged, metric lies); (b) exclude FLING as the serve-conveyor, compare the four deliberate verbs; (c) per-player normalisation.
- **DECIDED:** (b). Dominance = median per-shift activation count across the 20-seed sweep satisfies `Steals > max(Chugs, Fixes, Sabotages)` AND `Steals ≥ 6/shift` (a real force, not a rounding win). FLING is excluded from the dominance contest and defended in the memo text: it is the serve conveyor (a requirement, not a choice). Bot steal policy is opportunistic (steal when crossing a cup-holder en route to a task), never scripted-forced, so the metric measures pull, not choreography. A3's drug tension may later push Chugs up; the A2 gate is measured at A2 time and re-verified at A12 balance.

### 5. Grief-style taxonomy and the ≤50% guard
- **Options:** (a) define styles loosely at review time (ungateable); (b) countable styles = {CupHit, Steal, Sabotage} in A2, grown in A4; (c) include cat spill as a style.
- **DECIDED:** (b), with cat excluded. Player grief styles in A2 = **CupHit** (fling at friend), **Steal**, **Sabotage**. `CatSpill` and `MachineStall` are environment chaos, tagged `src=cat|machine`, and EXCLUDED from the guard so the cat cannot mask or inflate player-style distribution. Gate: over 20 seeds, no style's share of player grief events exceeds 50% per seed AND in aggregate. A4 grows the style set (bean-sack, trip-wire, steam scald); the guard re-measures there as a standing invariant, not a one-shot. Design levers if the guard trips: stun duration (.65s), SabotageAmount (.35), BotPolicy mix weights — tune policy/balance, never the counter.

### 6. AFK-cat scope — exact behavior, which verbs it bungles, how
- **Options:** (a) full utility-driven cat NFSM covering a station (A6 territory, 10× cost); (b) minimal seeded "cat covers badly" cycle; (c) cat as an automatable 5th player (full verbs).
- **DECIDED:** (b), absolutely not (c). Trigger: 10s of zero input (move/charge/keys) flags a player AFK; cat dispatch within 1.5s; any input resumes → cat scurries away within 2s. Cat runs a seeded 2.5s action cycle that bungles every verb:
  - **FLING bungle:** cat "flings" = hop + drop, no charge. Serves the active order at a seeded 30% rate; 70% of drops spill.
  - **CHUG bungle:** cat grabs a full cup, stops to glug 2s, wastes 50% liquid, wanders off.
  - **FIX bungle:** pats nearest machine; 50% +0.1 health (a tenth of a real wrench's .5), 50% a cute pat and nothing.
  - **STEAL bungle:** grabs a bot-held cup, carries it to the window, abandons it 4s later — no tip, no credit.
  - **SABOTAGE bungle:** knocks over one counter cup per cycle (spill = chaos event, src=cat). Never touches machines (too mean), never griefs the human.
- **Friendliness rails:** cat only interferes with AFK/bot stations, never the human player; never blocks a machine interaction; never dies (spec). Cat actions count toward chaos rate but NEVER toward any player's verb usage or style share. Big Vinny roasts every cat failure ("The cat servin'? That's why we can't have nice things."). Full cat NFSM (sleep/groom/laser/knock-keys/patrol) stays in A6; A2 ships exactly this one courteous-gremlin cycle.

### 7. Grief-safety rails — can a friend be removed from the shift?
- **Options:** (a) vote-kick/removal in A2; (b) no removal, grief bounded by design + public shame; (c) mechanical grief cooldowns.
- **DECIDED:** (b). NO player removal in A2 — removal is lobby/social surface (A11), and a kick button is product weight we don't need. Instead grief is bounded by design so unplayability is unreachable by one player:
  1. **Denial cap:** any single grief act denies a victim ≤1.5s (stun .65s + walk-back). No death (A3), no destruction.
  2. **Recovery invariant (tested):** stolen cups can always be stolen back (CanSteal is symmetric — victim is the natural counter-thief); spilled cups re-enter the rack pool; tips are transferred, never destroyed: `tips[any player] ≥ 0` always, `Σtips` conserved across steals.
  3. **Quota slack absorbs solo grief:** rack refill + 12s order spawn + 40s patience leaves slack for ~2 lost serves; measured fallback (only if /sim shows solo griefer sinks quota in >20% of shifts) is a cup-deny timer, NOT a punishment system.
  4. **Shame rail (funny, not punitive):** same-player ≥3 grief acts on one target in 60s → Vinny public callout line + culprit counter (already public in HUD). No mechanical penalty — shaming is the rail.
  - Sabotage's 3-hits-kills-machine (.35 × 3) is defused by A2 scope: health ≤0 in A2 causes a shudder + slower rack refill (degradation), NOT death/stall — that consequence lands in A4. This is the single most important anti-rage cut in the ticket.

### 8. Never-lose invariant (grief cannot make the game unplayable)
- **DECIDED:** global test-covered invariant, EditMode: from any state, 60s of adversarial single-player griefing cannot: reduce any player's tips below their stolen-away total (conserved), destroy a cup permanently (rack pool ≥ 4 cups always), stall all machines, or extend victim denial beyond 1.5s per event. If a grief sequence violates it, that's a blocker, not a balance tweak.

### 9. A2/A3 seam on chug consequences — what stays vs defers
- **Options:** (a) full drug/overdose/death/respawn in A2; (b) trigger + wobble-flop in A2, ragdoll collapse/death/respawn/roasts in A3; (c) chug as pure buff, overdose off.
- **DECIDED:** (b). Stays in A2: hold-SPACE chug, 1.0→1.65 boost ramp over 3.5s, liquid consumption via `VerbRules.ChugConsume`, jitter sway already in Barista (Sin-based), overdose TRIGGER detection (`IsOverdose` 2.5s), wobble-flop = drop cup + 1.5s unsteerable sway + `Overdoses++`/`ChaosEvents++` + Vinny line. Defers to A3: X-eyes, violent-shake ragdoll, 3s respawn, `dizzy_flop` ragdoll replacement, "chugging genuinely the right call" tension tuning, overdose roast table. A2's fatal line is *never* crossed: the wobble-flop is the choke point, and A3 swaps its animation + duration, not its rules — `VerbRules.IsOverdose` and the trigger path are the stable seam (A3 must not touch A2's chug rules, only its consequence).

### 10. Cut/keep fights
- **Cut from A2:** bean-sack + trip-wire + steam scalds (A4); machine death/stall consequences (A4); geyser event (A4); jitters/ragdoll/respawn/overdose roasts (A3); cat NFSM beyond the coverage cycle (A6); vote-kick (A11); per-verb tutorial toasts (A11 — A2 keeps first-use Vinny roast line); grief report card screen (A8 awards); chug energy-bar cosmetic (A3); FLING as dominance metric (branch 4).
- **Kept in A2:** all five verbs' core input→state→roast path; AFK-cat cycle; chaos classifier; grief rails + invariants; 8 animation sets; recorded demo. Nothing else. Five great verbs, not twenty okay systems.
- **DECIDED:** A2's own roast lines are required (each verb has ≥1 Vinny line already); new copy must pass Boston-voice review in the self-review pass.

---

## Refined acceptance criteria (A2 gate, artifact language)

1. **Chaos rate:** `/sim` 20-seed sweep (seeds recorded): median chaos events ≥ 8/min using the branch-3 taxonomy; report line prints `chaos/min` + per-`src` breakdown.
2. **STEAL dominance:** medians satisfy `Steals > max(Chugs, Fixes, Sabotages)` and `Steals ≥ 6/shift`; every bot steals ≥1×/shift (opportunistic policy, no forced events).
3. **Grief balance:** no player grief style >50% share per seed or in aggregate; player grief events ≥ 6/shift median (so the distribution is non-vacuous); cat-sourced chaos excluded from the guard.
4. **Fun-alone viability:** each verb has an EditMode state-tag transition test (input → named state change ≤0.5s) and a roast-hook assertion; every verb exercised ≥1× per bot per shift in the sweep.
5. **Grief rails:** denial/event ≤1.5s; recovery invariants hold (tests, branch 8); solo-griefer quota-sink ≤20% of shifts in the sweep; no removal mechanic exists.
6. **AFK-cat:** AFK flag ≤1s after 10s idle; cat assigned ≤1.5s; cat serve success ≤30% seeded; cat exit ≤2s after input; cat chaos tagged `src=cat` and excluded from player verb metrics and style guard.
7. **Art (ART RULE):** the 8 new animation sets (branch below) exist in the bpy pipeline, are manifest-validated (required-name list extended), total triangles ≤35,000 across the artifact's meshes, origins grounded y=0, per-submesh material indices in bounds; import via "Import Authored Art" is clean. Acceptance criteria for this ticket's visible assets say "authored via Blender bpy pipeline" and name the sets.
8. **Non-regression:** all existing EditMode tests (21 as of 2026-09-10) plus new suites (verb transitions, chaos classifier, AFK-cat rules, grief invariants) pass; zero new KWA FEE console errors.
9. **Deliverable evidence:** recorded in-editor Play Mode demo covering all five verbs + AFK-cat bungles + grief shame line; /sim numbers persisted (seeds + report) in EVIDENCE. Human-laugh evidence: explicitly deferred to A8's ladder and stated as such — no fabricated human test.

## Blender art needs (bpy pipeline additions)

All authored in-repo via `blender -b -P`, parameterized, .glb export, manifest-validated (required-name list extended):

- **Barista rig — 6 new animation sets:** `chug_drink` (head-tilt glug loop, googly-eye bulge), `chug_wobble` (pre-flop sway — root of A3 jitters), `fix_wrench` (strike bob with squash + spring-back), `steal_grab` (lunge with reach-stretch, Gamble catch-stretch parent), `sabotage_yank` (knob/pipe yank wind-up), `dizzy_flop` (overdose wobble-flop; A3 swaps in ragdoll collapse at this seam). Reuse existing `windup`/`catch`/`squash-stretch` master language; every land squashes, every grab stretches (rule 7).
- **Shop Cat rig — 2 new sets:** `cat_flipserve` (hop-drop bungle serve), `cat_scurry` (2s exit after input resumes).
- **Shared driver:** googly-eye puck squash shared by `chug_drink`/`steal_grab` (rule 2, candy glossy puck).
- Triangle/material budget: aggregate across authored meshes ≤35,000; origins y=0; per-submesh material indices in bounds; no new materials outside the 3-material palette (matte clay body, candy glossy eyes, felt apron).

## Dependencies + risks

- **Blocked by:** A1 (`kwaffee-uq2`) gate — ticket `kwaffee-zkm` is linked blocked-by 01. Specifically A1's `/sim` metric contract is the base the branch-3 classifier extends; base harness run (chaos/serve/tip numbers) must land first.
- **Prereqs (met):** host Play Mode + in-editor SimHarness execution authorized (DECISIONS 2026-09-10); VerbRules/TipLedger/Machine prefabs exist; seeded RNG in CoreGame; 21 passing EditMode tests.
- **Still blocking later:** Unity Personal license + WebGL module (human) — no WebGL builds; A2 evidence is editor Play Mode + /sim only. Never retry batchmode licensing.
- **Risks:**
  1. **Metric gaming** (bots scripted to force steal counts) — guarded by opportunistic-policy rule + seeds recorded; any forced-event bot code is a blocker in self-review.
  2. **Chaos overcount/undercount** — taxonomy is the contract; A1 harness must adopt it or A2's classifier is the single source. One classifier, one report line.
  3. **A3 boundary creep** — wobble-flop is the choke point; A3 swaps consequence, never A2 rules. If A3 lands jitters-resistant, `VerbRules.IsOverdose` stays.
  4. **Grief anger threshold unknown without humans** — mitigated by bounded denial + recovery invariants + quota slack; measured fallback (cup-deny timer) named, not built.
  5. **Cat scope creep toward A6 NFSM** — A2 ships exactly the coverage cycle; anything else is a cut.
  6. **Triangle budget with 8 new sets** — additive meshes must stay under 35k; validate in manifest before import.
  7. **STEAL dominance vs A3's chug reasonableness** — re-verified at A12 balance, not a blocker at A2 time.