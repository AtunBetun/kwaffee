# Artifact 4 — Machines & Sabotage
- Beads id: `kwaffee-kvu` | Slug: `04-machines-sabotage`
- Grilled scope: overpressure/geyser/steam-cascade system on the 4 existing machine prefabs, the FIX/SABOTAGE push-pull, the cascade determinism contract owed to Artifact 8's REPLAY, and the ≥80% replay-worthy gate.
- Current repo state (ground truth, from EVIDENCE): machines are real scene objects (Espresso/Grinder/SteamWand/IceMachine prefabs) with a single `health: float 0..1`, passive drain + use-degrade per cup, `IsBroken` at a threshold, production delay lerps with health, FIX `Repair(+0.5)` and SABOTAGE `-0.35` as generic health nudges, seeded `System.Random` in CoreGame, pooled cups/backlog. NO overpressure, NO geyser, NO steam scald, NO per-kind state machine, NO cascade chain, NO replay stream. `/sim` harness drives the same `CoreGame.Step` + real physics at fixed 50Hz.

## Design tree (decisions resolved)

1. **Overpressure: how much new state?**
   Options: (a) fold into existing health float, (b) separate `overPressure: float 0..1` parallel to health, (c) bespoke per-machine subsystems.
   **DECIDED: (b)** — one `overPressure[4]` array alongside `machineHealth`. One new mechanic, shared across all 4 machines, kept orthogonal to health so a machine can be healthy AND primed, or broken AND primed. Health = "will it produce"; overpressure = "will it blow on next use". No bespoke subsystems (YAGNI; friendslop).

2. **Per-kind state machine.**
   Options: discrete enum per machine vs. derive states from the two floats.
   **DECIDED: derive.** `machineHealth` maps to a 4-rung ladder: `HEALTHY (≥0.70)`, `WORN (0.35–0.70)`, `JAMMED/BROKEN (≤0.35, no production, wrench revives)`. Overpressure is a parallel 2-state: `SAFE (<1.0)` / `PRIMED (≥1.0)`. Crossing PRIMED is the visible "shake harder + steam wisps + bulge (squash-stretch, LOOK rule 7)" tell. No per-kind enums — the visual/ladder is uniform, payload differs. This matches "Grinder jams — wrench" (BROKEN = jammed = wrench).

3. **When does the geyser fire?**
   Options: on sabotage hit, on next-use, on a timer.
   **DECIDED: on next USE of a PRIMED machine.** The saboteur primes it; the next user (victim or anyone) triggers their own geyser by pulling from it. That is the prank: sabotage the machine someone's about to hit. Predictable, readable, and the victim is always the one comically buried — fun over fidelity. If a PRIMED machine breaks (health 0) unused, it just jams (no geyser); the trigger is a use, always.

4. **Geyser physics — cheapest funny.**
   Options: (a) GPU ParticleSystem beans (cheap, non-physical, no bounce-comedy), (b) pooled Rigidbody bean burst, (c) full rigidbody sim of the machine blowing up.
   **DECIDED: (b)** — pooled Rigidbody bean burst (≥12 beans) + a steam ParticleSystem + camera shake + hitstop + user stun. Beans already exist as pooled physics objects and ragdoll-comedy (jelly-bean, candy-glossy, LOOK rule 5); the bounce IS the joke, so they must be physical. Rigidbody pool reuses the existing pool discipline — zero per-frame allocation (perf rule). Steam is a GPU ParticleSystem (not a mesh). Payload per machine: Espresso=coffee beans, Grinder=grounds, IceMachine=sad ice cubes, SteamWand=steam only. One geyser code path, per-machine payload prefab slot.

5. **Steam scald (Steam Wand).**
   Options: sabotage fires an immediate scald burst vs. same prime-on-use pattern.
   **DECIDED: same prime-on-use pattern.** SABOTAGE on the Steam Wand primes it; next swing/use emits a scald burst — steam ParticleSystem + AoE radius that stuns ("OW") anyone in range and deals small health drain to adjacent machines (cascade seed). Uniform trigger = one rule to learn; scald is the weaponized reading of the same mechanic. This is the machine "weapon + tool" split in the spec.

6. **FIX/SABOTAGE interaction — can sabotage be repaired mid-cascade?**
   Options: (a) no — primed machines must blow, (b) yes — FIX bleeds overpressure, (c) FIX only revives broken, not primed.
   **DECIDED: (b), yes, mid-cascade.** FIX on a PRIMED machine vents overpressure to 0 with a hiss ("that was close" — Vinny line) and a small relief puff; FIX on a BROKEN machine revives it. This is the designed counterplay and the anti-grief rail: a victim/friend can wrench a shaking machine to defuse the prank, turning sabotage into a sprint (defuse before it blows) instead of a guaranteed hit. Keeps sabotage funny, never rageful (spec: losing is funny), and makes FIX genuinely matter mid-shift (artifact 2 gate: STEAL most-used; FIX must stay relevant).

