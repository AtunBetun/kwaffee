# Grill memo — Artifact 12: Balance & pacing

- **Beads id:** `kwaffee-8nh` (blocked by 11)
- **Grilled scope:** 20-shift four-bot seeded campaign; every balance/pacing number in the artifact-12 gate; what the sweep tunes and who owns it; how the evidence gates artifact 13. Friendslop lens: cheapest sim that proves the numbers, no gold-plating.

Read before writing: spec §THE GAME, §ARTIFACTS (12), §Simulated-play harness; DECISIONS (host Play Mode authorized, main-branch only, 2-tier loop); EVIDENCE (SimHarness v5-waypoint built+compiles, metric surface exists, no campaign numbers recorded yet); `RoundRules.cs`, `MachineRules.cs`, `CoreGame.cs`, `SimHarness.cs` (current knobs and counters). No verification commands run — memo only.

---

## Design tree (decisions resolved)

### 1. Bot policy — who does what, seeded
- **Question:** The existing harness has one serve-line policy per bot with griefing bolted to fixed timestamps (P2 sabo @20s, P3 sabo @45s, P4 steal @70s). Fixed timestamps are not a policy and cannot hit per-player theft or vendetta guarantees.
- **Options:** (a) keep fixed script, sweep knobs around it; (b) per-bot seeded role wheels with per-shift guarantees; (c) full utility-based bot brains mirroring player decision making.
- **DECIDED: (b) — a role wheel.** Each shift, role assignment per (seed, shiftIndex, botId) via a small pure PRNG chain: `role = Role(seed+shiftIndex*4+botId)` drawn from {Server, Saboteur, Thief, Junkie} with the guarantee that over the 20-shift campaign every bot plays each role ≥4 times and every shift has ≥1 Thief and ≥1 Saboteur. Servers run the existing serve-line waypoints; Saboteur adds the sabotage moments (with a customer-burn script seeding vendettas, see 6); Thief adds a steal window; Junkie adds chug calls. Fixed-time scripted moments are deleted — the policy is the seeded schedule, not the timestamps. Rationale: deterministic, replayable, covers every verb every shift, and role rotation prevents "one bot is always the griefer" artifacts from contaminating per-player stats. (c) is gold-plating: utility brains are artifact-6 NPC work, not balance work.
- **Guarantees per shift (hard constraints in the policy):** every verb exercised (FLING n/a-implicit, ≥4 CHUG calls, ≥1 FIX attempt per broken machine while a bot is near, ≥1 STEAL, ≥1 SABOTAGE); no single grief style >50% of that shift's grief events (carries artifact-2 gate); every bot commits and suffers theft over the campaign.
- **Out of scope:** skill modeling, adaptive difficulty, anti-grief arbitration. Bots dumb-but-faithful; fun tuning is artifact 8/13 territory.

