# Artifact 8 — Juice & Delivery Axis — Spec

## Problem Statement (from the user's perspective)

You and three friends just survived a shift: a bean geyser painted the counter, someone stole someone's tray mid-serve, the quota was saved at the last breath. Then… nothing. The round just stops and a score screen appears. The biggest fail was never shown, never named, never roasted. There is no hitstop, no slow-mo, no confetti, no wobble — the physics are funny but the game does not sell the funny. The round ends in silence and the shop reopens in silence. Nobody laughs at what happened because the game never pointed at it. There is no reason to say "again."

## Solution (user perspective)

Every shift ends by becoming a comedy tape and a ceremony. Automatic slow-mo replay of the shift's single best catastrophe, played on the same camera that lived it. Five awards (Top Tipper / Biggest Culprit / Coffee Junkie / Customer Wrecker / Theft of the Night) roasted off your real shift numbers. A candy tabloid whose headlines name you and your actual crimes. A 15-second absurd upgrades shop whose five purchases change only props and never the sim. Big Vinny narrates every single segment — Boston, deadpan, disappointed — via TTS-when-available with a styled text-banner + sting fallback and a mute toggle, so no beat is ever silent. Moments get weight through hitstop, slow-mo, camera shake, confetti, and squash on every land. The whole round is glued together by one owning seam (`JuiceController`) so the juice never fights itself and never allocates a byte per frame. The loop proves the comedy: two recorded real-friend sessions of the scripted catastrophe + replay, raw recordings and filled feedback cards in `/EVIDENCE.md` — the model never claims anyone laughed.

## User Stories

1. As a player, I want the shift's single biggest catastrophe replayed automatically in slow-mo at the end of the shift, so that I relive and re-laugh at the best fail with my friends without anyone deciding anything.
2. As a player, I want the replay to show the exact same physics, positions, and timing as the live moment, so that what I rewatch is what actually happened, not a reenactment.
3. As a player, I want the game to pick the biggest moment by one objective score, so that nobody argues and nobody has to choose.
4. As a player, I want EVERY shift to produce exactly one replay, so that the ceremony never skips a round, even a boring one (the closing bookend is itself the joke then).
5. As a player, I want hitstop on big impacts, so that every cup slam and body hit lands like a hit.
6. As a player, I want slow-mo on catches, splats, and deaths, so that the funniest instant is stretched to be seen.
7. As a player, I want camera shake on bean geysers, so that the shop physically reels from the sabotage.
8. As a player, I want confetti when the quota is saved, so that a saved shift feels like a victory we earned.
9. As a player, I want every land to squash, so that the clay reads comedic deformation, never breakage.
10. As a player, I want the Awards screen to roast my real shift numbers (tips, grief, chugs, customers wrecked, thefts), so that the ceremony is about me and my friends, not stock silliness.
11. As a player, I want exactly the five named awards, each backed by a real stat, so that the ceremony is complete but never padded.
12. As a player, I want a candy tabloid whose headlines name real players with real counts, so that everyone at the table gets called out for what they actually did.
13. As a player, I want the upgrades shop to sell five pure-joke purchases that change visible props and nothing else, so that I buy the gag for its gag and never worry the sim skewed.
14. As a player, I want "Unlock The Shop Cat" delivered as the "it was always there" joke, so that the purchase itself is the punchline.
15. As a player, I want Big Tony to sit in The Recliner when he repossesses it, so that losing a fixture is a sight gag, not a rage moment.
16. As a player, I want Big Vinny to narrate every segment with a banner + sting that I can mute, so that the room is never silent and never overruled.
17. As the host, I want a one-click "Catastrophe Theatre" scene (scripted bean geyser + chain-drop combo + auto-replay) in a dedicated loop-friendly build and in the regular playtest build, so that a friend session can be recorded easily.
18. As the host, I want to record two real-friend sessions and drop the raw files + two filled 10-question cards into `/EVIDENCE.md`, so that the laugh gate passes on human evidence, provably.
19. As the host, I want the model's only claim to be "recording file exists with non-trivial duration + card is filled", so that the loop can never fabricate a laugh.
20. As a developer, I want replay determinism proven in the `/sim` harness (same seed + inputs ⇒ same catastrophe score and replay window), so that the Replay is guaranteed identical by construction, not by hope.

## Implementation Decisions

