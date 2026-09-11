# Artifact 8 — Juice & Delivery Axis

- **Beads id:** kwaffee-8iu (blocked by 07)
- **Grilled scope:** hitstop, slow-mo, shake, confetti, CATASTROPHE REPLAY, Vinny narration, awards, tabloid + upgrades shop, Boston voice pass, human-laugh gate.
- **Lens:** friendslop — cheapest thing that is genuinely funny for 3-4 friends; replay-and-roast is the glue; jank that reads funny wins; shrink surfaces, keep one way to do a thing.

---

## Design tree (decisions resolved)

### 1. Replay capture model
- Question: how does the CATASTROPHE REPLAY capture the biggest moment? Options: (a) record full frame states (transforms/velocities per frame), (b) record seed + input stream and re-simulate deterministically, (c) record sparse key-event log + resim from snapshot.
- **DECIDED: (b) — seed + inputs, re-sim.** The sim is already deterministic and seeded (DECISIONS: CoreGame.Step at fixed 50 Hz, seeded System.Random). Replay = re-run the SAME scene graph and physics from the shift-start snapshot with the recorded input timeline, rendered in slow-mo. Cost: one small input ring buffer, zero per-frame transform recording, zero GC. Rationale: cheapest deterministic option, and determinism is free — we already own it. Replays are LOCAL ONLY (same build, same machine, in-memory, post-shift), so WebGL/desktop float drift never matters; replays are never saved or transferred. Anything else (frame recording) is strict waste.
- Explicitly OUT: persistent replay files, replay sharing, cross-platform replay reproduction, frame recording.

### 2. What is "the biggest moment" (ranking metric)
- Question: how does the shift pick which catastrophe to replay? Options: (a) dev-pinned scripted moments, (b) a scored event log, (c) human picks.
- **DECIDED: (b) — scored event log.** Every replayable event type registers a scored entry the moment it fires: bean geyser, chain-drop, van incident (mule), perfect theft, overdose collapse, steam scald, Big Tony fixture loss. Each event computes `catastropheScore = Σ eventTypeWeight × magnitude` where magnitude is a per-type numeric (e.g. victims hit, tips lost/stolen, quota proximity at close, simultaneous deaths). Weights live in one data table (tunable, seeded). At shift end, highest score wins; tie → later event wins (party pacing, freshest memory). Replay window = `N` seconds before event start through event end; N in table, default 6s before, ~4s after. Rationale: single metric, no special-casing per event type, feeds straight from the /sim harness which already counts events. The replay always replays *some* moment (fallback: chain-drop or biggest tip-loss event) — no empty replay state.
- Explicitly OUT: manual replay selection, multi-event compilations, replay of non-catastrophe moments.

### 3. Juice engine central choke
- Question: where do hitstop, slow-mo, camera shake, and confetti live? Options: (a) scattered in whatever system triggers them, (b) one JuiceController seam.
- **DECIDED: (b) — ONE JuiceController.** A single MonoBehaviour owning ALL time-scale writes (`Time.timeScale`), one camera shake pool, and pooled confetti/particle emitters. Any system fires a juice event (`JuiceEvent.Burst(impact)`, `SlowMo(death)`, `Shake(geyser)`, `Confetti(quotaSave)`) into the controller; the controller prioritizes (hitstop interrupts slow-mo briefly, shake max-clamps, confetti is fire-and-forget pooled). Nobody else touches time scale or the camera transform. Rationale: competing time-scale writers are the classic juice bug (two slow-mos fighting = jitter); one seam is testable, swap-able, and keeps the no-per-frame-allocation rule enforced in one place.
- Two distinct systems, one choke: LIVE micro-juice (hitstop on impacts, slow-mo on catches/splats/deaths, shake on geysers, squash on every land — sub-second, in-shift) and the post-shift REPLAY (full slow-mo re-sim). Both route time-scale changes through JuiceController.

### 4. Upgrades shop scope — all 5 ship, ALL cosmetic
- Question: which of the 5 upgrades are mechanical vs cosmetic? Mechanical upgrades re-tune gameplay → artifact 12 balance work.
- **DECIDED: all 5 ship as PURE COSMETIC/JOKE purchases — zero mechanical, zero numeric effect.** Double-Speed Conveyer, The Recliner, Pigeon-Powered Van Turbo, The Liability Bell, "Unlock The Shop Cat" each change one visible prop + one Vinny line + one tabloid headline. "Unlock The Shop Cat" is delivered as the joke: "it was always there" (cat was already on screen). Rationale: the 15s tabloid+shop interlude is a COMEDY BEAT, not a balance system; mechanical upgrades would drag artifact 12's sim work into a juice artifact and violate friendslop (one way to do a thing). Big Tony sits in The Recliner when he repossesses it — that's the whole mechanic, and it's the punchline. If art 12 later wants real mechanical upgrades, that is a NEW decision, explicitly out of scope here.
- Explicitly OUT: any upgrade that changes sim numbers, shop currency system, persistent loadouts across shifts, more than these 5.