### 2. Metric set + report schema
- **Question:** Current report has chaos/min, serves, tips, shots/hits/misses/catches, chugs/overdoses/fixes/sabotages/steals, machine healths, rack/expired/grab. The gate needs quota success, close calls, downtime, shift length, tip economy, theft rate, NPC latency, cascades, vendettas.
- **Options:** (a) extend sim-report.txt lines arbitrarily; (b) structured one-file-per-shift JSON + aggregate; (c) both, with a computed gate-check section.
- **DECIDED: (c) — `sim-report.json` (schema below) + keep `sim-report.txt` human-readable per shift + `sim-campaign.json` (20 rows + aggregate + `gateChecks` computed in code, not by hand).** Persist seeds, knob set used, and a report hash (stable hash of the aggregate section) as reproducibility evidence. All counters below are new or newly aggregated; none exist yet.
- **Definitions (numeric, operational):**
  - **downtime** (dead-air) = fraction of shift-seconds where `ActiveOrders == 0` AND `RackCups + HeldCups + AirborneCups == 0` AND `BrokenMachines == 0`. No order to serve, no cup to fling/drink, no machine to fix ⇒ nothing productive. Measurable from existing state, monotonic, simple.
  - **quota success** = share of shifts with `Serves >= QuotaTarget`. Target band ≈60%.
  - **close call** = shift where quota MISSED by ≤2 serves (`QuotaTarget - Serves <= 2`) OR quota MET with ≤20s remaining at the moment it was met. Target 30–50% of shifts.
  - **shift length** = measured sim seconds from step 0 to `EndShift` (current `RoundRules.ShiftLength 180 + ShiftEndGrace 3`). Target band 180–240s; sweep does not tune it (see 4) — shifted content (mule interlude cross-artifact) must still land in band.
  - **tip economy** = `tips_total`, mean/player, Gini over 4 per-player tips, and thief-redistribution share = % of tips_total that ended on a thief (owner id vs last-carrier credit, `TipLedger`). Target: Gini ≤ 0.30; redistribution ≥ 10% of total.
  - **theft rate** = `Steals`/shift; plus new per-player counters **`stealsCommittedByPlayer[4]`**, **`stealsSufferedByPlayer[4]`** (not currently tracked — must be added to `TipLedger`/`CoreGame.TrySteal`).
  - **cascade rate** = share of shifts with ≥1 cascade; **cascade** = second machine reaches `Broken` while ≥1 other machine is already `Broken`, OR a machine stays `Broken` ≥30s. State machine on machine-health events, same event stream that will feed artifact-8 replay moments.
  - **vendetta detection** = count of customers whose memory record flips to vendetta (definition in 6), with detection latency and tip effect.
  - **NPC reaction latency** — measured only where incident→reaction is scriptable: `median ≤ 1.0s`, `p95 ≤ 1.5s`, zero unreacted incidents. Depends on artifact 6 emitter; if 6's memory store does not expose the incident API by this artifact's run, record `npc_latency = "pending"` — this is a hard dependency, not a skipped gate (see Dependencies).
  - **chaos/min** ≥8 carried forward from artifact 2.
- **Counters to add (each defended by a gate number):** `stealsCommittedByPlayer`, `stealsSufferedByPlayer`, `quotaMetTime` (shift-relative seconds when quota crossed), `cascadeCount/shift` + `firstCascadeTime`, `vendettaCount`, `vendettaTipRefusals`, `downtimeSeconds`, `npcLatencyMs` array, `fixLatency` (time from break to first FIX — cascade input).
- **Out of scope:** multi-shift persistence, session/streak stats, dashboards, confidence intervals, per-shift heatmaps. Mean + band is enough for a party-game gate; anything more is gold-plating.

### 3. What "downtime" means operationally
- **Question:** naive "no order AND no machine producing" collapses to ~0 forever because healthy machines always produce (they fill the rack), so the metric would never fail — a fake pass.
- **Options:** (a) no-order-only; (b) no-order + no-cups-in-play + no-broken-machine (definition above); (c) include wander/interact time vs a "fun ceiling."
- **DECIDED: (b).** It is the cheapest definition that can actually fail and that matches party intent: dead seconds where no one has a serve, a throw, or a fix. (c) needs a fun model — RATER's job, not a metric. Acknowledge `(b)` reads empty only when the shop has truly nothing on the board; cup-in-flight counts as alive.
- **Target:** campaign-mean downtime < 15%; at most 3 of 20 shifts > 20%.

### 4. Tuning knobs, owners, sweep protocol
- **Question:** which knobs does the sweep drive, which are hands-off, who runs it, and how do we know a change helped?
- **Options:** (a) tune everything including feel constants; (b) small sweep set of pacing knobs, everything else asserted; (c) treat the whole thing as one black-box parameter search.
- **DECIDED: (b), with an injected `KnobSet` — never mutate the shipped constants during the sweep.** Sweep set (5): `OrderSpawnInterval`, `CustomerPatience`, `QuotaTarget`, `MachineRules.PassiveDegradePerSecond`, `MachineRules.UseDegradePerCup`. Hands-off, asserted-in-band: `ShiftLength` (180s — the 3–4 min round is DESIGN, not balance), `MaxActiveOrders` (3 — UI simplicity), `ProductionInterval` (4s), `BrokenThreshold` (0.05), move speed, chug/overdose, fling tip formula, npc patience (artifact 6 owns).
- **Owners:** sweep + campaign = BUILDER (cheap tier), one file `CampaignRunner.cs` + pure `BalanceReport` helpers (TDD like `RoundRules`); no expensive-tier spend until the full campaign row exists, self-review is written, and the RATER call happens (escalation ladder step 4).
- **Sweep protocol (one-at-a-time, in priority order):** quota → spawn interval → patience → degrade knobs. Each candidate runs the **same 20-seed campaign set** (fixed seed set `1000..1019`) so every comparison is on identical demand sequences. First gate: candidate improves/fails-to-regress the target metric on the reduced 5-seed subset; only the winner gets the full 20-seed run. Every tried value AND its aggregate row lands in EVIDENCE (the "numbers asset" rule). Final acceptance = full 20-seed run with the winning `KnobSet` on the canonical seed set; hash recorded.
- **Cost guard:** worst case = 2 passes × ~6 candidates × 20 seeds × 200s ≈ 8 hours sim time in editor; acceptable, and the reduced-subset first gate keeps cheap tier honest. If a seed takes >60s wall per shift on host, batch all 20 shifts in one editor session and write rows as they finish (crash-safe: no all-or-nothing campaign).

