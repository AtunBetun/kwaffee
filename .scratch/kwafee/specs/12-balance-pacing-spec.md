# Artifact 12 Spec — Balance & pacing

## Problem Statement (from the user's perspective)

A party wants to run a 20-shift campaign in the /sim harness and get numbers that prove the balance gate: shifts land 3-4 minutes, downtime stays under 15%, the shared Quota is achievable (about 60% success) but not trivial (30-50% close calls), every player gets stolen from at least once, machine cascades happen at party rate, and vendettas actually get noticed. Today none of this is measurable: the sim's bots run fixed-timestamp scripts (not a policy), the counters for theft-by-player, cascade, downtime, and vendetta do not exist, and no campaign numbers are recorded anywhere. The balance ticket cannot close on vibes — it needs a deterministic, replayable 20-shift seeded campaign whose every gate number is computed by code, not by hand.

## Solution (user perspective)

A cheap-tier `CampaignRunner` runs 20 seeded shifts with four bots and a per-shift role wheel drawn deterministically from (seed, shift, bot). Every verb fires every shift. The harness records new counters (per-player theft, quota-met time, cascade, downtime, vendetta, NPC latency) into one machine-readable `sim-campaign.json` with an aggregate row and code-computed `gateChecks`, plus a human-readable per-shift `sim-report.txt`. A `KnobSet` injects five pacing knobs without mutating shipped constants; the sweep tunes them one at a time (quota → spawn → patience → degrade) against the same canonical 20 seeds. The winning KnobSet becomes the shipped defaults. Vendettas are detected read-only from the artifact-6 memory store (2 incidents, same player, ≤3 shifts → burn → tip refusal). Every number in the gate is a measured band, steered by knobs, never edited by hand.

## User Stories