### 5. Human-laugh gate logistics (anti-fabrication)
- Question: how does the loop get evidence of ONE real friend laughing, without the model fabricating it?
- **DECIDED: the model BUILDS the theatre, the human produces the recording — the loop cannot self-certify.** Concretely:
  - Builder ships a "Catastrophe Theatre" scene: one-click play of a scripted catastrophe (bean geyser + chain-drop combo) followed by its auto-replay, on a loop-friendly dedicated build plus the regular playtest build.
  - The OWNER (human) invites ONE real friend to play/watch. The session is captured as a screen+audio recording (system recorder, host authorized — same authorization grant as Play Mode).
  - Evidence of the laugh = the raw recording file sitting in the repo (path + timestamp + who) PLUS the completed 10-question feedback card, both referenced from /EVIDENCE.md. The model asserts only "recording file exists with non-trivial duration + card is filled" — NEVER "the friend laughed".
  - The ticket's "re-test" = the gate needs TWO recorded friend sessions (initial test + one retest), per the spec's "test ... re-test". Both must be real, distinct humans-adjacent sessions, both recorded.
  - The recording happens with consent; the friend is told they are being recorded for a playtest (they are reading a line? No — they play/watch; recording for evidence is pre-announced).
- Explicitly OUT: any AI/LLM-synthesized laugh, simulated-human feedback standing in for the friend, the model logging a laugh claim from its own run.

