# Artifact 6 — NPC cast (grilled)

- Beads id: `kwaffee-1x1` (blocked by 05)
- Grilled scope: customers WHO+memory+gossip, Shop Cat, Big Vinny, dealers (memory), inspector, street life. All local sim, zero runtime LLM. Decide NavMesh vs waypoints, signal contract, memory shape, gates' automated proxies.

Repo ground truth: customer basics = order spawn + patience + Boston demand (RoundRules.cs) only; no walking NPC sim exists. Existing rigs: Barista/Customer prefabs (capsule, authored core.glb). No NavMesh baked anywhere.

---

## Design tree (decisions resolved)

### 1. Movement: NavMesh vs authored waypoints
- Options: (a) Unity built-in NavMeshAgent + baked navmesh, (b) authored waypoint graph + steering.
- **DECIDED: authored waypoint graph.** One shop room, ≤10 concurrent customers, 3-4 min shifts. Waypoints = static transforms (Spawn, ServiceQueue, Seats, Window, Exit). Movement = pick nearest waypoint + point-to-point steering with radius collision against counter colliders (raycast, no per-agent pathfinding). Deterministic + seeded (NavMesh pathing is not /sim-replay-stable), zero bake cost, zero per-frame pathfinding/GC. 1s reaction satisfied by polling signals every FixedUpdate (30Hz). No-clip gate satisfied by steer-toward-nearest-waypoint + static-collider avoidance + shop-bounds clamp. NavMesh rejected: overkill for one room, non-deterministic pathing, bake step, extra allocs. RATER risk noted: this is the deliberate divergence from the spec's "NavMesh" word — spec language, not a hard requirement; waypoints meet both stated gates cheaper.

### 2. WHO list final size + spawn mix
- Options: trim to 3-4 WHO / keep all 6 / add filler "regular".
- **DECIDED: keep all 6, add no filler.** Cop, influencer, nurse, food critic, grandma, mayor's intern — each gets one distinct signature behavior (cop sighs, influencer records you, nurse tuts, critic grades, grandma kisses the cat, intern panics). Spawn mix: weighted random 6-WHO, with hard guarantees per shift: ≥1 grandma (kiss-cat gag), ≥1 influencer (records). Filler rejected: no budget, 6 already covers the funny spread. Distinct silhouettes reserved for artifact 7 but this artifact must NOT let two WHO read identical (see acceptance).

### 3. Personality weight schema
- Options: per-WHO bespoke script vs data table.
- **DECIDED: static data table** — per-WHO fixed base vector over 6 goals (getCoffee, findSeat, complain, flee, film, gossip) + patience scalar; per-instance = base + seeded jitter. Data-driven: lines keyed by (WHO, goal, eventTag). One schema, one table, no per-WHO code. Behaviours differ under identical stimulus purely from the weight vector (gate).

### 4. Reaction contract: exact world-state signals
- Options: direct event subscription vs polled signal table.
- **DECIDED: one shared `WorldSignals` table + one discrete event ring buffer, polled by every NPC each FixedUpdate.** Authoritative server-side, so "1s" is server sim time, network irrelevant.
  - Continuous scalars: chaosMeter (0-1), filthLevel (0-1), tipEconomy (recent tip delta), playerReputation[playerId] (per-NPC per-player, from memory), machineHealth (worst + mean), timePressure (shift clock), noise (recent event burst).
  - Event ring buffer (last 16, each: eventTag, sourcePlayer, position, magnitude, timestamp): flingHit, spill, burn, machineDeath, geyser, sabotage, steal, overpressure, beanSack, tripWire, quotaMilestone, catGremlin, vanIncident, vendettaTrigger, failInspection.
  - Each NPC has an interest filter (WHO → which tags it consumes + magnitude threshold). Consumes matching events within 1s → reaction line + state impulse. Gossip/reaction lines are data-driven off (WHO, eventTag) — proves "gossip references real events".

