# Artifact 9 — Music & audio (Original music and audio)

- Beads id: `kwaffee-tbw` (blocked by 08)
- Grilled scope: the entire code-synthesis stack — loop + heat + every cue, the synth/build pipeline, the audio architecture, the cue→event table, and the "no silent state / sane mix / heat felt not noticed" gate.

Lens: friendslop (cheapest version that is still fun; jank that reads funny is fine; shrink over expand). All audio code-synthesized, zero external assets. Every decision below is DECIDED — no human in the loop.

---

## Design tree (decisions resolved)

### 1. Synthesis split: offline WAV bake vs runtime DSP
Spec literally says "WAV-authoring script at build time + runtime synthesis." Grilled and overruled the literal reading.

- **Option A (spec-literal):** music baked at build, one-shots synthesized live in Unity via `OnAudioFilterRead`.
- **Option B (bake everything):** music AND all one-shots are parametric-synthesized in a Python script at build time, written to WAV, then played by Unity's plain AudioSource pipeline.
- **Option C:** music baked, one-shots live DSP with per-frame allocation risk.

**DECIDED: B — bake EVERYTHING to WAV at build time.** Runtime does NOT synthesize; it selects a baked variant, plays, and pitch-shifts via `AudioSource.pitch` (free, zero allocation) and ducks via AudioMixer (native, zero DSP code). Rationale: live `OnAudioFilterRead` is a WebGL-uncertainty + GC-storm risk for zero audible payoff in a party game; baking keeps every cue an inspectable, tunable asset and is fully deterministic. This preserves "synthesis" honestly — every sound is constructed from oscillators in code, just rendered offline. Runtime = selection, not synthesis. Marking this as a deliberate deviation-from-spec with the above rationale.

### 2. Per-cue variation (the meow problem)
Needed: meows that don't repeat identically, rattle that tracks machine health, etc.

- **Option A:** live parametric generation per instance.
- **Option B:** bake 2–4 pitch/velocity variants per one-shot at build time; runtime picks variant + jitters `AudioSource.pitch` ±10%.

**DECIDED: B.** Baked variants + free playback-rate pitch. No runtime DSP, no allocation, WebGL-safe, still organic-sounding. Meow = FM carrier (~500–700 Hz) + modulator sweep for the "mew" contour + filtered breath noise; parameterized per cat state (purr = low growl w/ 25 Hz AM, alert = rising mew, hiss = noise) → 3 baked variants. All oscillator-constructed, no samples.

### 3. Music: two-tier soundtrack shape
- **Option A:** per-shift-seed melodies (ticket asked explicitly).
- **Option B:** one fixed hummable loop + one heat layer, same key/tempo for every shift.

**DECIDED: B.** One loop, one heat layer, single key + tempo (~C/A minor, 96 BPM, 4-bar loop) for ALL music so stings resolve in-key and never clash. **CUT per-shift-seed music** — multiplies asset count and tune risk for zero party payoff; one hummable hook is the comedy glue (players hum it after one evening). Freshness comes from chaos-driven heat + cue placement, not new melodies.

- **Heat layer mechanics (DECIDED):** same tempo/key; adds driving percussion, a counter-line, brighter timbres, a tension ostinato. Crossfaded by the chaos meter (0..1) with a ≥2 s slew. **"Felt, not noticed" rules:** the loop always stays the melody carrier; heat never exceeds loop loudness (hard mix ceiling, see gate); heat is never the only thing audible (it layers, never replaces). Normal chaos ≠ sudden loud — smooth ramp.
- **Round phases (DECIDED):** no per-phase music tracks (Open/Replay/Awards/Tabloid reuse the loop+heat). Catastrophe Replay re-pitches the heat layer down + low-pass via mixer EQ for the slow-mo vibe — a mixer treatment, not a new asset. Awards/Tabloid get the short `award_fanfare` / `tabloid_flourish` one-shots over the ducked loop.

### 4. Spatialization
- **Option A:** full 3D positional audio per source.
- **Option B:** flat 2D mixing.

**DECIDED: B — flat 2D, no positional audio.** One room, one mostly-fixed camera, four-player chaos — positional tuning cost, near-zero comedy value. Skip. (Only the Mule drive uses a looping engine bed, still non-positional.)

### 5. Pipeline: where the synth script lives
- **Option A:** route through Blender/bpy (rejected in ticket: NOT Blender).
- **Option B:** standalone stdlib Python checked in beside the art pipeline.

**DECIDED: B.** New `tools/kwafee/build_audio.py` — mirrors `tools/kwafee/build_art.py` convention exactly. Run at build time (`python3 tools/kwafee/build_audio.py`), stdlib-only (`wave`, `array`, `math`) — no numpy. Deterministic fixed seed → byte-identical WAVs (hash gate). Emits WAVs + `audio-manifest.json` into `kwaffee/Assets/KwaFee/Audio/`. Editor menu `[MenuItem("KWA FEE/Import Authored Audio")]` (mirrors `Import Authored Art`) reads the manifest, applies import settings (all clips **Decompress-On-Load**, uncompressed PCM — WebGL cannot stream-decompress; loop=true on loop/heat), normalizes loudness, and wires an `AudioCueLibrary` ScriptableObject.