1. As a BUILDER, I want the campaign to run 20 shifts on canonical seeds `1000..1019` with the used `KnobSet` and a stable aggregate hash persisted, so that every sweep comparison and the final acceptance run are reproducible.
2. As a BUILDER, I want each shift's role wheel (Server/Saboteur/Thief/Junkie) to be a pure function of `(seed, shiftIndex, botId)` with every bot playing each role ≥4 times over the campaign, so runs are deterministic and no bot is unfairly typecast as the griefer.
3. As a BUILDER, I want every verb exercised each shift — ≥4 CHUG calls, ≥1 FIX attempt per broken machine while a bot is near, ≥1 STEAL, ≥1 SABOTAGE — so artifact 12's evidence covers the full verb surface and carries the artifact-2 no-single-grief-style>50% rule.
4. As a BUILDER, I want strict per-shift grief variety (no grief style >50% of that shift's grief events, every bot commits and suffers theft over the campaign), so the balance evidence does not inherit scripted-greefer artifacts.
5. As a BUILDER, I want downtime defined as shift-seconds with no order AND no cup (rack + held + airborne) AND no broken machine, so the metric can genuinely fail and matches party dead-air intent, with cup-in-flight counting as alive.
6. As a BUILDER, I want campaign-mean downtime <15% and at most 3 of 20 shifts >20%, so "dead air" is proven rare and the party never sits waiting.
7. As a BUILDER, I want quota success between 55% and 70% of shifts with 30-50% close calls (missed by ≤2 serves, or met with ≤20s to spare), so the shared goal is tight but winnable.
8. As a BUILDER, I want chaos ≥8 events/min carried forward, so artifact-12 numbers inherit and reconfirm the artifact-2 gate.
9. As a BUILDER, I want theft counters by player (committed and suffered) added to `TipLedger`, with ≥90% of shifts having ≥1 steal, ≥60% of shifts with all 4 players theft-involved, and every player ≥5 committed / ≥5 suffered, so the "everyone ends a thief" promise is measured, not assumed.
10. As a BUILDER, I want cascades counted as a machine state machine (second machine broken while ≥1 already broken, OR a machine broken ≥30s), with ≥80% of shifts producing ≥1 and mean 1-3 per shift and `firstCascadeTime` ≥75s in ≥80% of shifts, so machine death spirals happen at party rate without first-minute rage.
11. As a BUILDER, I want the same cascade event stream to feed artifact-8's replay moments, so the balance evidence and the delivery axis share one source of truth.
12. As a BUILDER, I want vendetta detection as a read-only heuristic over the artifact-6 memory store (≥2 incidents from same player within ≤3 shifts → burn → vendetta within ≤1s → tip refusal), so vendetta is a measured customer behavior, not a scripted flag.
13. As a BUILDER, I want ≥2 vendetta detections over the campaign, every detected vendetta showing ≥1 `vendettaTipRefusal`, so vendetta is provably noticed and has an observable tip-economy effect.
14. As a BUILDER, I want the tip economy measured per shift (Gini ≤0.30, thief redistribution ≥10% of `tips_total`, all 4 players positive tips in ≥80% of shifts), so the theft-is-productive fantasy stays fair and legible.
15. As a BUILDER, I want `CampaignRunner` to write per-shift rows as they finish (crash-safe, no all-or-nothing campaign), so an hours-long in-editor run survives an editor crash.
16. As a BUILDER, I want the sweep to mutate only the injected `KnobSet` — `OrderSpawnInterval`, `CustomerPatience`, `QuotaTarget`, `PassiveDegradePerSecond`, `UseDegradePerCup` — with `ShiftLength`, `MaxActiveOrders`, `ProductionInterval`, `BrokenThreshold` hands-off and asserted in-band, so design constants are never swept away.
17. As a BUILDER, I want the sweep protocol to be one-at-a-time in priority order quota → spawn → patience → degrade, each candidate gated on the reduced 5-seed subset before the full 20-seed run, so cheap tier spends honestly and every tried value lands in EVIDENCE.
18. As a BUILDER, I want the winning KnobSet to become the shipped defaults in `RoundRules`/`MachineRules` and be re-validated at the authoritative 30Hz tick by artifact 13, so the sim never silently tunes the online game.
19. As a BUILDER, I want a repro check (same binary + same 20 seeds → identical aggregate hash, verified once and logged), so the evidence asset can be independently replayed.
20. As the RATER, I want gate evidence — `sim-campaign.json` with computed `gateChecks`, repro line, BUILD-SELF-REVIEW — before spending a review, so the expensive tier judges measured bands, not promises.

## Implementation Decisions

- **Role wheel, not fixed timestamps.** Per-shift role = `Role(seed + shiftIndex*4 + botId)` drawn from {Server, Saboteur, Thief, Junkie} via a small pure PRNG chain; hard constraints: every bot plays each role ≥4 times over 20 shifts, every shift has ≥1 Thief and ≥1 Saboteur. Servers run the existing serve-line waypoints; Saboteur adds sabotage moments (with a customer-burn script seeding vendettas); Thief adds a steal window; Junkie adds CHUG calls. Delete all fixed-time scripted moments — the policy is the seeded schedule. Per-shift guarantees: ≥4 CHUG calls; ≥1 FIX attempt per broken machine while a bot is near; ≥1 STEAL; ≥1 SABOTAGE; no grief style >50% of that shift's grief events (carries artifact-2 gate); every bot commits and suffers theft over the campaign. No skill modeling, no adaptive difficulty, no anti-grief arbitration — bots are dumb-but-faithful (fun tuning is artifact 8/13 territory).
- **Report schema.** Three outputs per campaign: `sim-report.json` (structured per-shift), `sim-report.txt` (human-readable per shift), `sim-campaign.json` (20 rows + aggregate + `gateChecks` computed in code). Persist seeds, knob set used, and a stable hash of the aggregate section as reproducibility evidence.
- **Operational definitions (numeric, non-negotiable):**
  - Downtime = fraction of shift-seconds where `ActiveOrders == 0` AND `RackCups + HeldCups + AirborneCups == 0` AND `BrokenMachines == 0`. Cup-in-flight counts as alive. Unknown (c) fun-ceiling options rejected: no fun model in a metric — that is RATER's job.
  - Quota success = share of shifts with `Serves >= QuotaTarget` (center ≈60%).
  - Close call = quota missed by ≤2 serves OR quota met with ≤20s remaining at the moment met.
  - Shift length = measured sim seconds step 0 to `EndShift` (`RoundRules.ShiftLength 180 + ShiftEndGrace 3`).
  - Cascade = second machine reaches `Broken` while ≥1 other machine already `Broken`, OR a machine stays `Broken` ≥30s; computed as a state machine over machine-health events (same event stream feeding artifact-8 replay moments).
  - Vendetta = heuristic over artifact-6 memory store: per-customer `{customerId, WHO, incidents[][]}`, incident types `FLUNG_HIT`, `EXPIRED_ORDER`, `SPILLED_ON`, `SCALDED`, `GEYSERED`; ≥2 incidents from the same player within ≤3 shifts → burn → vendetta flips immediately (≤1s), materializing as refusal-to-tip that player (next serve by that player = $0, observable in `TipLedger`) + vendetta dialogue flag. Read-only on artifact 6's store; no scripted flags, no fiat.
  - NPC latency = median ≤1.0s, p95 ≤1.5s, zero unreacted incidents, measured only where incident→reaction is scriptable.
  - Tip economy = `tips_total`, mean/player, Gini over 4 per-player tips, thief-redistribution share (% of `tips_total` ending on a thief, owner-id vs last-carrier credit via `TipLedger`).
  - Theft rate = `Steals`/shift plus new per-player `stealsCommittedByPlayer[4]` / `stealsSufferedByPlayer[4]` added to `TipLedger`/`CoreGame.TrySteal`.
- **New counters (each defended by a gate number):** `stealsCommittedByPlayer`, `stealsSufferedByPlayer`, `quotaMetTime` (shift-relative seconds when quota crossed), `cascadeCount`/shift + `firstCascadeTime`, `vendettaCount`, `vendettaTipRefusals`, `downtimeSeconds`, `npcLatencyMs` array, `fixLatency` (time from break to first FIX — cascade input). Counters stay `int`/scalar on `CoreGame`/`SimHarness`; no per-step allocation (perf rule). Cascade/downtime stay counter-based (first-cascade timestamp, downtime seconds), no time-series structures.
- **KnobSet injection.** Sweep set (5): `OrderSpawnInterval`, `CustomerPatience`, `QuotaTarget`, `MachineRules.PassiveDegradePerSecond`, `MachineRules.UseDegradePerCup`. Hands-off, asserted-in-band: `ShiftLength` (180s — the 3-4 min round is DESIGN), `MaxActiveOrders` (3 — UI simplicity), `ProductionInterval` (4s), `BrokenThreshold` (0.05), move speed, chug/overdose, fling tip formula, NPC patience (artifact 6 owns). Never mutate shipped constants during the sweep.
- **Sweep protocol.** One-at-a-time, priority order: quota → spawn interval → patience → degrade knobs. Every candidate runs the same 20-seed campaign set (seeds `1000..1019` fixed) so comparisons share identical demand sequences. First gate: candidate improves / fails-to-regress the target metric on the reduced 5-seed subset; only the winner gets the full 20-seed run. Every tried value + its aggregate row lands in EVIDENCE ("numbers asset" rule). Final acceptance = full 20-seed run with the winning KnobSet; hash recorded.
- **Crash-safe campaign writes.** Write rows as shifts finish; a campaign is never all-or-nothing. If a seed takes >60s wall per shift, batch all 20 shifts in one editor session and write rows incrementally.
- **Cost guard.** Worst case 2 passes × ~6 candidates × 20 seeds × 200s ≈ 8 hours sim time in editor — acceptable; the reduced-subset first gate keeps cheap tier honest.
- **Ownership.** Sweep + campaign = BUILDER (cheap tier), one file `CampaignRunner.cs` + pure `BalanceReport` helpers (TDD like `RoundRules`). No expensive-tier spend until the full campaign row exists, BUILD-SELF-REVIEW written, and the RATER call happens (escalation ladder step 4).
- **Gate evidence for artifact 13.** `sim-campaign.json` with (i) all band targets passing code-computed `gateChecks`, (ii) repro verified once and logged, (iii) BUILD-SELF-REVIEW appended to EVIDENCE before RATER, (iv) 2 consecutive RATER passes ≥8/10, 0 blockers, 0 majors. The winning KnobSet becomes shipped defaults; artifact 13 plays live on those defaults.
- **External dependencies.**
  - Artifact 6 (NPC cast) — HARD dependency: vendetta detection + NPC latency require the memory store's incident-append and burn-predicate API (`{customerId, WHO, incidents}` keying). If 6's memory model keys differently by run time, `npc_latency`/`vendettaCount` record `"pending"` — a blocker, not a skip. Gate keeps both as required fields.
  - Artifact 4 (machines) — machine-health events currently go to Roast only; needs a neutral per-shift `MachineEvent` list (small, named, no GC) for the cascade state machine.
  - Artifact 8 (juice/replay) — cascade event stream doubles as replay-moment feed; cascade metric stands alone even if 8's taxonomy diverges.
  - Artifact 10 (netcode) — authoritative server ticks at fixed 30Hz (ADR-0001); sim runs 50Hz (`TimeManager` 0.02). Balance numbers are valid for the sim configuration; knob-swept defaults MUST be re-validated at 30Hz before artifact 13's online sessions — owned by 10/13, not silently assumed.
  - Artifact 1 (metrics surface) — new counters land on the existing `CoreGame`/`SimHarness` counters; risk is the counters being `int`s, mitigated by staying counter-based.
- **Process constraints.** Main-branch-only (ADR-0005); one file at a time; `SimHarness.cs`/`CoreGame.cs` edits coordinate with artifact siblings; commit small. RATER budget: max 2 reviews per artifact; review #1 not spent until campaign row + self-review complete. Human prereqs: host Play Mode + in-editor SimHarness authorized; WebGL/license pending — artifact 12's evidence needs no build.

## Testing Decisions

- Good tests assert measured bands on campaign aggregates, never on implementation wiring or source text. The gate itself is the test suite: every number in `gateChecks` is computed by code from counters, then checked against its band.
- Unit-test `BalanceReport` as pure helpers exactly like `RoundRules` is TDD'd: definitions (downtime fraction, close-call classification, cascade state machine, Gini, thief redistribution share, vendetta burn predicate) get deterministic table tests with hand-computed expected values.
- Role-wheel determinism is a test: same (seed, shift, botId) triple → same role; over the fixed campaign, every bot plays each role ≥4 times and every shift has ≥1 Thief and ≥1 Saboteur.
- Repro is a test: run of the campaign on canonical seeds produces a stable aggregate hash; verified once and logged as evidence.
- Cascades and downtimes get targeted scenario tests: inject machine-break sequences and order/cup emptiness windows and assert cascade classification and downtime fraction.
- Prior art: existing 21 green EditMode tests for `RoundRules`/`MachineRules`; new `BalanceReport` tests follow that convention. SimHarness exists and its metric surface is the integration point — campaign-level assertions (quota band, theft involvement, chaos/min, cascades, vendettas) run through the harness, not mocks.
- No project-wide validation runs during sibling-concurrent work; verification happens once after all artifacts land.

## Out of Scope

- No new gameplay, no new NPC behavior — vendetta consumption is read-only on artifact 6's store.
- No difficulty scaling, rubber-banding, or per-bot skill models.
- No ANOVA/DOE — one-at-a-time sweep is the whole method.
- No CI/p-value machinery, no confidence intervals, no per-shift heatmaps.
- No campaign dashboards, no multi-shift persistence, no session/streak stats, no saving/loading campaigns.
- No anti-grief design (artifact 2 owns no-single-grief-style).
- No tuning of hands-off constants (`ShiftLength`, `MaxActiveOrders`, `ProductionInterval`, `BrokenThreshold`, move speed, chug/overdose, fling tip formula, NPC patience).
- No re-validation at 30Hz here — flagged loudly, owned by artifacts 10/13.

## Further Notes

- Fake-pass guard: every target is a band on measured counters steered by the injected `KnobSet`; gateChecks are computed in code, never edited by hand.
- STEAL-most-used is an artifact-2 gate, not repeated here.
- The cascade event stream is defined here and doubles as artifact-8's replay feed; if 8's moment taxonomy diverges, the cascade metric still stands alone.
- Memo did not reference the `kwaffee-6id` / "shift-seam" ticket; if such a refactor exists it stays external-only and its work is not absorbed here.

**Blender art needs: NONE.** Artifact 12 is pure code/data/netcode — bots, counters, sweep, report. State explicitly: zero new or re-authored meshes; no bpy scripts touched; no manifest changes; nothing imported via "Import Authored Art". Any visible art in the sim is the already-landed 12 authored meshes, used as-is, and the ART RULE is unaffected.

## Refined Gate

Runs on canonical seed set `1000..1019`, `KnobSet` recorded, aggregate hash recorded; each in `sim-campaign.json → gateChecks`, computed by code:

1. **Shift length:** mean in [180, 240]s; zero shifts outside [180, 250]s.
2. **Downtime:** campaign-mean < 15%; at most 3 of 20 shifts > 20% (definition: no order + no cups in play + no broken machine).
3. **Quota success:** 55-70% of shifts (center ≈60%).
4. **Close calls:** 30-50% of shifts (missed by ≤2, or met with ≤20s to spare).
5. **Chaos:** ≥8 events/min campaign-mean (artifact-2 gate carried).
6. **Theft:** ≥90% of shifts have ≥1 steal; ≥60% of shifts have all 4 players theft-involved; every player ≥5 committed and ≥5 suffered; no grief style >50% of any shift's grief events.
7. **Cascades:** ≥80% of shifts produce ≥1; mean cascades/shift in [1, 3]; `firstCascadeTime` ≥75s in ≥80% of shifts.
8. **Vendettas:** ≥2 detections over the campaign; all detected vendettas show ≥1 `vendettaTipRefusal`; detection latency ≤1s.
9. **NPC latency:** median ≤1.0s, p95 ≤1.5s, zero unreacted incidents (hard-blocked on artifact 6 API; `"pending"` is a blocker, not a skip).
10. **Tip economy:** per-shift Gini ≤0.30; thief redistribution ≥10% of `tips_total`; all 4 players positive tips in ≥80% of shifts.
11. **Reproducibility:** identical binary + same 20 seeds ⇒ identical aggregate hash (verified once, logged).
12. **Gate protocol:** BUILD-SELF-REVIEW appended before RATER; 2 consecutive RATER passes ≥8/10, 0 blockers, 0 majors; artifact 13 starts only after this gate closes, playing the tuned defaults.