### 5. Memory store shape (what persists per cafe)
- Options: persist named customer instances vs aggregate reputations.
- **DECIDED: small per-cafe JSON, archetype + player level, NOT instance level.** Customers are procedurally respawned each shift — individual instances do not persist. Persist: per-archetype burn count (vendetta triggers at 2 burns), per-player reputation per customer, vendetta flags, gossip continuity seeds, Ma Paddy grudge price multiplier. Do NOT persist: positions, current orders, patience (recomputed per shift). Cap ~few KB, one JSON per cafe on server, written at shift end. Vendetta = "burn them twice → refusal to tip + tabloid subplot" (spec) — satisfied via archetype burn counter.

### 6. Big Vinny trigger-table depth
- Options: full TTS at runtime vs text-banner + sting.
- **DECIDED: text-banner + code-synth sting, no runtime TTS.** TTS = expensive, brittle, breaks mute/pacing. Trigger table keyed by (eventTag, magnitude) → line id + optional combo line. ~50 lines covering machine death, geyser, steal, burn, overdose, quota, sabotage, cat gremlin, inspector, smoke-break. Priority: highest-priority matching buffered event wins per tick; cooldown to prevent spam. Smoke-break = timed 8-12s silence window mid-shift (chaos window, spec). Never compliments (spec). Sting requests emitted as event; actual audio lives in artifact 9.

### 7. Shop Cat state machine + AFK cover
- **DECIDED exact states:** idle/patrol, sleep, groom, sitOnMachine, knockKeys, laserChase, meow, **coverStation**. Utility drive = boredom/attention/chaos. Unkillable: takes no damage; any fling hit → ragdoll 1-2s (cosmetic physics) then reset to nearest idle state. **AFK cover:** player with no input >N s → cat walks to that machine, "helps" = reduced machine throughput + random knock-overs + meow; resumes normal states when player returns. Ragdoll is cosmetic only — reaction flags/state transitions stay deterministic (physics ragdoll alone would break /sim replay).

### 8. Inspector loop
- **DECIDED:** idle-at-window → if filth > threshold, suspicion meter rises + visible patrol walk (investigate stations) → if suspicion full, fail inspection (fine). Sweeping reduces filth AND drains suspicion faster (the "temporarily clean illusion"). Loop = scout → investigate → verdict, re-enters while suspicion active. Failed inspections remembered → next fine bigger (multiplier in memory JSON).

### 9. Street life cost cap
- **DECIDED: pure ambient, no full NPC sim.** Passersby = flat 2D SVG/code sprites in the window (looping bobble-walk, few dozen max), no NavMesh, no AI, no physics. A flagged passerby "becomes special customer" = spawn a real customer through the door (reuse spawn path). Cost trivial. Full NPC sim for passersby rejected: zero gameplay value, pure cost.

### 10. Automated gate proxies
- **1s reaction:** harness stamps an event, asserts the target NPC's reaction flag set within ≤30 fixed ticks (30Hz = 1s).
- **≥2 gossip lines/shift:** harness counts distinct real-event-referencing (eventTag-matched) gossip emissions per shift ≥2.
- **no clips:** every tick, assert every active NPC within shop bounds AND min separation to static counter colliders (distance to nav graph + collider clearance ≥ radius).
- **no stuck loops:** state-machine watchdog — any (state, target) dwell >6s forces transition; harness asserts no dwell exceeds threshold.
- **no ignored flames:** harness flings a cup at an NPC; assert ragdoll/reaction within 1s (proxy for "never ignores a fling to the face").

---

## Refined acceptance criteria (additions to gate, in artifact's own language)