### 6. Audio architecture: one seam + pooling
- **Option A:** 30 call sites across MonoBehaviours each triggering clips.
- **Option B:** one `AudioManager` singleton behind a central event bus.

**DECIDED: B — one `AudioManager` seam.** Mirrors the Shift-seam philosophy (architecture-spec.md): AudioManager subscribes to a central `GameEvents` bus and maps event→cue via one lookup table. No gameplay component touches audio directly. Structure:
- **Pool:** 8 one-shot AudioSources (round-robin, lowest-priority steal when full) + 2 music sources (loop, heat). Pre-created, zero per-frame allocation.
- **Mixer:** 3 buses — `Music`, `SFX`, `Voice` (Vinny). Ducking via AudioMixerSnapshot crossfade (Ducked ↔ Normal), not sidechain — simpler + deterministic. Duck SFX+heat ≥6 dB under Voice on narration start (250 ms in), restore (500 ms out). Heat lives in Music bus, crossfaded by chaos.
- **Mute toggle** (spec-required): one global AudioManager mute + per-bus volumes.

### 7. Cue → event table (the trigger mapping)
Boundary with artifact 8: **Artifact 8 (juice) OWNS event definitions + emits the cinematic moments (catastrophe replay, awards, tabloid, upgrades, Vinny narration start/stop). Artifact 9 consumes those events.** One-directional: 8 declares, 9 maps to sound. No audio logic in 8, no event creation in 9.

Spec'd cues + core-loop minimum (the "no silent state" floor — every action of every verb + every round phase has a non-silent sound):

| Cue | Variants | Trigger event | Owner |
|---|---|---|---|
| `shift_loop` (loop) | 1 | continuous, shift active | 1-8 Shift |
| `heat_layer` (loop) | 1 | chaos meter 0..1 | 1-8 Shift |
| `register_cha_ching` | 3 | ServeCredited (tip) | Shift |
| `serve_ding` | 2 | ServeLanded | Shift |
| `machine_death_rattle` | 2 | MachineBroken | Shift/Machine |
| `meow` | 3 | CatPurr/CatMew/CatHiss | Cat |
| `splash` | 2 | CupSpill | Cup |
| `burn` | 2 | Burn | Shift |
| `horn` | 1 | HornPressed (V) | Input |
| `crowd_gasp` | 1 | CatastropheDetected | 8 |
| `vinny_sting` | 1 | VinnyNarrateStart | 8 |
| `tabloid_flourish` | 1 | TabloidShow | 8 |
| `upgrade_jingle` | 1 | UpgradeBought | 8/UI |
| `dealer_sting` | 1 | DealerArrive | 5 Mule |
| `fling_whoosh` | 2 | FlingReleased | Shift |
| `cup_land_thud` | 2 | CupLanded (comic squash thud) | Cup |
| `chug_gulp` | 2 | Chug | Shift |
| `grab_pluck` | 2 | Grab/Steal/Drop | Shift |
| `fix_tick` | 1 | Fix | Shift |
| `geyser_boom` | 1 | Geyser (overpressure) | Shift/Machine |
| `overdose_wobble` | 1 | Overdose (JITTERS) | Shift |
| `respawn_pop` | 1 | Respawn | Shift |
| `order_spawn_chime` | 1 | OrderSpawned | Shift |
| `order_warn` | 1 | OrderExpiring (patience low) | Shift |
| `big_tony_impact` | 1 | FixtureTaken | Shift |
| `quota_save_flourish` | 1 | QuotaSaved | Shift |
| `award_fanfare` | 1 | AwardsShow | 8 |
| `inspector_sting` | 1 | InspectorSuspicion | 6 NPC |
| `mule_engine_loop` (loop) | 1 | MuleActive | 5 |
| `ma_paddy_chase` | 1 | MaPaddyChase | 5 |

Cut/keep fights: **CUT** per-shift-seed music, per-phase tracks, positional audio, sidechain, reverb buses, a separate `cat_station_cover` cue (cat reuses `meow`). **KEEP** the full spec'd list + the core-loop floor above — these ARE the no-silent-state minimum, not gold-plating.

### 8. How "no silent state" is gated automatically
Three automated proxies (all runnable headless, no RATER spend):
- **Event↔cue coverage (EditMode test):** every `GameEvent` enum value maps to ≥1 non-silent cue, and every cue is reachable from ≥1 event (no orphans). Fails on any silent event or dead cue.
- **WAV silence/mix scan (Python `--check` mode in build_audio.py, mirrors the art manifest validator):** required-name list enforced; each WAV non-zero RMS above floor (no accidental silent render); loop seams continuous (first/last-sample amplitude within 0.001 after crossfade — kills the loop click); no clipping (peak ≤ −1 dBFS); no DC offset > 0.001; loudness band (Music −14 ±2 LUFS, SFX bed −18 ±2 LUFS, Voice most prominent); deterministic hash (same seed → byte-identical).
- **No-stock-audio scan (Editor):** every AudioClip in the project must resolve to a file listed in `audio-manifest.json`; build script asserts zero external file references. The "no stock audio" blocker, made automatic.

