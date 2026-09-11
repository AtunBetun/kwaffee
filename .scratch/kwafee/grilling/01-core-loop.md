# Artifact 1 — Core loop prototype — grilling memo

Beads: `kwaffee-uq2` — status `open`, label `ready-for-human`, 0 deps.
Grilled scope: what "core loop gate passed" means with the loop MOSTLY built (playable 3-min shift, 4 machines, 21 EditMode tests, 12 Blender meshes, live Play Mode probe recorded in EVIDENCE.md), and the metrics contract Artifacts 2-13 inherit.

---

## Design tree (decisions resolved)

### B1. What does "gate passed" mean for Artifact 1?
- Question: spec gate (charge readable, flight floaty-funny, spill legible, loop self-teaches, /sim CLI with recorded numbers) vs current asset: structure built, feel unmeasured, human fun evidence absent.
- Options: (a) claim gate on structure + tests + live probe; (b) gate closes only on measured evidence; (c) split structural vs feel.
- **DECIDED: (b), gate stays OPEN.** The gate is a measurement gate, not a code gate. Two criteria (charge readable, spill legible) are defensible from code + EditMode + live probe; three are not yet evidenced (flight floaty-funny, loop self-teaches, /sim recorded numbers). Closing checklist is exactly B5. No pass claim before then — matching EVIDENCE's standing no-RATER-pass posture.

### B2. RATER pass-count contradiction (the forgotten blocker)
- Question: PROMPT gate demands min 3 RATER passes; ops budget caps 2 reviews/artifact. Unresolved in DECISIONS, and it blocks ANY artifact gate from formally closing.
- **DECIDED: preserve the 2-review spending ceiling; record the contradiction in EVIDENCE and do not claim "gate passed" until the human corrects policy.** 2 consecutive ≥8/10 with 0 blockers/0 majors in the 2 allowed reviews = "gate-ready pending policy correction". Never burn astra on a 3rd review to satisfy a contradictory clause.

### B3. /sim runtime form: "runs CLI" vs in-editor authorization
- Question: spec criterion says CLI harness; DECISIONS bans batchmode builds (license) and only authorizes in-editor host execution.
- **DECIDED: in-editor invocation via MCP execute_code calling `SimHarness.Run(seed, seconds)` IS the evidence-equivalent for Artifact 1.** SimHarness is already headless-shaped (drives CoreGame.Step + real engine physics at fixed 50 Hz, no input/presentation, manualSim guard). The true CLI/headless form lands with the headless server in Artifact 10 where Unity batchmode becomes legal. Do not retry batchmode to satisfy a letter of the criterion.

