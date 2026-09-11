# Artifact 6 — NPC cast (spec)

Source: `.scratch/kwafee/grilling/06-npc-cast.md` (grilled scope: customers WHO+memory+gossip, Shop Cat, Big Vinny, dealers, inspector, street life). Beads `kwaffee-1x1`, blocked by artifact 5.

## Problem Statement (from the user's perspective)

When I play a shift at KWA FEE, the room is full of people who should feel alive: customers order, judge, remember, and gossip; the Shop Cat knocks things over; Big Vinny narrates; the Health Inspector catches me slacking. Right now a customer is only an order with a patience meter — nobody walks, nobody reacts to the chaos I cause within a second, nobody remembers that I burned their grandma twice. A dead room makes the chaos meaningless. I want to wreck a coffee shop full of people who react like people: react fast, move without clipping, remember across shifts, never get stuck, never ignore a cup to the face, and never stop being funny. I should feel watched, judged, and remembered — and so should my friends.

## Solution (user perspective)

The shop becomes a living room:

- Customers walk in, pick a spot, and leave through a real walking loop (waypoints, not magic teleporting). Six WHO types with distinct personalities — the cop sighs at you, the influencer records you, the nurse tuts, the food critic grades, the grandma kisses the cat, the mayor's intern panics. Every shift is guaranteed a grandma and an influencer.
- Everyone reacts to what actually happens, within one second: a geyser, a spill, a burn, a machine dying, a tip stolen — loud NPCs react loudly, calm NPCs gossip about it to each other, all referencing the real event.
- The shop remembers between shifts (small JSON per cafe): burn the same kind of customer twice and she refuses to tip next shift; the tabloid has a vendetta subplot; Ma Paddy's grudge price carries over.
- The Shop Cat lives an 8-state life (sleeps, grooms, patrols, sits on machines, knocks keys, chases lasers, meows) and is unkillable — hit it with a cup and it ragdolls for a second, then gets right back to business. If I go AFK, it courteously covers my station (poorly: slower machines, random knock-overs).
- Big Vinny narrates in sting + text-banner: ~50 trigger rules keyed to real events, he takes an 8-12s smoke break mid-shift (his chaos window), and he never compliments.
- The Health Inspector watches the window; pile up filth and he patrols, suspicion visibly rising; sweep fast enough and you get the temporary-clean illusion; fail and the fine lands — and the next one is bigger.
- Passersby drift through the street outside the window; occasionally one walks through the door as a real customer.

Zero runtime LLM, zero network in any NPC path. Deterministic and seeded: same shift seed + same inputs replays identically.

## User Stories