7. **The cascade chain — how one event becomes a REPLAY moment.**
   Options: (a) new cross-machine physics/coupling, (b) reuse existing events as cascade seeds, (c) no cascade, single geyser.
   **DECIDED: (b), cheapest.** A cascade = ≥2 machine events within a 6s window. Seed mechanisms, all zero-new-physics: a geyser's flying beans and scald AoE deal a small health drain to adjacent machines; a geyser flings the victim into a neighbor; a broken machine stalls production (already present) which drives pressure/chaos. Nothing new to build — the chain emerges from existing degradation + the new AoE nudge. Reads as comedy chain (LOOK delivery axis), costs nothing extra.

8. **Sabotage-vs-grief rails — where do bean-sack and trip-wire live?**
   Options: here vs. Artifact 2.
   **DECIDED: Artifact 2 (out of scope here).** Bean-sack (blindfold a friend) and trip-wire (counter when inspector comes) are player/NPC-targeted griefing verbs, not machine mechanics — they belong to artifact 2 "five verbs + griefing". Artifact 4 owns ONLY machine sabotage: overpressure/geyser, steam scald, wrench repair. Keep the four machines the only sabotage surface. Explicit OUT.

9. **Cascade seed & determinism for replayability.**
   Options: wall-clock/Unity-nondeterministic vs. pure-seeded-event-driven.
   **DECIDED: pure seeded + event-driven.** All machine logic is a pure function of `(state, inputs, dt, seededRng)` — no wall-clock, no `Time.unscaledTime`, no nondeterministic physics reads feeding state. Geysers/scalds/breakdowns are emitted as deterministic events from the sim step (they are data, not side effects), so Artifact 8's REPLAY can re-simulate the same seeded window exactly. Seed = the per-shift `System.Random` CoreGame already owns. This is the single contract that makes the replay axis (and the gate) possible.

10. **Replay interface owed to Artifact 8 (build the contract, NOT replay).**
    Options: (a) build replay now, (b) record state frames, (c) expose a pure machine-event stream + determinism guarantee and stop.
    **DECIDED: (c) — contract only.** Artifact 4 ships a pooled, capped `MachineEvent` log: timestamped `(shiftTime, machineId, kind, fromState, toState)` for geyser / scald / breakdown / fix / prime transitions, plus the documented determinism guarantee (seeded pure function) and the fixed-tick step CoreGame already uses. Artifact 8 replays by re-simulating a captured (seed, inputs, fixed-step) window. Artifact 4 must NOT build playback, slow-mo, or recording UI — that is artifact 8's job. The event log is the seam.

11. **The ≥80% gate's measurable proxy.**
    Options: subjective "replay-worthy" vs. numeric /sim proxy.
    **DECIDED: numeric proxy in /sim.** Proxy = **cascade events per shift ≥ 1 in ≥80% of scripted seeded shifts**, where a cascade = one geyser OR scald OR breakdown that, within a 6s window, triggers a second machine event (or any geyser, which alone is the replay moment by definition — beans + stun + shake). /sim runs N seeded shifts (N≥40) across the 4 bots, reports `cascadesPerShift` and the ≥80% pass rate. This is measured, repeatable, and maps 1:1 to "a machine cascade produced a replay-worthy moment". Numbers gate first, RATER second.

12. **Self-teaching (blocker: "machines need docs").**
    Options: tutorial text vs. ambient visual language + Vinny nudge.
    **DECIDED: ambient + one-liner, no docs.** Degradation is visible (worn tint shift + wobble vibration, LOOK rules 6+7); primed machines visibly shake/bulge/wisp steam; FIX/SABOTAGE proximity verb already works on visible machines (position math). A single Vinny nudge fires on first prime/breakdown ("That steam wand looks wicked thirsty, kehd"). No instruction text, no docs. If a fresh player can't read "shaking machine = about to blow, wrench defuses it" from sight, that is a gate failure.

## Refined acceptance criteria (added to artifact 4's gate, in its own language)