### 1. Replay model — seed + inputs re-sim (DECIDED)
Replay is re-simulation, never frame recording. The simulator is already deterministic and seeded (Artifact 1: `CoreGame.Step` at a fixed tick, seeded `System.Random`), so a replay re-runs the SAME scene graph and physics from the shift-start snapshot while feeding the recorded input timeline through the same fixed-step loop. Renders the replay window in slow-mo. One bounded input ring buffer per shift (capacity = max shift duration, reused every shift) holds the full input timeline; capturing is a few bytes per tick. Zero per-frame transform recording, zero GC from capture or playback. Replays are LOCAL ONLY — same build, same machine, in-memory, post-shift — so cross-platform float drift never matters; replay files are never saved, never shared, never transferred. Playback never writes authoritative sim state and never runs during live multiplayer stepping (re-checked when Artifact 10 lands). The re-sim steps the full shift headlessly from the start snapshot (cheap: fixed-tick numeric loop, nothing presented); only the replay window is presented to the camera at slow-mo — the pre-window portion is stepped without presentation, so "zero per-frame transform recording" still holds. If Unity physics ever proves touchy on re-sim, the fallback is a sparse key-event log + local snapshot — recorded as a fallback, not the plan.

Contract: `ReplayCapture` receives one input sample per sim tick (append-only ring buffer), closes at shift end; `ReplaySim` restores the shift-start snapshot and replays inputs through `CoreGame.Step`; the renderer consumes only the window tick range at the slow-mo factor granted by `JuiceController`. Determinism is an artifact-1 property; this artifact only preserves and proves it.

### 2. Biggest-moment ranking — scored event log (DECIDED)
Every replayable event type registers a scored entry the moment it fires. Entry schema: `{ eventType, magnitude, occurredAtTick }`. Score per entry: `catastropheScore = weight(eventType) × magnitude`, where magnitude is a per-type numeric (victims hit, tips lost/stolen, quota proximity at close, simultaneous deaths, cups/orders lost). Replayable types: bean geyser, chain-drop, van incident (The Mule), perfect theft, overdose collapse, steam scald, Big Tony fixture loss. Weights, magnitude semantics, and the window parameter `N` (default `N` seconds before event start, ~4 seconds after event end) live in one tunable, seeded data table. At shift end the highest score wins; ties go to the LATER event (party pacing, freshest memory). The log only records; the table only scores; nothing special-cases a type in code.