1. 30Hz poll: every NPC reads WorldSignals + event buffer each FixedUpdate; no event older than 30 ticks consumed. (1s reaction.)
2. ≥2 distinct gossip/reaction lines per shift referencing a real buffered eventTag (counted by harness, ≥2).
3. Zero clips: all active NPC positions pass bounds + collider-clearance check every tick (harness-asserted).
4. Zero stuck loops: no (state, target) dwell >6s; watchdog forces transition (harness-asserted).
5. Zero ignored flames: fling-at-NPC → ragdoll/reaction within 1s (harness-asserted).
6. Personalities differ: same stimulus → ≥2 WHO produce different goal/state outcome (harness asserts divergent next-state under identical event + matched noise seed).
7. All 6 WHO + cat + inspector present with distinct silhouette (carried into artifact 7 check).
8. Determinism: same seed + scripted input → identical reaction/state sequence (via /sim).
9. No runtime LLM/network call in any NPC path (static assertion / code-review).
10. Memory persists: burn 2× → vendetta flag + refusal-to-tip in next shift (harness, cross-shift).
11. Cat covers AFK: AFK >N s → cat at that station, machine throughput reduced (harness).
12. Inspector: filth > threshold → visible patrol; sweep drains suspicion; failed inspection → next fine bigger (harness).

---

## Blender art needs (bpy pipeline, manifest-validated)

Reuse the existing Customer/Barista humanoid rig (core.glb). New authored via `blender -b -P` per-asset bpy scripts + shared helpers, exported .glb, imported via "Import Authored Art":
- 6 customer WHO variants: cop (hat), influencer (phone), nurse (cap), food critic (notebook), grandma (gray bun), mayor's intern (briefcase). Distinct silhouettes + accessory; one rig, tint/palette + accessory variation.
- Shop Cat: new 4-leg + spring-tail rig, felt material (needs cat skeleton + idle/patrol/sleep/groom/sit/knock/laser/meow/ragdoll anims).
- Inspector: reuse human rig + badge/visor accent.
- Street-life passersby: **none in Blender** — flat 2D SVG/code sprites (ART RULE: 2D/UI in code/SVG).
- Dealer bodies (Big Sal, Rosie, Ma Paddy): authored in **Artifact 5** (cross-ref), not here; this artifact only consumes their memory/grudge interface.

## Dependencies

- Artifact 1 (round loop): customer order spawn/patience/demand — the substrate this NPC sim extends.
- Artifact 2: AFK detection + verb events that feed WorldSignals (flingHit, steal, sabotage, burn).
- Artifact 4: machine health/geyser/sabotage events feeding signals + cat sit-on-machine + inspector machine checks.
- Artifact 5: dealer memory + Ma Paddy grudge price interface (this artifact wires NPC memory store alongside it); dealer bodies.
- Artifact 9: code-synth stings for Vinny text-banner + reaction stings (this artifact emits sting requests only).
- ADR-0001 (headless authoritative server): NPC sim runs server-side; 30Hz poll aligns with 30Hz sim tick. ADR-0005 (main-only): per-file ownership; NPC scripts owned by this ticket.

## Risks

- **NavMesh divergence contested by RATER.** Spec says "NavMesh movement" but the gates are 1s reaction + no-clip. Authored waypoints meet both, cheaper + deterministic. Loudly documented; if RATER rejects, cost is swapping the steering driver only (contract unchanged).
- **Ragdoll physics nondeterminism vs /sim.** Ragdoll is cosmetic; all decisions/reactions deterministic via seeded RNG. Ragdoll must not drive state transitions or the replay harness breaks.
- **1s reaction vs network.** Server-authoritative polling makes "1s" server-sim time; client interpolation already decoupled (ADR-0001). No cross-network latency in the gate.
- **Inspector fairness.** Generous filth threshold + sweep-drain so failure reads as "we deserved it" (fun, never rage). Tune via /sim, not judgment.
- **Performance.** Cap concurrent active customers (pool ~8-10), street life static sprites, no per-frame allocs. NPC count bounded.
- **Memory size.** Per-cafe JSON must stay few-KB; archetype-level (not instance-level) persistence keeps it small.
