# Spec — Artifact 2: The Five Verbs + Griefing

Ticket: `kwaffee-zkm` (blocked by `01`). Authoritative source of resolved decisions: `.scratch/kwafee/grilling/02-five-verbs-griefing.md`.

## Problem Statement (from the user's perspective)

Me and my friends are here to sling coffee AND wreck each other for tips. Right now only FLING exists, so "ruin each other" is a promise the game hasn't kept. I want to chug, fix, steal, and sabotage — and I want the mess to be funny, never rageful. When my buddy steals my tray or the cat knocks over my cups, it should feel like a party story, not an attack. The game has to prove it: every verb must actually do something distinct, the chaos has to be measurable, stealing has to feel like the natural thing to do, and no single grief style may swallow the whole shift. If I go AFK, the Shop Cat has to "help" — badly, adorably, without ruining my game. And no matter how much my friends grief, the shift must stay playable: nobody gets kicked, tips never vanish, cups always come back, and machinery degrades instead of killing the shop.

## Solution (user perspective)

Five verbs, all of them working and all of them funny alone: FLING (chemical-serve conveyor), CHUG (drink → speed boost → wobble-flop, never death), FIX (wrench heal), STEAL (cup transfer that the victim can always steal back), SABOTAGE (machine-health damage only). Real chaos counter that counts disruptions, not busywork, with STEAL measured as the most-used deliberate verb. Grief bounded by design so one jerk friend physically cannot unplay the shift: short stuns, conserved tips, recoverable cups, degraded-not-dead machines, public Vinny shame instead of punishment. The Shop Cat covers AFK stations with a seeded bungle cycle — flings, glugs, pats, grabs, knocks — and always scurries off when I come back. Proof is numbers, not vibes: `/sim` 20-seed sweep plus a recorded in-editor Play Mode demo of every verb, with the human-laugh test explicitly deferred to A8 by design.

## User Stories