- 4 machines each expose the derived 4-rung health ladder (HEALTHY/WORN/JAMMED/BROKEN) and a parallel PRIMED/SAFE overpressure state; all driven by existing `machineHealth` + new `overPressure[4]`.
- SABOTAGE raises overpressure; crossing 1.0 = PRIMED (visible shake + steam wisps + squash-stretch bulge). Next USE of a PRIMED machine fires a GEYSER.
- GEYSER = ≥12 pooled Rigidbody beans (or grounds / sad ice / steam payload per machine) + steam ParticleSystem + camera shake + hitstop + user stun. Replay-worthy by construction.
- Steam Wand SABOTAGE → PRIMED; next swing scalds (steam + AoE stun "OW" + adjacent-machine health drain).
- FIX vents PRIMED overpressure to 0 (hiss + relief puff) and revives BROKEN machines; repair is valid mid-cascade.
- Cascade: a geyser/scald/breakdown nudges adjacent-machine health; ≥2 machine events within 6s = one cascade event.
- All RNG seeded; machine logic is a pure function of (state, inputs, dt, seededRng); zero wall-clock/nondeterministic state reads.
- `MachineEvent` log pooled+capped, timestamped, emitted for geyser/scald/breakdown/fix/prime — the Artifact 8 contract. No playback built here.
- /sim: run ≥40 seeded shifts; report cascadesPerShift; pass rate ≥80% with ≥1 cascade.
- Zero per-frame allocation (pooled beans + pooled event log); 60fps 4-player budget holds.
- Self-teaching: first prime/breakdown triggers one Vinny nudge; no docs; sight-readable degradation.
- Blender art: any new visible mesh (geyser beans if no reusable bean, wrench, steam sprite) authored via the bpy pipeline, manifest-validated, ≤35,000-tri total cap (current 17,112 + delta), origins grounded y=0, per-submesh material indices.

## Blender art needs (via bpy pipeline, all trace to a named LOOK rule)

1. **Geyser beans** — candy-glossy jelly-bean (LOOK rule 5: chunky/rounded, candy accent). If a bean mesh is reusable from artifact 5 mule/van scope, author it here once and share; otherwise a single parameterized bean bpy script. Grounds/ice variants reuse the same jelly-bean shape with material-tint/payload (no extra meshes).
2. **Wrench (L)** — small candy-glossy tool prop for the FIX verb (visible wrench flash aids self-teaching). Minimal tri budget. (FIX is a proximity verb; the prop is a juice accent, not a required system.)
3. **Steam sprite** — a soft radial-gradient sprite texture authored in Blender bpy for the ParticleSystem (steam scald + geyser wisps + prime tell). A sprite, not a mesh; GPU particles carry the rest.
4. **Degradation states on the 4 machine rigs** — NO new mesh. Worn tint = code lerp of the existing candy-glossy machine material toward a grubby variant; vibration/bulge = scripted wobble + squash-stretch scale pulse (LOOK rules 6+7). Zero geometry added to the manifest.
- Explicitly NOT authored here: bean-sack, trip-wire (artifact 2), any new machine geometry.

## Dependencies + risks

- **Inputs (artifacts 1–3):** machines as real scene objects + production/degrade (artifact 1 repair); FIX/SABOTAGE/STEAL verb wiring + proximity targeting (artifact 2); stun/death/react (artifact 3) reused by geyser/scald stun. Ticket blocked by 03 — correct; geyser stun reuses artifact 3's stun path.
- **Downstream contract:** the `MachineEvent` log + determinism guarantee is consumed by Artifact 8 (REPLAY). Artifact 5 (mule/van) may reuse the bean asset.
- **External prereqs:** host Play Mode + in-editor /sim already authorized (DECISIONS). Unity license/WebGL module still human-pending — irrelevant to this artifact's EditMode tests + /sim; no batchmode build needed for the gate.
- **Risks:** (a) determinism — geysers must be data, not physics side effects, or Artifact 8 replay desyncs; mitigated by event-driven design and a /sim determinism check. (b) cascade rate — too rare fails the ≥80% gate, too common is annoying (artifact 12 balance); tuned via /sim `cascadesPerShift`, not intuition. (c) tri budget — 17,112 current + beans/wrench/sprite must stay well under 35,000; beans are cheap, tracked by manifest validator. (d) overpressure vs. health coupling — PRIMED must be readable independently of health so FIX has a distinct defuse action; verified by sight test. (e) perf — pooled beans + pooled event log must not allocate per frame; gated by the 60fps budget.
- **Assumption stated loudly:** "replay-worthy" is defined operationally as (any geyser) OR (≥2 machine events in 6s) — a proxy the RATER may challenge on feel, but it is the numeric contract the /sim harness can defend before expensive judgment.