### 9. How "mix sane / heat felt not noticed" is gated
- Numeric: heat hard ceiling (chaos=1 adds ≤ +6 dB to Music RMS, never exceeds loop loudness); heat crossfade slew ≥2 s, no step >1 dB/100 ms; ducking budget (≥6 dB in 250 ms, restore 500 ms). EditMode test on the heat + duck controllers.
- Judgment: 1 human playtest card question ("music annoying?" / "noticed the heat layer?") before RATER, per artifact-8 human-test convention.

---

## Refined acceptance criteria (additions to Artifact 9 gate, numeric)

1. All 12 spec'd cues + the core-loop minimum set (full table above) exist as manifest-validated baked WAVs; build is byte-reproducible (same seed → identical hash).
2. **No-silent coverage:** every `GameEvent` enum value and every round phase (Lobby/Open/Shift/Replay/Awards/Tabloid) has ≥1 non-silent cue — EditMode coverage test passes.
3. **Mix sanity scan passes:** all cues in loudness band (−14 ±2 LUFS music, −18 ±2 SFX), peak ≤ −1 dBFS, DC ≤ 0.001, loop seams continuous (first/last within 0.001).
4. **No stock audio:** zero project AudioClips outside `audio-manifest.json` (Editor scan) — the "stock audio" blocker is machine-checked.
5. **Heat felt not noticed:** chaos=1 heat adds ≤ +6 dB over chaos=0 Music RMS and never exceeds loop; crossfade slew ≥2 s, max step 1 dB/100 ms; human card reports heat not annoying.
6. **Ducking:** Vinny narration start ducks SFX+heat ≥6 dB within 250 ms, restores within 500 ms of end.
7. **WebGL-safe:** all clips Decompress-On-Load, uncompressed PCM, no streaming; audio unlocked on first player input (browser autoplay policy handled — an audio unlock on first Enter/click is a required acceptance, else WebGL ships silent).
8. **Performance:** zero per-frame allocations in audio path (pool of 8 one-shots + 2 music sources); audio CPU < 1 ms/frame at max chaos (one-shot storm) measured via Profiler in authorized Play Mode.
9. **Meow is synthesis, not sample:** meow/hiss/purr built from oscillators+noise in build_audio.py (no embedded audio), 3 baked variants.

## Blender art needs: NONE

Per the user's standing art rule — "any work needing **visible** art must be authored via the Blender bpy pipeline." This artifact's visible-asset count is **zero**: audio is pure code (Python WAV synthesis) + Unity mixer. Therefore the bpy-pipeline acceptance clause **does not apply** here; there are no Blender assets, no .glb, no manifest, no triangle budget. Stated explicitly to close the art-rule audit.

## Dependencies + risks

**Dependencies (inputs):**
- **Artifact 8 (juice) — hard boundary:** owns the `GameEvents` bus, the catastrophic-replay/awards/tabloid/upgrade triggers, and Vinny narration start/stop. Artifact 9 consumes these; blocked on 8 delivering them (ticket is already `Blocked by: 08`).
- **Shift seam (architecture-spec.md):** the event source of truth (`Step`/`Command`/`Metrics`). Event names are the cross-artifact contract. If the seam hasn't landed, AudioManager falls back to hooking existing MonoBehaviours' events — but that is coupling; the bus is the required path.
- **Artifacts 1-6** raise the gameplay events (serve, fling, chug, fix, steal, sabotage, machine, mule, NPC inspector).
- **System Python 3** at build time, stdlib only (wave/array/math). No numpy, no external packages.
- **Unity AudioMixer** + WebGL audio unlock path.

**Risks:**
- **WebGL silent audio** — the classic party-game failure: wrong import settings (streaming/compressed) or autoplay policy → silent build ships. Mitigated by gate items 7 + 4 (import-settings + clip-origin scans + unlock-on-input).
- **Loop seam click** — exact-length bars + zero-crossing crossfade; gated by item 3.
- **Heat overpowering comedy** — bounded mix delta (item 5) + human card.
- **Mixer ducking latency** — snapshot blend time budgeted at 250 ms (item 6).
- **Determinism across platforms** — fixed sample rate + fixed-point int math + fixed seed; hash gate (item 1).
- **Judgment-heavy mix feel** — get sim-captured numbers + 1 human card before RATER spend (RATER capped at 2 reviews/artifact per DECISIONS).
- **Assumption stated loudly:** system python3 available in the build env; if the build container lacks it, that is a prerequisite to install before this artifact's build step runs.