1. As a player, I want to hold SPACE and chug so that I ramp from normal speed to 1.65× over 3.5 seconds and my coffee-addict barista sways with the jitters.
2. As a player, I want to chug too much so that I hit the overdose trigger, drop my cup, and wobble-flop for 1.5 seconds while Big Vinny roasts me — so that overdosing is funny and never fatal (A2's fatal line is never crossed).
3. As a player, I want my jitter/overdose wobble to be a real, named state I can see on the HUD (state-tag `Overdose`) so that the game visibly responds within 0.5 seconds of my input.
4. As a player, I want to press L with a wrench and hit a machine so that its health heals by 0.5 per strike with a squash-and-spring-back bob — so that FIX is a verb with feedback, not a button that does nothing.
5. As a player, I want FIX to work on all four machines (Espresso, Grinder, Steam Wand, Ice Machine) so that degraded machines can always be nursed back.
6. As a player, I want to grab a friend's carried cup (E) and carry it to my own order so that I can serve it to MY customer and take the tip — steal-style betrayal that is also productive.
7. As a player, I want a stolen cup to always be stealable back (CanSteal is symmetric) so that the victim's natural counter is to be the counter-thief — grief answers itself without any removal or punishment mechanic.
8. As a player, I want tips to never be destroyed or reduced below my stolen-away total (Σtips conserved across steals) so that stealing shifts money, it never deletes money — the game can't be broken by theft.
9. As a player, I want to sabotage a machine (health reduction only, 0.35 per hit) so that I can hurt the shop's capacity without killing anything.
10. As a player, I want a machine at zero health to shudder and refill cups slower (degradation, not death/stall) so that sabotage reads on the shift without the rage-threshold of a dead machine (death/stall lands in A4).
11. As a player, I want the chaos counter to count only consequential disruptions — stuns, spills, steals, sabotage, overdoses, expired orders, cat spills, machine stalls — so that spamming benign throws at the conveyor doesn't game the ≥8 chaos/min gate.
12. As a player, I want every chaos event tagged with a source (PlayerCupHit, PlaySpill, Steal, Sabotage, Overdose, ExpiredOrder, CatSpill, MachineStall) so that the game can honestly report where chaos comes from, and players can't pad the number.
13. As a player, I want STEAL to be measured as the most-used *deliberate* verb (FLING excluded — it's the serve conveyor, a requirement not a choice) so that the "STEAL most-used" gate measures pull, not choreography.
14. As a player, I want my grief style to never exceed half the shift's player-grief events, with the guard measured per seed and in aggregate over 20 seeds, so that no single grief style (CupHit / Steal / Sabotage) dominates and shifts stay varied.
15. As a player, I want to be able to grief without ever removing a friend from the shift — no vote-kick, no removal — so that the punishment stays in the fiction (Big Tony, Vinny's mouth), not the lobby.
16. As a player, I want any single grief act to deny me at most 1.5 seconds (0.65s stun + walk-back) so that "you stunned me" is a beat, not a hostage situation.
17. As a player, I want spilled cups to re-enter the rack pool (rack always ≥4 cups) and the 12s order spawn + 40s patience + rack refill to absorb ~2 lost serves, so that a solo griefer cannot sink the quota alone.
18. As a player, I want 3+ grief acts by the same player on one target in 60 seconds to trigger a Big Vinny public callout plus a public culprit counter — shame as the rail, with zero mechanical penalty.
19. As a player going AFK, I want the Shop Cat to take over my station within 1.5 seconds of 10 seconds idle, and to scurry off within 2 seconds of any input resuming, so that my absence is covered badly but courteously.
20. As a player, I want the cat to bungle every verb: hop-drop "fling" that serves the active order only 30% of the time (70% of drops spill), a 2s glug that wastes half the cup, wrench pats that heal 50% of the time by only 0.1, cup thefts abandoned at the window with no tip, and counter-cup knocks — so that cat coverage is comically bad and never a fifth competent player.
21. As a player, I want the cat to never touch my machine interactions, never grief the human player (only AFK/bot stations), and never die, so that the beloved gremlin stays beloved.
22. As a player, I want cat chaos to count toward the chaos rate but never toward any player's verb usage or grief-style share, so that the cat can't mask or inflate player metrics.
23. As a player, I want Big Vinny to roast every cat failure ("The cat servin'? That's why we can't have nice things.") so that even a missed serve is a punchline.
24. As a player, I want a recorded in-editor Play Mode demo covering all five verbs, cat bungles, and a shame line, with HUD counters live and Vinny lines visible, so that the fun is observable now even though real human laughter is A8's job by design.
25. As a player, I want each verb to carry at least one Vinny roast line, and any new copy to pass Boston-voice review, so that every verb use lands in the comedy frame.

## Implementation Decisions

### Scope (DECIDED: vertical-cut depth (b), NOT deep build (a), NOT verbs-only (c))
- A2 ships: FLING (exists, verified), CHUG = drink → boost ramp → overdose *trigger* with wobble-flop (NOT death), FIX = wrench heal on all 4 machines, STEAL = cup transfer with traveling owner (exists) + dominance metrics, SABOTAGE = machine-health reduction ONLY, AFK-cat station coverage, grief-style taxonomy + per-`src` counters, `/sim` metric extensions, grief rails, 8 new bpy animation sets.
- Explicitly OUT of A2: overdose death / jitter ragdoll / 3s respawn / roast table (A3); machine death, stall, overpressure, steam, geysers, bean-sack, trip-wire (A4); full cat personality NFSM (A6); vote-kick / removal (A11). Every deferral keeps A2's grief bounded: the funny-not-rageful rail holds with zero new punishment systems.

### Chaos taxonomy (DECIDED: consequential disruptions only (b))
- `ChaosEvents` = count of: cup-hit-stun on another player (`PlayerCupHit`), any spill (`PlaySpill`), steal (`Steal`), sabotage applied (`Sabotage`), overdose trigger (`Overdose`), expired order (`ExpiredOrder`), cat knockover (`CatSpill`), machine stall (`MachineStall`).
- Benign fling shots and successful serves are NOT chaos (they are the conveyor). Every chaos event carries a `src` tag from the 8-tag set for the grief-style guard.
- Contract extension to A1's `/sim` metric surface: A1's base harness stays; A2 owns the classifier (`SimHarness.Build v6+`). One classifier, one report line. A1's harness must adopt the taxonomy or the A2 classifier is the single source of truth.
- 8/min over a 180s shift = ≥24 consequential events; loud but reachable and honest with 4 players + cat.

### STEAL dominance (DECIDED: exclude FLING (b), compare the four deliberate verbs)
- Dominance = median per-shift activation across the 20-seed sweep satisfies `Steals > max(Chugs, Fixes, Sabotages)` AND `Steals ≥ 6/shift`.
- FLING is excluded from the dominance contest and this exclusion is defended in text: FLING is the serve conveyor (a requirement, not a choice). Raw activation counts across all five would make FLING win every shift trivially and the metric would lie.
- Bot steal policy is opportunistic (steal when crossing a cup-holder en route to a task), never scripted-forced, so the metric measures pull, not choreography. Any forced-event bot code is a blocker in self-review.
- A3's drug tension may push Chugs up; the A2 gate is measured at A2 time and re-verified at A12 balance.

### Grief-style taxonomy + guard (DECIDED: countable styles {CupHit, Steal, Sabotage} (b), cat excluded)
- Player grief styles in A2 = **CupHit** (fling at friend), **Steal**, **Sabotage**. `CatSpill` and `MachineStall` are environment chaos, tagged `src=cat|machine`, EXCLUDED from the guard so the cat cannot mask or inflate player-style distribution.
- Gate: over 20 seeds, no style's share of player grief events exceeds 50% per seed AND in aggregate; player grief events ≥ 6/shift median so the distribution is non-vacuous.
- A4 grows the style set (bean-sack, trip-wire, steam scald); the guard re-measures there as a standing invariant, not a one-shot.
- If the guard trips, tune policy/balance levers — stun duration (0.65s), SabotageAmount (0.35), BotPolicy mix weights — NEVER the counter.

### AFK-cat coverage state machine (DECIDED: minimal seeded "cat covers badly" cycle (b), absolutely not automatable 5th player (c))
- **Idle → AFK flag:** 10s of zero input (move/charge/keys) flags a player AFK; flag set within 1s; cat dispatched within 1.5s. **AFK → resume:** any input resumes → cat scurries away within 2s.
- Cat runs a seeded 2.5s action cycle that bungles every verb:
  - **FLING bungle:** hop + drop, no charge; serves the active order at seeded 30% rate; 70% of drops spill.
  - **CHUG bungle:** grabs a full cup, stops to glug 2s, wastes 50% liquid, wanders off.
  - **FIX bungle:** pats nearest machine; 50% +0.1 health (a tenth of a real wrench's 0.5), 50% a cute pat and nothing.
  - **STEAL bungle:** grabs a bot-held cup, carries it to the window, abandons it 4s later — no tip, no credit.
  - **SABOTAGE bungle:** knocks over one counter cup per cycle (spill = chaos event, `src=cat`). Never touches machines (too mean), never griefs the human.
- Friendliness rails: cat only interferes with AFK/bot stations, never the human player; never blocks a machine interaction; never dies.
- Cat actions count toward chaos rate but NEVER toward any player's verb usage or style share.
- Big Vinny roasts every cat failure. Full cat NFSM (sleep/groom/laser/knock-keys/patrol) stays in A6; A2 ships exactly this courteous-gremlin cycle. Anything else is a cut.

### Grief rails (DECIDED: no removal (b); grief bounded by design + public shame)
- **No player removal in A2** — removal is lobby/social surface (A11); a kick button is product weight not needed.
- **Denial cap:** any single grief act denies a victim ≤1.5s (0.65s stun + walk-back). No death (A3), no destruction.
- **Recovery (tested invariants):** stolen cups always stealable back (CanSteal symmetric — victim is the natural counter-thief); spilled cups re-enter the rack pool; tips transferred, never destroyed: `tips[any player] ≥ 0` always, `Σtips` conserved across steals.
- **Quota slack absorbs solo grief:** rack refill + 12s order spawn + 40s patience leaves slack for ~2 lost serves. Measured fallback (ONLY if `/sim` shows a solo griefer sinks quota in >20% of shifts) is a cup-deny timer, NOT a punishment system.
- **Shame rail (funny, not punitive):** same-player ≥3 grief acts on one target in 60s → Vinny public callout line + culprit counter (already public in HUD). No mechanical penalty — shaming is the rail.
- **Sabotage floor (not death):** health ≤0 causes shudder + slower rack refill (degradation), NOT death/stall. The 3-hits-kills-machine math (.35 × 3) never lands as death in A2. This is the single most important anti-rage cut in the ticket; death/stall consequence lands in A4.

### Never-lose invariant
- Global test-covered invariant, EditMode: from any state, 60s of adversarial single-player griefing cannot: reduce any player's tips below their stolen-away total (conserved), destroy a cup permanently (rack pool ≥ 4 cups always), stall all machines, or extend victim denial beyond 1.5s per event. A grief sequence violating it is a blocker, not a balance tweak.

### A2/A3 seam on chug consequences (DECIDED: trigger + wobble-flop in A2 (b))
- Stays in A2: hold-SPACE chug, 1.0 → 1.65 boost ramp over 3.5s, liquid consumption via `VerbRules.ChugConsume`, jitter sway already in Barista (Sin-based), overdose TRIGGER detection (`IsOverdose` 2.5s), wobble-flop = drop cup + 1.5s unsteerable sway + `Overdoses++` / `ChaosEvents++` + Vinny line.
- Defers to A3: X-eyes, violent-shake ragdoll, 3s respawn, `dizzy_flop` ragdoll replacement, "chugging genuinely the right call" tension tuning, overdose roast table.
- `VerbRules.IsOverdose` and the trigger path are the stable seam: A3 swaps animation + duration, never A2's chug rules. If A3 lands jitters-resistant, `VerbRules.IsOverdose` stays.

### Blender animation-set art needs (ART RULE: authored via bpy pipeline)
- All authored in-repo via `blender -b -P`, parameterized, .glb export, manifest-validated (required-name list extended). Reuse existing `windup` / `catch` / `squash-stretch` master language; every land squashes, every grab stretches (LOOK rule 7).
- **Barista rig — 6 new sets:** `chug_drink` (head-tilt glug loop, googly-eye bulge), `chug_wobble` (pre-flop sway — root of A3 jitters), `fix_wrench` (strike bob with squash + spring-back), `steal_grab` (lunge with reach-stretch, Gamble catch-stretch parent), `sabotage_yank` (knob/pipe yank wind-up), `dizzy_flop` (overdose wobble-flop; A3 swaps in ragdoll collapse at this seam).
- **Shop Cat rig — 2 new sets:** `cat_flipserve` (hop-drop bungle serve), `cat_scurry` (2s exit after input resumes).
- **Shared driver:** googly-eye puck squash shared by `chug_drink` / `steal_grab` (LOOK rule 2, candy glossy puck).
- Budget: aggregate triangles across authored meshes ≤35,000; origins grounded y=0; per-submesh material indices in bounds; no new materials outside the 3-material palette (matte clay body, candy glossy eyes, felt apron). Validate in manifest before import; "Import Authored Art" must be clean.

### "FUN alone" evidence layers (DECIDED: two-layer mechanics + recorded demo (c); human laughter deferred)
- Three measurable, non-negotiable layers + one recorded showpiece:
  1. **Viability:** every verb input produces a distinct, named world-state transition within ≤0.5s, asserted in EditMode (state-tag transition tests, not compile-only). No verb may press-and-nothing-happen. The existing roast hook fires on every verb use as the comedy frame (already in CoreGame).
  2. **Behavioral:** `/sim` 20-seed sweep: every verb exercised ≥1× per bot per shift; grief denial per event ≤1.5s; never-lose invariant holds; chaos rate and STEAL dominance pass.
  3. **Showpiece:** one recorded in-editor Play Mode demo per verb (host Play Mode authorized) — bots playing, HUD counters live, Vinny lines visible.
- Anti-gaming: "fun" is NOT asserted by self-report or vibes; it is asserted by denial-bounded behavior + chaos rate + one pass of the A8 human ladder later. A2 records its gap honestly in EVIDENCE: *human fun evidence pending A8 by design* — stated loudly so nobody later claims A2's human-fun gap was skipped.
- A2's own roast lines are required (each verb has ≥1 Vinny line already); new copy must pass Boston-voice review in the self-review pass.

### External dependencies
- **Blocked by A1 (`kwaffee-uq2` gate):** A1's `/sim` metric contract is the base the branch-3 classifier extends; base harness run (chaos/serve/tip numbers) must land first.
- Prereqs met (do not re-verify): host Play Mode + in-editor SimHarness execution authorized (DECISIONS 2026-09-10); VerbRules / TipLedger / Machine prefabs exist; seeded RNG in CoreGame; 21 passing EditMode tests.
- Still blocking later: Unity Personal license + WebGL module (human) — no WebGL builds; A2 evidence is editor Play Mode + `/sim` only. Never retry batchmode licensing.

## Testing Decisions

- **What makes a good test here:** a test that fails if a grief mechanic stops being bounded, if a metric can be gamed, or if a verb stops visibly responding. Tests assert observable state transitions and invariant math — never wiring, field copies, or source text.
- **Verb state-tag transition tests (EditMode):** each verb maps input → named state change asserted ≤0.5s (e.g. hold-SPACE → `Chugging` ramp → `Overdose` trigger → `WobbleFlop` 1.5s → recover), plus a roast-hook assertion per verb (firing the existing hook). No compile-only tests.
- **Chaos classifier tests:** each of the 8 `src` tags is assigned by the right event; benign fling shots / successful serves never increment `ChaosEvents`; the classifier is the single source (A1 harness adoption or A2 classifier dominates).
- **STEAL dominance tests:** median-selection logic over the sweep's per-shift activation counts; exclusion of FLING from the contest; opportunistic-bot policy asserted (no forced steal script — forced-event bot code is a blocker in self-review).
- **Grief-style guard tests:** per-seed and aggregate >50% detection on player-style shares; `src=cat|machine` events excluded from the guard; non-vacuity floor (player grief ≥6/shift median).
- **AFK-cat rules tests:** 10s idle → AFK flag ≤1s; dispatch ≤1.5s; seeded 2.5s cycle produces the 30% serve success and 70%-of-drops-spill rates; input resume → scurry exit ≤2s; cat actions excluded from player verb metrics and style share; the five bungle outputs (fling/chug/fix/steal/sabotage) each assertable.
- **Grief invariants tests (EditMode, prior art: TipLedger + machine prefab state):** 60s adversarial single-player griefing from any state cannot reduce any tip below stolen-away total, destroy a cup (rack pool ≥4 always), stall all machines, or extend denial beyond 1.5s/event. Sabotage floor: 3×0.35 hits → health ≤0 = shudder + slower refill, never death/stall.
- **Non-regression:** all existing EditMode tests (21 as of 2026-09-10) plus the new suites pass; zero new KWA FEE console errors.
- Skip: no new tests for A3/A4/A6/A11-scoped features (they are out of scope, not half-built).

## Out of Scope

- Overdose death, jitter ragdoll, 3s respawn, overdose roast table, chug energy-bar cosmetic (A3).
- Machine death/stall, overpressure, steam scalds, bean geysers, bean-sack, trip-wire, geyser event (A4).
- Full cat personality NFSM (sleep/groom/laser/knock-keys/patrol) (A6).
- Vote-kick / player removal, per-verb tutorial toasts, grief report card screen (A11).
- FLING as dominance metric (it is the serve conveyor, excluded by decision).
- Human laughter testing (A8 ladder — explicitly deferred and recorded as such).
- WebGL builds (blocked on human license + module).

## Further Notes

- **Risks and mitigations (from memo):** metric gaming (opportunistic-policy rule + recorded seeds; forced-event bot code is a blocker); chaos overcount/undercount (taxonomy is the contract — one classifier, one report line); A3 boundary creep (wobble-flop is the choke point — A3 swaps consequence, never A2 rules; `VerbRules.IsOverdose` stays); grief anger threshold unknown without humans (bounded denial + recovery invariants + quota slack; cup-deny timer named, not built); cat scope creep toward A6 NFSM (A2 ships exactly the coverage cycle; anything else is a cut); triangle budget with 8 new sets (validate in manifest before import); STEAL dominance vs A3's chug reasonableness (re-verified at A12 balance, not a blocker at A2 time).
- **Counting rule:** cat chaos counts toward chaos rate, never toward player verb usage or style share. Machine stalls count as chaos (`MachineStall`) but only if a stall consequence exists — in A2 that is only the A1-legacy stall path; the A4 consequences are out of scope.
- **Honest evidence:** the A2 EVIDENCE entry records seeds + `/sim` report + demo recording, and states plainly that human-laugh evidence is A8's by design. No fabricated human test.

## Refined Gate

Measurable acceptance for A2, all must hold at the ticket gate:

1. **Chaos rate:** `/sim` 20-seed sweep (seeds recorded): median chaos events ≥ 8/min under the consequential-disruption taxonomy; report line prints `chaos/min` + per-`src` breakdown.
2. **STEAL dominance:** medians satisfy `Steals > max(Chugs, Fixes, Sabotages)` and `Steals ≥ 6/shift`; every bot steals ≥1×/shift (opportunistic policy, no forced events).
3. **Grief balance:** no player grief style >50% share per seed or in aggregate; player grief events ≥ 6/shift median (non-vacuous); cat-sourced chaos excluded from the guard.
4. **Fun-alone viability:** each verb has an EditMode state-tag transition test (input → named state change ≤0.5s) and a roast-hook assertion; every verb exercised ≥1× per bot per shift in the sweep.
5. **Grief rails:** denial/event ≤1.5s; recovery invariants hold (tests); solo-griefer quota-sink ≤20% of shifts in the sweep; no removal mechanic exists.
6. **AFK-cat:** AFK flag ≤1s after 10s idle; cat assigned ≤1.5s; cat serve success ≤30% seeded; cat exit ≤2s after input; cat chaos tagged `src=cat` and excluded from player verb metrics and style guard.
7. **Art (ART RULE):** the 8 new animation sets (6 Barista: `chug_drink`, `chug_wobble`, `fix_wrench`, `steal_grab`, `sabotage_yank`, `dizzy_flop`; 2 Shop Cat: `cat_flipserve`, `cat_scurry`) exist in the bpy pipeline, manifest-validated (required-name list extended), total triangles ≤35,000 across the artifact's meshes, origins grounded y=0, per-submesh material indices in bounds; "Import Authored Art" is clean.
8. **Non-regression:** all existing EditMode tests (21 as of 2026-09-10) plus new suites pass; zero new KWA FEE console errors.
9. **Deliverable evidence:** recorded in-editor Play Mode demo covering all five verbs + AFK-cat bungles + grief shame line; `/sim` numbers persisted (seeds + report) in EVIDENCE; human-laugh evidence explicitly deferred to A8's ladder and stated as such.