### B4. /sim metrics contract (Artifacts 2-13 inherit this)
- Question: what exact schema, seed, and location, so every artifact gates on the same numbers?
- **DECIDED:** one canonical gate run per artifact: **seed = `20260910` (pinned artifact-1 date), 180 s (one 3-min shift), 4 bots, 50 Hz fixed step, CoreGame-initialized `InitializeSim(seed, botMode: true)`**. Report:
  - Location: `<Application.persistentDataPath>/kwafee-sim/sim-report.txt` at runtime; **commit the canonical copy in-repo at `.scratch/kwafee/sim/gate-1/<seed>.txt`** so later artifacts diff against a checked-in baseline.
  - Keep existing fields verbatim (no renames — they're already the language of EVIDENCE): `chaos_per_min`, `chaos_total`, `serves`, `tips_total`, `tips_by_player`, `shots`, `hits`, `misses`, `catches`, `chugs`, `overdoses`, `fixes`, `sabotages`, `steals`, 4× `machine_health`, `broken`, `rack_cups`, `expired_orders`, `grab_tries/grab_success`, bot positions, per-bot verb attempts.
  - **ADD** (all one-line additions, zero renames): `seed=`, `determinism_hash=` (FNV-1a over step-count, serves, chaos, tips per player, broken machines — same-machine reproducibility proof), `quota_served=`, `downtime_seconds=` + `downtime_pct=` (Artifact 12 gates ≤15%), `machine_cascades=` (events where ≥2 machines broken within a 20 s window).
  - Chaos/min is **recorded, not gated**, at Artifact 1 (≥8/min is the Artifact-2 blocker). This keeps the Artifact-1 gate from silently absorbing a later gate.
- No new metric beyond the above — friendslop rule: metrics that no gate reads are deleted. These 8 additions all have consumers in Artifacts 2/4/12.

### B5. What precisely remains to close the Artifact-1 gate
1. Run `/sim` in-editor (authorized 2026-09-10): commit report `sim/gate-1/20260910.txt`, run twice same-machine, assert `determinism_hash` matches.
2. Host Play Mode observation session: charge readability, flight fun, spill legibility, hit consequence — recorded as live-state probes + screenshots (EVIDENCE already establishes this as the authoritative evidence path; the black-screen render artifact is known — probes, not pixels).
3. Self-teach: provisional evidence = owner's own host play session (fling→serve cycle discovered without instruction). Full naive-human laugh test is **NOT in Artifact-1 scope** — spec's gold-standard human test sits after Artifact 8 and at the end; cheap now, expensive now is waste.
4. BUILDER self-review vs the five criteria, then ≤2 RATER reviews (B2).

### B6. Charge curve: readability contract
- Question: what charge behavior do we declare "readable" and thus gate on? Options: trajectory arc always / arc at full charge only / no arc, wind-up visuals only.
- **DECIDED: no trajectory arc. Wind-up carries readability** — cup squash-stretch (THE LOOK rule 7: fling winds up like Gamble) + hold-time ring on the cup (flat candy UI, rule 8, code-drawn 2D — NOT a Blender mesh). Miss feedback is the teacher: "TOO WEAK" / "TOO HOT" in the Boston demand banner. Target curve to land on: **hold linear 0→1.2 s; release requires ≥0.35 s hold (below = drop, no launch); launch speed monotonic in hold to ~14 m/s.**
- Arc overlay is a candidate Artifact-8 juice addition, not Artifact-1 scope.

### B7. Flight & hit consequences: cut/keep fights
- Question: which has-to-exist flight/spill/hit systems survive the friendslop lens?
- Keep: spill = bounce + floor stain decal (code), hit barista = stun, serve = tip to carrier, miss = cup carries on (physics is the joke). All exist; all gate-evidenced in B5 step 2.
- **CUT:** per-cup liquid volume state machine (spill is binary: intact/stained); charge power tiers beyond linear hold; combo/streak scoring (never specced); wobble physics beyond Rigidbody (cups already Rigidbody; visual wobble only).
- **CUT to later artifacts:** hitstop/slow-mo/camera shake → Artifact 8; Shop Cat AFK → Artifact 2; machine degrade/cascade depth → Artifact 4; tabloid/upgrades → round-structure artifacts. Awards summary already built — keep, it's free.

### B8. Blender art deltas
- Question: does remaining Artifact-1 work need new authored art (e.g. a charge feedback prop)?
- **DECIDED: none.** Charge feedback = existing cup rig (code-side scale-stretch) + code-drawn 2D ring. The 12 meshes (8 + 4 machines, 17,112 tris ≤ 35,000 cap, grounded, manifest-validated) already satisfy the ART RULE for everything Artifact 1 renders. If any prop is added before gate close, it MUST enter via the bpy pipeline + manifest (required-name, ≤35k, y=0 grounded, per-submesh material indices) and the `Import Authored Art` step — no shortcuts, ever.

---

## Refined acceptance criteria (additions to the artifact's gate, own language, numeric)

- AC-1: `/sim` with seed `20260910`, 180 s, 4 bots exits clean and writes all contract fields (B4); a second same-machine run reproduces `determinism_hash` bit-identical.
- AC-2: chaos/min, downtime%, serves, tips_by_player are present and finite in the committed report (recording only; no thresholds at Artifact 1).
- AC-3: charge hold < 0.35 s never launches (guard verified in EditMode test); launch speed strictly monotonic in hold 0.35→1.2 s.
- AC-4: full-charge cup flight stays airborne ≥ 0.8 s and lands or hits something (no air-pops, no sink-through).
- AC-5: every hit has a visible consequence within ~1 s: serve → tip to last carrier; barista hit → stun; miss → bounce + stain. No silent outcomes.
- AC-6: self-teach: an observed session (owner host play, provisional) performs grab→charge→fling→serve without instruction, using only demand lines + HUD.
- AC-7: charge/spill/hit visual feedback is code/2D, zero new Blender meshes (guards B8).
- AC-8: no per-frame allocation in `CoreGame.Step` / `SimHarness` loop beyond pooled objects (steady-heap probe; regression = blocker per standards).

## Blender art needs
- None for remaining work. Named assets that must remain pipeline-authored and already validated: Cup, Barista, Customer, Counter, Floor, Wall, Tray, Sign, Espresso, Grinder, SteamWand, IceMachine (12 required meshes, manifest-checked).

## Dependencies + risks

- **Dependencies:** none from prior artifacts (Artifact 1 is first). Inputs already landed: 12 meshes + scene (EVIDENCE 2026-09-10), CoreGame/VerbRules/RoundRules/SimHarness, 21 EditMode tests green, owner authorization for in-editor /sim + host Play Mode.
- **Human prereqs:** Unity Personal license + WebGL Build Support remain NOT activated. Not needed for the Artifact-1 gate (in-editor play only), but blocking WebGL evidence and the Artifact-10 headless server. Never retry batchmode.
- **/sim authorization interplay:** in-editor execution is authorized and conditional on the owner standing direction; container/VM stays the ceiling for anything network-facing. SimHarness was built under that constraint: it steps the real engine physics scene, so its numbers are the same numbers Play Mode sees — no separate feature needed.
- **Risky assumption 1 — Rigidbody determinism:** Unity physics is not cross-platform/cross-machine deterministic. `/sim` determinism is claimed **same-machine, same-build, pinned engine 6000.6.0f1, fixed 50 Hz via TimeManager + manualSim** only. `determinism_hash` evidence must always come from two same-machine runs. Cross-machine consistency is deliberately **not** claimed — the Artifact-10 authoritative server removes the need (server is the single authority). This assumption is stated loudly in DECISIONS so no later artifact reads reproducibility into it.
- **Risky assumption 2 — machine colliders non-trigger:** FIX/SABOTAGE target machines by position math (Unity rejects concave triggers). Risk: players can't tell machines are interactive; mitigation is proximity highlight, deferred to Artifact 4 — do not silently inherit as "fixed" before then.
- **Risky assumption 3 — owner-as-human datapoint** is weak (design-aware, self-interested). Recorded as provisional in EVIDENCE; the formal ladder stays at Artifacts 8/11 per spec. Not promoted to stronger evidence without the human test.
- **Risk 4 — the 3-pass gate contradiction (B2)** must be corrected by the human before ANY artifact can formally close. Until then: "gate-ready, policy-pending."