### 5. How balance evidence gates artifact 13
- **Question:** what exactly must exist before the RATER call, and what unblocks 13.
- **Options:** (a) numbers only; (b) numbers + repro + self-review; (c) numbers + repro + self-review + a live spot-play of the tuned defaults.
- **DECIDED: (b), and 13 consumes the tuned defaults.** Gate evidence = `sim-campaign.json` with (i) all band targets passing computed `gateChecks`, (ii) reproducibility: re-running the campaign on the same binary + same 20 seeds reproduces the same aggregate hash (verified once, evidence line), (iii) BUILD-SELF-REVIEW appended to EVIDENCE before RATER, (iv) 2 consecutive RATER passes ≥8/10, 0 blockers/majors (standard loop). The winning `KnobSet` becomes the shipped defaults in `RoundRules`/`MachineRules`; artifact 13 plays the game live on those defaults — the sim does not tune 13's sessions.

### 6. Vendetta/cascade detection made measurable
- **Question:** "vendettas noticed" and "cascades at party rate" are vibes; make them counters.
- **Options:** (a) heuristics on memory + machine events (spec's own wording); (b) script the vendetta by fiat (inject the flag, fake it); (c) wait for real human playtest only.
- **DECIDED: (a).** No scripted flags, no fiat. **Cascade** (machine-event state machine, above): fully computable in-core, also feeds artifact-8 replay moments (same event stream). **Vendetta** = heuristic over artifact-6 memory store: per-customer record `{customerId, WHO, incidents[][]}`; incident types = `FLUNG_HIT` (cup hit customer), `EXPIRED_ORDER` (customer walked), `SPILLED_ON`, `SCALDED`, `GEYSERED`; a customer reaches **burn** at ≥2 incidents from the **same player** within ≤3 shifts; burn flips **vendetta** immediately (≤1s), which materializes as refusal-to-tip that player (next serve by that player tips $0 — observable in `TipLedger`) + a vendetta dialogue/line flag for the gossip/tabloid axis. Measurables: `vendettaCount` per shift, detection latency, `vendettaTipRefusals`. Hard dependency: artifact 6 memory store must expose an incident-append + burn-predicate API (see Dependencies).
- **Targets:** ≥2 vendetta detections over the 20-shift campaign; every detected vendetta shows ≥1 `vendettaTipRefusal`; detection latency ≤1s.

### 7. Scope — what ships, what is explicitly OUT
- **IN:** `CampaignRunner.cs` (20-shift, batch, seeded), `BalanceReport` pure helpers + JSON schema, new counters (2), `KnobSet` injection, sweep evidence in EVIDENCE, vendetta/cascade heuristics on memory + machine events, hash-based repro check.
- **OUT (explicitly):** no new gameplay; no difficulty scaling or rubber-banding; no per-bot skill models; no ANOVA/DOE — one-at-a-time sweep is fine; no CI/p-value machinery; no campaign dashboards; no anti-grief design (artifact 2 owns no-single-grief-style); no saving/loading campaigns; no new NPC behavior — vendetta consumption is read-only on artifact 6's store.

---

## Refined acceptance criteria (gate language, all numeric)

Runs on canonical seed set `1000..1019`, `KnobSet` recorded, aggregate hash recorded; each in `sim-campaign.json → gateChecks`, computed by code:

1. **Shift length:** mean between 180s and 240s; zero shifts outside [180, 250]s.
2. **Downtime:** campaign-mean < 15%; at most 3 of 20 shifts > 20%; definition as decided (no order + no cups in play + no broken machine).
3. **Quota success:** between 55% and 70% of shifts (center ≈60%).
4. **Close calls:** between 30% and 50% of shifts (definition as decided: missed by ≤2, or met with ≤20s to spare).
5. **Chaos:** ≥8 chaos events/min campaign-mean (artifact-2 gate carried).
6. **Theft:** ≥90% of shifts have ≥1 steal; in ≥60% of shifts all 4 players are theft-involved (commit or suffer); campaign: every player commits ≥5 and suffers ≥5 steals. No grief style >50% of grief events in any shift.
7. **Cascades:** ≥80% of shifts produce ≥1 cascade; mean cascades/shift in [1, 3]; `firstCascadeTime` ≥ 75s in ≥80% of shifts (no first-minute cascade rage).
8. **Vendettas:** ≥2 vendetta detections over the campaign; all detected vendettas show ≥1 tip refusal; detection latency ≤1s.
9. **NPC latency:** median ≤1.0s, p95 ≤1.5s, zero unreacted incidents (hard-blocked on artifact 6 API; see Dependencies).
10. **Tip economy:** per-shift Gini ≤ 0.30; thief redistribution ≥10% of `tips_total`; all 4 players positive tips in ≥80% of shifts.
11. **Reproducibility:** identical binary + same 20 seeds ⇒ identical aggregate hash (verified once, logged).
12. **Gate protocol:** BUILD-SELF-REVIEW appended before RATER; 2 consecutive RATER passes ≥8/10, 0 blockers, 0 majors; artifact 13 starts only after this gate closes, playing the tuned defaults.

## Blender art needs
**NONE.** Artifact 12 is pure code/data/netcode — bots, counters, sweep, report. State explicitly: zero new or re-authored meshes; no bpy scripts touched; no manifest changes; nothing imported via "Import Authored Art".

## Dependencies + risks

- **Artifact 1** (blocked-by chain: 12 ← 11 ← … ← 1): metric counters in `CoreGame`/`SimHarness` already exist for the core set; new counters (theft by-player, quotaMetTime, cascade, downtime) land on that same surface. Risk: counters are `int`s on `CoreGame` — cascade/downtime need time-series reasoning but stay counter-based (first-cascade timestamp, downtime seconds), no per-step allocation (perf rule).
- **Artifact 4** (machines): cascade state machine consumes machine-health events — `Machine.Configure`/`CoreGame.Step` already push these; reuses event stream for artifact-8 replay moments. Risk: event stream currently goes to Roast only; needs a neutral event bus (`MachineEvent` list per shift) — small, named, no GC.
- **Artifact 2** (verbs/griefing): theft per-player counters and no-single-style>50% are re-asserted here; STEAL-most-used is an artifact-2 gate, not repeated here.
- **Artifact 6 (NPC cast) — the hard dependency.** Vendetta detection + NPC latency need the memory store's incident-append and burn-predicate API. If 6's memory model keys customers differently than `{customerId, WHO, incidents}` by the time this runs, the vendetta counter cannot be evidence. Mitigation: state this exact API in the memo; artifact 12's gate keeps `npc_latency`/`vendettaCount` as required fields — a "pending" row is a blocker, not a skip.
- **Artifact 8** (juice/replay): cascade event stream doubles as replay-moment feed; if 8's moment taxonomy diverges, cascade metric still stands alone (it is defined here, not there).
- **Artifact 10 (netcode):** the authoritative server runs a fixed 30Hz tick (ADR-0001) while the sim harness runs 50Hz (`TimeManager` 0.02). Balance numbers are valid for the sim configuration; knob-swept defaults must be re-validated at 30Hz before artifact 13's online sessions — flagged loudly, owned by 10/13, not silently assumed.
- **Human prereqs:** Windows/macOS host Play Mode + in-editor SimHarness are authorized (DECISIONS); WebGL/license still pending — 12's evidence does not need a build. Campaign runtime ~hours in-editor: keep crash-safe per-shift writes (decided in 4).
- **Main-branch-only (ADR-0005):** ownership = one file at a time; `SimHarness.cs`/`CoreGame.cs` edits must coordinate with artifact siblings via the harness/balance owner; commit small.
- **RATER budget:** max 2 reviews per artifact; do not spend review #1 until the campaign row + self-review are complete.
- **Risk — fake pass:** every target is a band on measured counters, steered by the injected `KnobSet`, never by editing the gate numbers; gateChecks computed in code, not by hand.