Fallback so every shift yields exactly one replay [DECIDED in synthesis]: when the scored log is empty (a clean-but-uneventful shift), the replay window is the final `N` seconds of the shift (the closing/quota reveal, Big Tony's entrance if quota failed), scored at floor 0 with a Vinny bookend line. Cheapest replay that is still funny, and it rides the same re-sim machinery.

### 3. Event ingestion — consume existing event contracts (DECIDED)
The scored log is a subscriber/consumer of the existing `MachineEvent` contract (Artifact 4: geyser, steam scald, chain-drop, overpressure) and the `GameEvents` contract (Artifact 6: verb events for steal/grief, deaths, per-shift reactions). No new event bus, no parallel event system — the scoring listener reads the same streams the machines and NPCs already emit, attaches the per-type magnitude, and appends to the log. Van incidents and Ma Paddy chase (Artifact 5) and overdose collapse/respawn (Artifact 3) subscribe the same way. This honors the prohibition on a second convention and keeps one causality source.

### 4. One `JuiceController` seam (DECIDED)
A single `JuiceController` owns ALL `Time.timeScale` writes, the one gameplay camera's shake (amplitude-limited, time-decayed, max-clamped), and a pooled confetti/particle emitter set. Systems fire juice events — `Burst(impact)`, `SlowMo(death)`, `Shake(geyser)`, `Confetti(quotaSave)`, `Squash(land)` — and the controller applies them with one priority policy: hitstop interrupts slow-mo briefly, shake clamps to a max amplitude and decays, confetti is fire-and-forget pooled, squash routes into the shared rig deformation language (Artifact 7 rig convention — geometry language, not time-scale). NO other code writes time scale or the gameplay camera transform — enforced, per the refined gate, by a grep-able ownership check and a test-level guard. Two distinct systems share the one choke: LIVE micro-juice (sub-second, in-shift: hitstop on impacts, slow-mo on catches/splats/deaths, shake on geysers, squash on every land, confetti on quota save) and the post-shift REPLAY slow-mo (full re-sim presented at reduced time-scale). Both route every time-scale change through `JuiceController`, so competing slow-mo writers (the classic jitter bug) are structurally impossible. The camera is the single gameplay camera; no per-player cameras, no split-screen, no mid-replay cuts beyond the replay's own framing.

### 5. One data-driven line table asset (DECIDED)
ALL tabloid headlines, award roasts, Vinny narration lines, and upgrade purchase lines live in ONE line-table asset (scriptable table / JSON), loaded once. Every entry is a parameterized template keyed by context and stats: `{playerName}` `{count}` `{stat}` selected against the real shift stats; selection = templates whose keys match reality, picked by seed. Zero runtime LLM; copy iteration never touches code; the entire Boston voice pass is ONE file sweep (the refined gate's "any line that could be from any city = blocker" is checkable in one asset). This EXTENDS the existing Vinny trigger-table pattern from Artifact 6 — same convention, one more table — not a second mechanism.

### 6. Upgrades shop — all 5, all cosmetic (DECIDED)
All five purchases ship: Double-Speed Conveyer, The Recliner, Pigeon-Powered Van Turbo, The Liability Bell, "Unlock The Shop Cat". Every purchase is PURE JOKE: exactly one visible prop change + one Vinny line + one tabloid headline, all entries in the line-table asset. One click, no currency, no cost economy. "Unlock The Shop Cat" reuses the existing Shop Cat (Artifacts 6/7) and delivers "it was always there" — the line is the mechanic. Big Tony sits in The Recliner when he repossesses it — the sight gag is the whole mechanic. Purchases toggle cosmetic dressing flags applying to the FOLLOWING shift (van turbo decal on The Mule, recliner in the shop, bell on the counter, conveyer sticker), cleared when the next shop opens; zero persistence across sessions; zero effect on sim numbers (proven in `/sim` with purchases toggled — criterion 9). Mechanical upgrades are explicitly deferred to Artifact 12; adding them here would drag sim rework into a juice artifact and violate friendslop's one-way-to-do-a-thing.

### 7. Round segment sequence — no silent state (DECIDED)
Post-shift flow: `Replay → Awards → Tabloid + Upgrades shop → close/next shift`, with quota-save treated as its own segment. EVERY segment opens with a Big Vinny narration beat: TTS when available, styled text-banner + sting fallback, mute toggle. Segments never sit silently; any segment without a banner + sting fails the gate. Tabloid headlines react to real stats (real names, real counts from the line table); Awards are exactly the 5 fixed awards, each stat-derived with a table roast from the same asset.

### 8. Human-laugh gate — the model cannot self-certify (DECIDED)
The BUILDER builds the theatre; the human produces the recording. A "Catastrophe Theatre" scene — one-click play of a scripted catastrophe (bean geyser + chain-drop combo) followed by its auto-replay — ships in a dedicated loop-friendly build AND the regular playtest build. The OWNER (human) invites one real friend per session to play/watch; the session is captured as a screen+audio recording with a system recorder (host-authorized, same authorization as Play Mode), recording pre-announced with consent. Evidence = the raw recording file sitting in the repo (path + timestamp + who) PLUS the completed 10-question feedback card, both referenced from `/EVIDENCE.md`. The model's assertion is limited to "recording file exists with non-trivial duration + card is filled" — a laugh claim is NEVER made from its own run. The gate requires TWO recorded friend sessions (the spec's test… re-test), both real, distinct sessions, both recorded. No recording + no card = gate NOT passed, no RATER pass claimed. Explicitly out: any AI/LLM-synthesized laugh, simulated-human feedback, or model-logged laugh claims.

### 9. Audio ownership boundary (DECIDED)
Artifact 8 must not ship silent segments waiting on Artifact 9's cues. Until 9 lands, the sting/flourish/confetti cues are the small code-synth WAV-at-build-time fallback OWNED by this artifact, wired to the same cue names 9 will own; when 9 lands it replaces the fallback behind the same calls. Mute toggle governs all of it.

### 10. Art pipeline for this artifact (DECIDED)
Blender (bpy, headless `blender -b -P`, parameterized, .glb export, manifest-validated: required-name list, ≤35,000 total triangles, origins grounded y=0, per-submesh material indices): new meshes = `Confetti` (pooled candy-gloss quad/tile, few triangles, warm candy palette, glossy plastic), `Podium` (candy awards podium, chunky rounded, 3-material palette), `Recliner` (clay+candy recliner for the Big Tony gag), `Bell` (candy-gloss counter bell, upgrade prop), `PigeonTurbo` (pigeon + van decal; may extend the Artifact 5 `Van` script, which also carries the dent-grows-every-shift rule). Shop Cat and Double-Speed Conveyer need NO new mesh — the cat is reused with a joke line; the conveyer is existing counter/cup meshes plus a sticker/SVG. Tabloid, headlines, awards cards, and shop UI are code/SVG ONLY — no Blender. Total new mesh budget: 4 small props + confetti, far inside the 35,000-triangle cap; all checked against Artifact 7's 8 LOOK rules before Artifact 8 closes.

## Testing Decisions

A good test here proves one of: determinism, ownership of the juice seam, zero allocation, data-table integrity, or stat-veracity. The `/sim` harness is the workhorse (SimHarness exists; `/sim` number reporting lands with prior artifacts and is consumed here).

1. **Replay determinism (gate criterion 1, the blocker test).** In `/sim`: run a scripted shift, capture the input timeline + top scored event, then re-sim from the shift-start snapshot with the same seed and inputs. Assert same catastrophe score and same replay window (within epsilon), IDENTICAL event tick. This is the regression guard that Artifact 1's determinism stays intact; goes in the same suite as Artifact 1's determinism tests. Prior art: Artifact 1's fixed-step/seeded-RNG tests.
2. **Scoring unit tests.** `weight × magnitude` math per type, magnitude extraction from real events, tie→later-event rule, seeded line-table pick stability, and the empty-log fallback producing the close-of-shift replay (criterion 2: exactly one replay per shift, never zero).
3. **Upgrades zero-effect (gate criterion 9).** `/sim` runs the same scripted shift with and without every purchase toggled; assert sim numbers (scores, timings, chaos stats) unchanged. Mechanical = blocker, proven by numbers.
4. **One replay per shift (gate criterion 2).** Every simulated shift yields exactly one replay selection, including clean-shift fallback.
5. **Ownership + zero-allocation (gate criteria 3 and 4).** Two guards: a grep-able static check that no code outside `JuiceController` writes `Time.timeScale` or the gameplay camera transform (freestanding `TimeScale` writes = blocker), and a `/sim`/profiler allocation check proving `JuiceController`, the replay path, and the confetti pool allocate ZERO per frame (pooled everything). Performance regressions are blockers (spec: 60fps at 4 players).
6. **Line-table integrity.** Every template compiles: all `{placeholders}` resolve from real stat keys, all keys reference live stats. The fixed set of 5 awards is asserted, each backed by a real stat source (criterion 8). The Boston voice pass (criterion 7) is a single-file review sweep of the line table — all lines in one asset makes "no line that could be from any city" mechanically reviewable in one pass, per the Artifact 6 trigger-table convention.
7. **No silent state (gate criterion 6).** Segment-sequence test asserts every segment transition fires its Vinny banner + sting (TTS-or-fallback) before advancing; silent segment = blocker.
8. **Squash on every land (gate criterion 12).** Cross-read with Artifact 7's visual identity pass (rig deformation language reviewed per rigged body) — a review-level check, done in the 7 cross-read, not a unit test.
9. **The laugh gate is NOT model-testable.** The only harness concern is that the Catastrophe Theatre scene runs and produces its replay; the gate itself is human evidence — two raw recordings + two filled 10-question cards cited in `/EVIDENCE.md`. The model never asserts the laugh.

Prior art: Artifact 1's determinism suite (replay rides it), Artifact 4's ≥80%-replay-worthy-moments measurement (the events 8 consumes and must not re-test), Artifact 2's stats harness (STEAL-most-used feeds awards), Artifact 6's trigger-table tests (extended by the line table, not re-invented).

## Out of Scope

- Any frame recording, persistent replay files, replay sharing, or cross-platform replay reproduction (local-only re-sim only).
- Manual replay selection, multi-event compilations, replays of non-catastrophe moments, re-roll/checkpoint systems (audit rule: recording only, never a checkpoint).
- Mechanical upgrades, any upgrade that changes sim numbers, shop currency systems, persistent loadouts, more than the 5 purchases.
- Per-player cameras, split-screen logic, camera cuts beyond the replay's own framing, any second camera owner.
- Runtime LLM line generation; copy in scene-serialized strings or per-screen hardcoded arrays.
- AI/LLM-synthesized laughs, simulated-human feedback, or any model self-certification of the laugh gate.
- Any authoritative-state writes or live-stepping participation for replays (Artifact 10's domain; cross-checked when it lands).
- Balance/pacing tuning of upgrade effects (Artifact 12's domain).

## Further Notes

- **Tick rate reconciliation:** the memo records the deterministic fixed step at 50 Hz (referencing Artifact 1's decisions), while the main spec's architecture line says 30 Hz sim tick. The replay rides WHATEVER `CoreGame.Step`'s canonical fixed tick is — reconcile the stated rate with Artifact 1 when it lands; this artifact adds no second clock. Determinism (same build + machine + fixed step ⇒ same trajectory) is the contract, not the specific rate.
- **External dependency check:** the memo references no pre-existing `kwaffee-6id`/"shift-seam" ticket, so no separate refactor is absorbed or depended on here; the `JuiceController` seam decision in this spec is this artifact's own surface.
- **Determinism fallback:** if Unity physics proves touchy on re-sim, the fallback is the sparse key-event log + local snapshot (option c from the grill) — recorded as fallback, not plan.
- **Loudest risk:** the laugh gate is the one hard human dependency; the model cannot schedule or fabricate it. If no friend session happens, Artifact 8 cannot close, but everything else is buildable now. Recording requires consent and real humans; recordings are gate evidence per the spec's human-test clause.
- **Party pacing:** tie→later-event and the 6s-before/4s-after window default favor the freshest, most readable moment; all tunables live in the seeded table, not code.
- **Mechanical upgrades are an Artifact 12 decision**, explicitly not an 8 expansion; this spec's shop is provably cosmetic via the `/sim` toggle test.
- **Deterministic everything:** seeds, weights, line-table picks all feed from the same seeded RNG so the replay, scoring, and ceremony are all reproducible and testable.
- The van's dent grows every shift (spec FEEL) is retained; the Pigeon Turbo extends the Artifact 5 van script rather than adding a meshed-only prop where the script already lives.

## Refined Gate

Measurable definition of done for Artifact 8 (all must hold; RATER-verified, evidence in `/EVIDENCE.md`):

1. **Replay determinism:** `/sim` proves same seed + input timeline ⇒ same catastrophe score and same replay window (within epsilon) — re-sim reproduces the top-scored event identically to the live run. Fails = blocker.
2. **Biggest-moment metric:** every shift yields exactly ONE replay (empty-log fallback defined and tested); `catastropheScore` computed, logged, and picked by table weights.
3. **One juice seam:** grep-able proof that NO code outside `JuiceController` writes `Time.timeScale` or the gameplay camera transform; freestanding time-scale writes = blocker.
4. **Zero per-frame allocation:** `JuiceController`, replay path, and confetti pool allocate zero per frame (pooled; `/sim` or profiler evidence). Perf regression = blocker (60fps, 4 players).
5. **Laugh gate (the real gate):** two recorded real-friend sessions, each showing the scripted catastrophe + replay eliciting an audible laugh; raw recording files + two completed 10-question cards on disk in the repo and cited in `/EVIDENCE.md`. The model never self-certifies. No recording + no card = no pass claimed.
6. **No silent state:** every round segment (replay, awards, tabloid, shop, quota save) has a Vinny banner + sting (TTS-when-available, styled text-banner + sting fallback, mute toggle). Any silent segment = blocker.
7. **Boston voice pass:** every line in the line-table asset is Boston-voice (kehd, wicked, pahk the cah…); any line that could be from any city = blocker. Applied wholesale across awards, tabloid, and Vinny.
8. **Awards:** exactly 5 (Top Tipper / Biggest Culprit / Coffee Junkie / Customer Wrecker / Theft of the Night), every one stat-derived from real shift numbers, each with a table roast.
9. **Upgrades:** all 5 present, each provably zero-effect on sim rules (cosmetic prop + line only) — `/sim` numbers unchanged with purchases toggled.
10. **Tabloid reacts to real stats:** headlines reference actual shift data (real player names, real counts); fabricated or static-generic copy fails the gate.
11. **Blender art acceptance:** every 3D asset (Confetti, Podium, Recliner, Bell, PigeonTurbo/Van-ext) authored via the bpy pipeline (headless `blender -b -P`, parameterized, .glb, manifest-validated: required-name list, ≤35,000 total triangles, origins grounded y=0, per-submesh material indices); 2D/UI (tabloid, awards cards, shop UI, icons) code/SVG only.
12. **Squash on every land:** land squash present on all rigged bodies, verified in the Artifact 7 visual-identity cross-read.