1. As a player, I want customers to react to my flings and sabotage within one second, so that the room feels alive and my chaos has an audience.
2. As a player, I want six distinct customer types (off-duty cop, influencer, nurse, food critic, grandma, mayor's intern) with distinct signature behaviors, so that every shift has personality and no two customers read as the same person.
3. As a player, I want every shift to include at least one grandma and one influencer, so that the kiss-the-cat gag and the "you're being recorded" bit are never missing.
4. As a player, I want different customer types to react differently to the same stimulus, so that behavior feels like personality, not a script.
5. As a player, I want customers to gossip about real events that happened this shift, so that gossip is proof the world noticed me.
6. As a player, I want to burn the same kind of customer twice across shifts and see the vendetta pay off (refusal to tip, tabloid subplot), so that my cruelty has consequences.
7. As a player, I want the Shop Cat to live its own life (sleep, groom, patrol, sit on machines, knock keys, chase lasers, meow) and be unkillable, so that it is a beloved source of harmless chaos.
8. As a player, I want the Shop Cat to ragdoll when I fling a cup at it and then get back up, so that the cat feels like comedy physics, never a victim.
9. As an AFK player, I want the Shop Cat to cover my station poorly (slower machine, random knock-overs), so that leaving is punished by comedy, not by nothing.
10. As a player, I want Big Vinny to narrate real events with ~50 trigger rules and an 8-12s smoke-break silence window, so that his voice matches what happened and the chaos window is a real pressure drop.
11. As a player, I want the Health Inspector to build suspicion from filth, visibly patrol, and be foolable by a good sweep, so that failing an inspection reads as earned, never random rage.
12. As a player, I want failed inspections remembered so the next fine is bigger, so that repeating my filth has escalating cost.
13. As a player, I want customers to walk between authored waypoints without clipping through counters or the walls, so that the illusion of people is never broken.
14. As a player, I want passersby visible in the window (ambient 2D sprites), with the occasional one becoming a real customer through the door, so that the city feels alive for near-zero cost.
15. As a developer, I want every NPC decision seeded and deterministic, so that the `/sim` replay harness proves identical behavior for identical input.
16. As a developer, I want a single data-table schema for personalities and lines, so that new WHO or lines are data, not new code paths.
17. As a developer, I want gate proxies automated (reaction ≤30 ticks, ≥2 gossip lines/shift, zero clips, zero loops, zero ignored flames), so that RATER passes are evidence-backed.

## Implementation Decisions

1. **Movement: authored waypoint graph, NOT NavMesh.** One shop room, ≤10 concurrent customers, 3-4 minute shifts. Waypoints are static transforms (Spawn, ServiceQueue, Seats, Window, Exit). Movement = pick nearest waypoint + point-to-point steering with radius collision against static counter colliders (raycast per agent, no per-agent pathfinding, no NavMesh). Rationale: deterministic and replay-stable (NavMesh pathing is not `/sim`-replay-stable), zero bake cost, zero per-frame pathfinding/GC. The 1s and no-clip gates are met by 30Hz signal polling and steer-toward-nearest-waypoint + static-collider avoidance + shop-bounds clamp respectively. This is a deliberate, loudly-documented divergence from the spec's "NavMesh movement" wording; the RATER may contest it, and the fallback cost is swapping the steering driver only (movement contract unchanged). Record in `/DECISIONS.md`.
2. **WHO roster: all 6, no filler.** Cop, influencer, nurse, food critic, grandma, mayor's intern. Each has exactly one distinct signature behavior (cop sighs, influencer records you, nurse tuts, critic grades, grandma kisses the cat, intern panics). Spawn mix: weighted random over the 6, with hard per-shift guarantees of ≥1 grandma and ≥1 influencer. Distinct silhouettes are locked properly in artifact 7, but this artifact MUST ensure no two WHO read identical (personality gate).
3. **Personality schema: one static data table.** Per-WHO fixed base vector over 6 goals (`getCoffee`, `findSeat`, `complain`, `flee`, `film`, `gossip`) plus a patience scalar; per-instance value = base + seeded jitter. All dialogue/gossip/reaction lines keyed by `(WHO, goal, eventTag)`. One schema, one table, zero per-WHO code; behaviors differ under identical stimulus purely from the weight vector (this divergence is a gate proxy).
4. **Reaction contract: single shared `WorldSignals` table + one discrete event ring buffer, polled by every NPC every FixedUpdate.** Authoritative server-side, so "1s" is server sim time — network is irrelevant to the gate (client interpolation already decoupled per ADR-0001). Ring buffer: last 16 events, each `(eventTag, sourcePlayer, position, magnitude, timestamp)`. Event tags: `flingHit`, `spill`, `burn`, `machineDeath`, `geyser`, `sabotage`, `steal`, `overpressure`, `beanSack`, `tripWire`, `quotaMilestone`, `catGremlin`, `vanIncident`, `vendettaTrigger`, `failInspection`. Continuous scalars also published each tick: `chaosMeter` (0-1), `filthLevel` (0-1), `tipEconomy` (recent tip delta), `playerReputation[playerId]` (per-NPC per-player, from memory), `machineHealth` (worst + mean), `timePressure` (shift clock), `noise` (recent event burst). Each NPC type has an interest filter — WHO → which tags it consumes + magnitude threshold. Matching events are consumed within 1s → reaction line + state impulse, both data-driven off the real `(WHO, eventTag)` pair; gossip therefore always references real events. No event older than 30 ticks may be consumed.
5. **Memory store: small per-cafe JSON at archetype + player level, never instance level.** Customers respawn procedurally each shift; individual instances do not persist. Persisted: per-archetype burn count (vendetta triggers at 2 burns → refusal to tip + tabloid subplot), per-player reputation per customer, vendetta flags, gossip continuity seeds, Ma Paddy grudge price multiplier. NOT persisted: positions, current orders, patience (recomputed per shift). One JSON per cafe on the server, written at shift end, capped at a few KB.
6. **Big Vinny: text-banner + code-synth sting; no runtime TTS.** TTS rejected (expensive, brittle, breaks mute/pacing). Trigger table keyed by `(eventTag, magnitude)` → line id + optional combo line; ~50 lines covering machine death, geyser, steal, burn, overdose, quota, sabotage, cat gremlin, inspector, smoke-break. Resolution: highest-priority matching buffered event wins per tick; per-line cooldown prevents spam. Smoke-break = timed 8-12s silence window mid-shift (the spec's chaos window). Vinny never compliments. Sting requests are emitted as events (id + priority); actual audio synthesis is artifact 9's.
7. **Shop Cat: 8 exact states — idle/patrol, sleep, groom, sitOnMachine, knockKeys, laserChase, meow, coverStation.** Utility drive = boredom + attention + chaos. Unkillable: takes no damage; any fling hit → 1-2s cosmetic ragdoll, then reset to nearest idle state. AFK cover: player with no input > N s → cat walks to that machine, "helps" = reduced machine throughput + random knock-overs + meow; resumes normal states when the player returns. Ragdoll is cosmetic only — reaction flags and state transitions stay deterministic (physics ragdoll driving state would break `/sim` replay); ragdoll uses seeded RNG.
8. **Inspector: scout → investigate → verdict loop.** Idle at window; if `filthLevel` > threshold, suspicion meter rises and the inspector does a visible patrol walk investigating stations; suspicion full → fail inspection (fine). Sweeping reduces filth AND drains suspicion faster — the "temporarily clean" illusion. The loop re-enters while suspicion is active. Failed inspections are remembered; each subsequent failure's fine is multiplied (via memory JSON).
9. **Street life: pure ambient, no NPC sim.** Passersby are flat 2D SVG/code sprites in the window (looping bobble-walk, few dozen max) — no NavMesh, no AI, no physics. A flagged passerby "becomes a special customer" by spawning a real customer through the door, reusing the existing spawn path. Full passerby NPC sim rejected: zero gameplay value, pure cost.
10. **Automated gate proxies** (this is the exact automated contract): (a) harness stamps an event, asserts the target NPC's reaction flag within ≤30 fixed ticks (30Hz = 1s); (b) harness counts distinct real-event-referencing (eventTag-matched) gossip emissions per shift, requires ≥2; (c) every tick, every active NPC asserted within shop bounds AND with min separation to static counter colliders (distance to nav graph + collider clearance ≥ radius); (d) state-machine watchdog — any `(state, target)` dwell >6s forces a transition; harness asserts no dwell exceeds the threshold; (e) harness flings a cup at an NPC, asserts ragdoll/reaction within 1s (the "never ignores a fling to the face" proxy).
11. **Dealer integration: consume, don't author bodies.** This artifact wires the NPC memory store alongside artifact 5's dealer memory and consumes the Ma Paddy grudge-price interface (grudge multiplier read from memory JSON). Dealer bodies (Big Sal, Rosie, Ma Paddy) are authored in artifact 5, not here.
12. **Determinism & performance constraints.** Seeded RNG everywhere; same seed + scripted input → identical reaction/state sequence. Concurrent active customers capped ~8-10 (pooled); street life is static sprites; no per-frame allocations, no GC storms. Zero runtime LLM/network calls in any NPC path — enforced by static assertion/code-review.

## Testing Decisions

- **What makes a good test here:** a test that proves an observable contract under the adversarial gates — reaction latency, gossip honesty (references a real buffered event tag), geometry safety (no clip), liveness (no stuck loop), responsiveness (no ignored fling), personality divergence, determinism, and cross-shift memory. Harness assertions beat unit tests for the behavioral gates; unit tests cover the data tables and state machines.
- **Automated gate proxies (the harness is the primary test surface):** the five proxies from Implementation Decision 10 — ≤30-tick reaction, ≥2 eventTag-matched gossip lines per shift, per-tick bounds + collider clearance for all active NPCs, ≤6s state/target dwell, fling-at-NPC → ragdoll/reaction within 1s.
- **Refined acceptance assertions (all harness-asserted via `/sim`):** (1) no event older than 30 ticks consumed; (2) ≥2 distinct gossip/reaction lines per shift referencing a real buffered eventTag; (3) zero clips (bounds + collider clearance every tick); (4) zero stuck loops (watchdog, >6s forces transition); (5) zero ignored flames (ragdoll/reaction ≤1s); (6) same stimulus + matched noise seed → ≥2 WHO take different next-state/goal; (7) all 6 WHO + cat + inspector present with distinct silhouette (feed-forward to artifact 7's check); (8) same seed + scripted input → identical reaction/state sequence; (9) no runtime LLM/network call in any NPC path (static assertion / code-review); (10) burn 2× → vendetta flag + refusal-to-tip next shift (cross-shift harness); (11) AFK >N s → cat at that station, machine throughput reduced; (12) filth above threshold → visible patrol, sweep drains suspicion, failed inspection → next fine bigger.
- **Prior art and substrate:** customer basics (order spawn + patience + Boston demand) already live in RoundRules.cs — this sim extends, not replaces, it. No walking NPC sim exists yet; the waypoint stepper, WorldSignals table, and ring buffer are new. Existing Barista/Customer rigs (capsule, authored core.glb) are reused for customers/inspector. EditMode test conventions already green (21 tests) are the pattern for the data-table and state-machine unit tests; SimHarness is the shell for the behavioral assertions (`/sim` numbers are not yet recorded as a baseline — the harness exists and this artifact starts recording them). Watchdog and gate assertions live in the harness so they run on every shift simulation.
- **Determinism parity:** tests must run the same seeded scenario twice and diff the full reaction/state trace; the ragdoll path is excluded from state-deriving assertions (cosmetic only) but included in the "zero ignored flames" latency proxy.

## Out of Scope

- Runtime TTS for Big Vinny (sting + text-banner only; sting audio itself lives in artifact 9 — this artifact emits sting requests).
- NavMesh pathing and baking (deliberate divergence, see Implementation Decision 1).
- Per-instance customer persistence (archetype + player level only).
- Full NPC sim for street-life passersby (2D ambient sprites; the door-spawn path is the only bridge).
- Dealer bodies (artifact 5), dealer banter assets beyond memory/grudge interface consumption.
- The silhouette/visual-identity lock pass (artifact 7) — but this artifact must not ship two WHO that read identical.
- Client-side movement, prediction, or interpolation (ADR-0001 owns that; this is server sim + state).
- Gross world systems: quotes, awards, tabloids beyond the vendetta hook, catastrophe replay — other artifacts' scope.
- Any damage/death path for the Shop Cat (unkillable by design).

## Further Notes

- **Risks carried into implementation:**
  - *NavMesh divergence:* spec wording says "NavMesh movement"; the gates are 1s reaction + no-clip, both met cheaper and deterministically by waypoints. Document loudly in `/DECISIONS.md`; the fallback (swapping the steering driver) does not change the signal/memory contracts.
  - *Ragdoll nondeterminism vs `/sim`:* ragdoll is cosmetic; all decisions/reactions deterministic via seeded RNG; ragdoll must never drive state transitions.
  - *1s reaction vs network:* server-authoritative polling makes "1s" server sim time; no cross-network latency sits in the gate.
  - *Inspector fairness:* generous filth threshold + sweep-drain so failure reads as earned, never rage; tune via `/sim`, not judgment.
  - *Performance:* bounded NPC counts (pool ~8-10), static street sprites, zero per-frame allocs.
  - *Memory size:* archetype-level JSON keeps per-cafe persistence at a few KB.
- **WorldSignals noise scalar** is the "recent event burst" rate; it feeds the cat's chaos drive and Vinny's intensity, not just reaction lines.
- **Vinny line quality bar** is the project's Boston-voice bar: funny or not done; he never compliments; smoke-break is timed (8-12s), not event-triggered.
- Art needed: 6 customer variants (cop hat, influencer phone, nurse cap, critic notebook, grandma gray bun, intern briefcase) + Shop Cat (new 4-leg, spring-tail felt rig, anims for all 8 states + ragdoll) + inspector (badge/visor accent) via the bpy pipeline — manifest-validated, ≤35,000 triangles, grounded y=0, ART RULE respected (2D/UI in code/SVG; passersby are code/SVG sprites).

**Refined Gate** — this artifact passes when ALL hold (harness-asserted):
1. Every NPC polls `WorldSignals` + event ring buffer each FixedUpdate (30Hz); no event older than 30 ticks consumed (1s reaction).
2. ≥2 distinct gossip/reaction lines per shift referencing a real buffered `eventTag`.
3. Zero clips: all active NPC positions pass shop-bounds + collider-clearance checks every tick.
4. Zero stuck loops: no `(state, target)` dwell >6s; watchdog forces the transition.
5. Zero ignored flames: fling at an NPC → ragdoll/reaction within 1s.
6. Personalities differ: identical event + matched noise seed → ≥2 WHO produce different next-state/goal.
7. All 6 WHO + Shop Cat + inspector present, no two WHO read identical (silhouette distinctness carried into artifact 7's gate).
8. Determinism: same seed + scripted input → identical reaction/state sequence (`/sim` trace diff).
9. No runtime LLM/network call in any NPC path (static assertion / code-review).
10. Memory persists: burn 2× → vendetta flag + refusal to tip in the next shift (cross-shift harness).
11. Cat covers AFK: AFK >N s → cat at that station, machine throughput reduced.
12. Inspector: filth above threshold → visible patrol; sweep drains suspicion; failed inspection → next fine bigger.
13. Performance: ≤10 concurrent active customers, no per-frame allocations; per-cafe memory JSON ≤ few KB.