### 6. Where tabloid + Vinny copy lives
- Question: hardcoded UI strings vs data-driven line tables?
- **DECIDED: one data-driven line table asset** (a scriptable table / JSON, loaded once, seeded RNG pick). Every tabloid headline, award roast, and Vinny line is a parameterized template in the table keyed by stats: `{playerName}` `{count}` `{stat}`; selection = templates matching the REAL shift stats, pick by seed. Rationale: single asset = one Boston voice pass covers everything (the gate's "any non-Boston line" blocker checked in one file), copy iteration never touches code, zero runtime LLM. This extends the existing Vinny trigger-table pattern from artifact 6 rather than inventing a second convention (spec: reuse existing patterns, prohibition on second convention).
- Explicitly OUT: copy in scene serialized strings, per-screen hardcoded arrays, any LLM call for line generation.

### 7. Shake/camera authority
- Question: who owns the camera?
- **DECIDED: JuiceController owns the single gameplay camera.** Shake requests are amplitude-limited and decayed by the controller; no other system writes `Camera` transform. (Artifact 10 netcode may later own a reconcile path — that is a 10 concern, not this one.)
- OUT: per-player cameras, split-screen logic, camera cuts mid-replay beyond the replay's own framing.

---

## Refined acceptance criteria (additions to the artifact's gate)

1. **Replay determinism:** replay re-sim reproduces the top-scored event identically to the live run (position/score within epsilon) in the /sim harness: same seed + inputs ⇒ same catastrophe score, same replay window. Fails = blocker.
2. **Biggest-moment metric:** every shift yields exactly ONE replay; no shift ends without a replay (fallback defined). `catastropheScore` computed, logged, and picked by table weights.
3. **One juice seam:** grep-able proof (code review) that NO code outside `JuiceController` writes `Time.timeScale` or the gameplay camera transform. Freestanding `TimeScale` writes = blocker.
4. **No per-frame allocation:** JuiceController + replay path + confetti pool allocate zero per frame (pooled; /sim or profiler evidence). Perf regression = blocker (spec: 60fps 4-player).
5. **Laugh gate (the real gate):** two recorded real-friend sessions each showing the scripted catastrophe + replay eliciting an audible laugh, raw recording files + completed 10-question cards on disk in repo and cited in /EVIDENCE.md. Model never self-certifies. No recording + no card = gate NOT passed, no RATER pass claimed.
6. **No silent state:** every round segment (replay, awards, tabloid, shop, quota save) has a Vinny banner + sting (TTS when available, styled text-banner + sting fallback, mute toggle). Any silent segment = blocker.
7. **Boston voice pass:** every line in the line-table asset is Boston-voice (kehd, wicked, pahk the cah...); any line that could be from any city = blocker. Applied wholesale across awards, tabloid, Vinny.
8. **Awards:** exactly 5 awards (Top Tipper / Biggest Culprit / Coffee Junkie / Customer Wrecker / Theft of the Night), every one stat-derived from real shift numbers, each with a table roast.
9. **Upgrades:** all 5 present, each provably zero-effect on sim rules (cosmetic prop + line only) — verified by the /sim harness numbers being unchanged with purchases toggled.
10. **Tabloid reacts to real stats:** headlines reference actual shift data (real player name, real counts); fabricated/static-generic copy fails the gate.
11. **Blender art acceptance:** every 3D asset below is authored via the Blender bpy pipeline (headless `blender -b -P`, parameterized, .glb export, manifest-validated: required-name list, ≤35,000 total triangles, origins grounded y=0, per-submesh material indices). 2D/UI (tabloid, awards cards, shop UI, icons, neon sign) in code/SVG — no Blender.
12. **Squash on every land** (spec FEEL): land squash present on all rigged bodies; checked in the visual identity pass cross-read with artifact 7.

## Blender art needs

- **Confetti mesh** — one small pooled candy-gloss quad/tile mesh (few triangles, instanced in pool), candy-saturated warm palette (Rule 4), glossy plastic (Rule 3). Manifest name `Confetti`.
- **Awards podium** — candy podium prop (Rule 8: candy podium), chunky rounded (Rule 5), 3 materials palette. Manifest name `Podium`.
- **The Recliner** — clay+candy recliner prop (Big Tony sits in it / repossession gag). Manifest name `Recliner`.
- **Liability Bell** — small candy-gloss counter bell (upgrade prop). Manifest name `Bell`.
- **Pigeon-Powered Van Turbo prop** — small pigeon + van decal/attachment for the mule van (artifact 5's van script extended, dent growth per spec "van's dent grows every shift"). Manifest name `PigeonTurbo` (or extend `Van`).
- **Shop Cat / Double-Speed Conveyer** — the cat already exists (artifact 6/7); "Unlock The Shop Cat" reuses it with a joke line, no new mesh. Conveyer upgrade = existing counter/cup meshes + a sticker/SVG, no new mesh.
- Tabloid, headlines, awards cards, shop UI: **code/SVG only — none**.

Total new mesh budget: 4 small props + confetti; well inside the 35,000-triangle cap.

## Dependencies + risks

**Dependencies (inputs from prior artifacts):**
- Artifact 1: deterministic `CoreGame.Step` at fixed tick + seeded RNG — replay re-sim rides on this; if 1's determinism regresses, replay breaks.
- Artifact 2: verb events (steal, grief) feed the catastrophe score log; STEAL-most-used stats feed awards.
- Artifact 3: overdose collapse + respawn are replayable death events; Coffee Junkie stat.
- Artifact 4: geyser/steam/chain-drop events — artifact 4's own gate already requires replay-worthy moments ≥80% of the time; artifact 8 consumes those events.
- Artifact 5: van incidents, Ma Paddy chase, pigeon — replay events + van dent growth; PigeonTurbo reuses the mule van script.
- Artifact 6: Big Vinny trigger table, gossip/tabloid subplots, per-shift reaction lines — the line-table asset here extends it (no second convention).
- Artifact 7: all props/screens must pass the 8 LOOK rules; new meshes (confetti, podium, recliner, bell) and the tabloid/awards screens are checked under artifact 7's rules before 8 closes.
- Artifact 9 (audio): juice needs sting/flourish/confetti cues; the no-silent-state criterion cross-references 9's cue list — 8 must not ship silent stings relying on unbuilt audio; use code-synth sting fallback owned by 8 until 9 lands.

**Risks / loud assumptions:**
- **Physics determinism on re-sim:** assumption: same build + same machine + same fixed step ⇒ same trajectory; verified by criterion 1 in /sim. If Unity physics proves touchy on re-sim, fallback is (c) key-event log + local snapshot — but that is a fallback, not the plan.
- **Replay vs netcode (artifact 10):** replay is local + post-shift by design; it MUST never write authoritative sim state or run during live multiplayer stepping. Cross-checked when 10 lands.
- **Laugh gate is the one hard human dependency:** the model cannot schedule or fabricate it. If the owner has no friend session, artifact 8 cannot close; everything else is buildable now. Loudest risk on this ticket.
- **Recording consent/privacy:** friend must know the session is recorded; recording stored in repo as gate evidence per spec's human-test clause.
- **Perf:** slow-mo + confetti + shake + replay all at once must hold 60fps at 4 players; pooled everything, verified by criterion 4. Regressions block.
- **Balance:** mechanical upgrades explicitly deferred to artifact 12; artifact 8's shop is provably cosmetic (criterion 9). If someone later wants mechanical upgrades, that is a 12 decision, not an 8 expansion.
- **No re-roll/checkpoint:** the replay is the only "relive" — recording only, per spec weird rules. Confirmed held.