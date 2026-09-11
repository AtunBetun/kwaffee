# Artifact 9 — Music & audio — Spec

Beads id: `kwaffee-tbw` (blocked by 08). Source of truth: `.scratch/kwafee/grilling/09-music-audio.md`. Lens: friendslop — cheapest version that is still funny; shrink over expand. Every decision below was grilled and is DECIDED; no human in the loop.

## Problem Statement (from the user's perspective)

I sit down at the laptop with three friends at 3am. The shop opens. Right now, the whole game is silent except whatever my friends and I yell — and a party game with no sound is a dead game. There is no soundtrack, no sting when the register goes cha-ching, no rattle when a machine is dying, no meow from the Shop Cat, no horn, no gasp when the geyser blows, no fanfare at Awards, no flourish at the Tabloid. I do not know a shift is heating up until a machine explodes in my face. And the next morning, when somebody asks "what did you play?", nobody can hum the song. I want the game to make noise — the right noise — so the chaos lands, the comedy lands, and the song sticks in my head.

## Solution (user perspective)

Every sound in KWA FEE is composed and synthesized in code at build time — zero downloaded audio, zero stock samples — then played through Unity's plain audio pipeline. One hummable 4-bar music loop (A minor, 96 BPM) carries every shift, and a heat layer adds driving drums and a tension line that fades in smoothly as the chaos meter rises — so the music heats up *felt, not noticed*. Every one-shot cue (register, meow, splash, rattle, sting, flourish, gulp, thud, whoosh) exists as 1–3 baked pitch/velocity variants so nothing repeats identically. Big Vinny's narration ducks the music and effects so his roast is heard. A WebGL-safe audio unlock happens on the first player input so the browser build is never silent. A machine-checked gate proves there is no silent state, no stock audio, no clicky loops, and the mix stays sane.

## User Stories

1. As a player, I want every action I take in the shop (fling, chug, fix, steal, sabotage, serve, horn) to make a sound, so that I always know my input registered and no moment in the game is dead air.
2. As a player, I want the music to change as the shift heats up, so that I can feel the tension rising without ever being told "the chaos meter is now 0.7".
3. As a player, I want to be able to hum the shift loop after one evening of play, so that the game has a hook that sticks with me and my friends.
4. As a player, I want the Shop Cat's meow to sound different each time — purr, alert mew, hiss — and never identical twice, so that the cat feels alive rather than like a repeating sound effect.
5. As a player, I want the machine death rattle to sound at least slightly different every time a machine dies, so that repeated failures stay funny instead of becoming a worn-out loop.
6. As a player, I want a miss on the shared Quota to sting with a proper impact sound, so that Big Tony taking a fixture lands as comedy, not rage.
7. As a player, I want the Catastrophe Replay to feel slow and ominous, so that the shift's best fail reads as a dramatic moment rather than a normal-speed rerun.
8. As a player, I want the Awards and the Tabloid to announce themselves with music, so that those moments feel like showtime and break up the shift rhythm.
9. As a player, I want Big Vinny's narration to be understandable, which means the music and effects duck out of the way when he talks, so that I never miss a roast.
10. As a player, I want the Mule drive to have its own engine bed, so that the bean run feels like driving, not like the shop loop playing in a van.
11. As a player, I want a mute toggle and per-bus volume control, so that I can quiet the game (or just the music) when we are talking over it.
12. As a player on the shared machine, I want the browser build to start making sound as soon as I press a key, so that the WebGL version is never mysteriously silent due to autoplay policy.
13. As the BUILDER, I want every cue to be a baked, inspectable WAV on disk, so that I can audition, tune, and debug any sound without touching runtime DSP code.
14. As the BUILDER, I want the entire audio build to be one stdlib-Python script with a fixed seed, so that builds are byte-reproducible and there are no external audio dependencies to install or license.
15. As the RATER, I want automated scans (event→cue coverage, silence/mix scan, no-stock-clip scan, heat and duck numeric checks) to be part of the artifact gate, so that "no silent state", "sane mix", and "no stock audio" are machine-checked, not promised.
16. As the ops layer, I want zero per-frame audio allocation and <1 ms/frame audio CPU at maximum chaos, so that the party game never stutters because of sound.

## Implementation Decisions

### Synthesis split — bake EVERYTHING to WAV at build time (deliberate deviation from spec literal)
Spec-literal read ("WAV-authoring script at build time + runtime synthesis") is overruled. **All** audio — music AND one-shots — is parametrically synthesized in a Python script at build time, rendered to WAV, and played by Unity's plain `AudioSource` pipeline. Runtime does NOT synthesize: it selects a baked variant, plays it, pitch-shifts via `AudioSource.pitch` (free, zero allocation), and ducks via AudioMixer (native, zero DSP code). Rationale: live `OnAudioFilterRead` is WebGL-uncertainty + GC-storm risk for zero audible payoff in a party game; baking keeps every cue an inspectable, tunable asset and is fully deterministic. "Synthesis" is preserved honestly — every sound is constructed from oscillators in code, just rendered offline. Mark this deviation from spec-literal in `/DECISIONS.md` with this rationale.

### Per-cue variation — baked variants + playback-rate pitch
Each one-shot gets 1–3 baked pitch/velocity variants at build time; runtime picks a variant and jitters `AudioSource.pitch` ±10%. No runtime DSP, no allocation, WebGL-safe, still organic. The meow family is parametric: FM carrier (~500–700 Hz) + modulator sweep for the "mew" contour + filtered breath noise, parameterized per cat state → **3 baked variants** — purr (low growl, 25 Hz AM), alert (rising mew), hiss (noise). All oscillator-constructed; no embedded samples.

### Music — one loop + one heat layer (single key/tempo; CUT per-shift-seed music)
Per-shift-seed melodies are CUT. One fixed hummable 4-bar loop (C/A minor, 96 BPM) for ALL music so stings resolve in-key and never clash; one heat layer at the same tempo/key adding driving percussion, a counter-line, brighter timbres, and a tension ostinato. Freshness comes from chaos-driven heat + cue placement, not new melodies.
- **Heat layer:** crossfaded by the chaos meter (0..1) with ≥2 s slew. "Felt, not noticed": the loop always stays the melody carrier; heat never exceeds loop loudness (hard mix ceiling); heat is never the only thing audible (it layers, never replaces); normal chaos ≠ sudden loud — smooth ramp only.
- **Round phases:** no per-phase music tracks. Open/Replay/Awards/Tabloid reuse loop+heat. Catastrophe Replay re-pitches the heat layer down + low-pass via mixer EQ (a mixer treatment, not a new asset). Awards/Tabloid get short `award_fanfare` / `tabloid_flourish` one-shots over the ducked loop.

### Spatialization — flat 2D
No positional audio. One room, one mostly-fixed camera, four-player chaos — positional tuning cost, near-zero comedy value. Skipped. Only the Mule drive uses a looping engine bed, still non-positional.

### Pipeline — `build_audio.py` mirrors `build_art.py`
Standalone stdlib Python (NOT routed through Blender/bpy, per ticket rejection). `python3 tools/kwafee/build_audio.py` runs at build time; stdlib only (`wave`, `array`, `math`) — no numpy. Deterministic fixed seed → byte-identical WAVs (hash-gated). Emits WAVs + `audio-manifest.json` into `kwaffee/Assets/KwaFee/Audio/`. Editor menu `[MenuItem("KWA FEE/Import Authored Audio")]` (mirrors `Import Authored Art`) reads the manifest and applies import settings: all clips **Decompress-On-Load**, uncompressed PCM (WebGL cannot stream-decompress); `loop=true` on loop/heat; normalized loudness; wires an `AudioCueLibrary` ScriptableObject.

### Architecture — one AudioManager seam on GameEvents
One `AudioManager` singleton sits behind a central `GameEvents` bus (mirrors the Shift-seam philosophy). AudioManager subscribes to the bus and maps event→cue via one lookup table (the 30-cue table below). No gameplay component touches audio directly.
- **Pool:** 8 one-shot `AudioSource`s (round-robin; lowest-priority steal when full) + 2 music sources (loop, heat). Pre-created; zero per-frame allocation.
- **Mixer:** 3 buses — `Music`, `SFX`, `Voice` (Vinny). Ducking via AudioMixerSnapshot crossfade (Ducked ↔ Normal), NOT sidechain — simpler + deterministic. Duck SFX+heat ≥6 dB under Voice on narration start (250 ms in), restore 500 ms out. Heat lives in the Music bus, crossfaded by chaos.
- **Mute toggle:** one global AudioManager mute + per-bus volumes (spec-required).
- **WebGL unlock:** audio unlocked on first player input (Enter/click), per browser autoplay policy — required acceptance, else WebGL ships silent.

### Cue → event table (the trigger mapping)
Boundary with Artifact 8: **Artifact 8 (juice) OWNS event definitions and emits the cinematic moments (catastrophe replay, awards, tabloid, upgrades, Vinny narration start/stop). Artifact 9 consumes those events.** One-directional: 8 declares, 9 maps to sound. No audio logic in 8, no event creation in 9. 1:1 event→cue lookup; 30 cues total. This full set IS the "no silent state" floor — every action of every verb + every round phase has a non-silent sound:

| Cue | Variants | Trigger event | Owner |
|---|---|---|---|
| `shift_loop` (loop) | 1 | continuous, shift active | Shift |
| `heat_layer` (loop) | 1 | chaos meter 0..1 | Shift |
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
| `dealer_sting` | 1 | DealerArrive | Mule |
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
| `inspector_sting` | 1 | InspectorSuspicion | NPC |
| `mule_engine_loop` (loop) | 1 | MuleActive | Mule |
| `ma_paddy_chase` | 1 | MaPaddyChase | Mule |

CUT: per-shift-seed music, per-phase tracks, positional audio, sidechain, reverb buses, and a separate `cat_station_cover` cue (cat reuses `meow`). KEEP: the full spec'd list + core-loop floor above — these ARE the no-silent-state minimum, not gold-plating.

### External dependency — the Shift seam (do NOT absorb)
The Shift seam (`kwaffee-6id` / "shift-seam", a separate refactor owning `Step`/`Command`/`Metrics` in architecture-spec.md) is the event source of truth and the cross-artifact contract for event names. It is an EXTERNAL dependency of this artifact, NOT absorbed here. If the seam has not landed by the time Artifact 9 builds, AudioManager falls back to hooking existing MonoBehaviours' events — but that is coupling; the bus is the required path. The `GameEvents` bus itself is owned by Artifact 8.

### Hardware/real-world note
Neither audible nor visual hardware exists here — audio is pure synthesized code paths. The one real-world constraint honored is the browser: WebGL cannot stream-decompress (hence Decompress-On-Load + uncompressed PCM) and blocks autoplay (hence unlock-on-input). No calibration knobs needed beyond the build-time constants above.

## Testing Decisions

Good test = machine-verifiable evidence for the artifact gate. All proxies run headless; no RATER spend for measurement.

- **Event↔cue coverage (EditMode test):** every `GameEvent` enum value maps to ≥1 non-silent cue, and every cue is reachable from ≥1 event (no orphans). Fails on any silent event or dead cue. Also assert every round phase (Lobby/Open/Shift/Replay/Awards/Tabloid) has ≥1 non-silent cue.
- **Heat + duck controllers (EditMode test):** chaos=1 heat adds ≤ +6 dB to Music RMS over chaos=0, never exceeds loop loudness; crossfade slew ≥2 s with no step >1 dB/100 ms; ducking budget ≥6 dB in 250 ms, restored within 500 ms of narration end.
- **WAV silence/mix scan (Python `--check` mode in `build_audio.py`, mirrors the art manifest validator):** required-name list enforced; each WAV non-zero RMS above floor (no accidental silent render); loop seams continuous (first/last-sample amplitude within 0.001 after crossfade — kills the loop click); no clipping (peak ≤ −1 dBFS); no DC offset > 0.001; loudness bands (Music −14 ±2 LUFS, SFX bed −18 ±2 LUFS, Voice most prominent); deterministic hash (same seed → byte-identical).
- **No-stock-audio scan (Editor):** every AudioClip in the project resolves to a file listed in `audio-manifest.json`; build script asserts zero external file references. The "no stock audio" blocker is automatic.
- **Performance (Profiler in authorized Play Mode):** zero per-frame allocations in the audio path; audio CPU < 1 ms/frame at max chaos (one-shot storm with all 8 pool slots cycling). Prior art: 21 EditMode tests green, SimHarness exists.
- **Human judgment (before RATER, per artifact-8 convention):** 1 card question — "music annoying?" / "noticed the heat layer?" — plus sim-captured numbers. RATER capped at 2 reviews per artifact.

## Out of Scope

- Per-shift-seed melodies and per-round-phase music tracks (CUT — one loop + heat is the soundtrack).
- Positional/3D audio, sidechain ducking, reverb/send buses.
- Runtime audio synthesis of any kind (no `OnAudioFilterRead` DSP); runtime is selection + pitch + duck only.
- A separate cat-station-cover cue (cat reuses `meow`).
- Any Blender/bpy involvement in audio; any visible art authored outside the standing bpy rule — see note below.
- Owning/defining `GameEvents` or cinematic triggers (Artifact 8's boundary; Artifact 9 consumes only).
- The Shift seam refactor itself (external dependency, see Implementation Decisions).
- Voice lines / TTS content generation (Artifact 6/8's domain; this artifact supplies the Voice bus, ducking, and stings).
- Installing system Python or Unity's WebGL module in build environments (human/platform prerequisite, tracked in DECISIONS).

## Further Notes

- **Blender art needs: NONE — explicit.** Per the standing art rule ("any work needing **visible** art must be authored via the Blender bpy pipeline"), this artifact's visible-asset count is zero: audio is pure code (Python WAV synthesis) + Unity mixer. The bpy-pipeline acceptance clause does not apply: no Blender assets, no `.glb`, no manifest, no triangle budget. Stated explicitly to close the art-rule audit.
- **Deviation-from-spec ledger entry:** bake-everything over spec-literal runtime synthesis, with rationale (WebGL/GC safety, determinism, tunability), goes in `/DECISIONS.md`.
- **Assumption stated loudly:** system `python3` available in the build env, stdlib only; if the build container lacks it, install that prerequisite before this artifact's build step runs.
- **13 remaining cues from the spec-literal list** are all present in the 30-cue table (spec list + core-loop minimum additions like `fling_whoosh`, `cup_land_thud`, `chug_gulp`, `grab_pluck`, `fix_tick`, `respawn_pop`).
- **Determinism across platforms:** fixed sample rate + fixed-point int math + fixed seed; hash gate item 1.
- **Risks tracked:** WebGL silent audio (gated by import-settings scan + unlock-on-input), loop seam click (gated by continuity check), heat overpowering comedy (bounded delta + human card), mixer ducking latency (250 ms budget), judgment-heavy mix feel (sim numbers + 1 human card before RATER spend).

## Refined Gate

Measurable additions to the Artifact 9 gate (gate: no silent state; mix sane; shift-heat layer felt, not noticed; blockers: stock audio, music fighting comedy):

1. All 30 cues in the table exist as manifest-validated baked WAVs; build is byte-reproducible (same seed → identical hash).
2. **No-silent coverage:** every `GameEvent` enum value and every round phase (Lobby/Open/Shift/Replay/Awards/Tabloid) has ≥1 non-silent cue — EditMode coverage test passes.
3. **Mix sanity scan passes:** all cues in loudness band (Music −14 ±2 LUFS, SFX −18 ±2), peak ≤ −1 dBFS, DC ≤ 0.001, loop seams continuous (first/last within 0.001).
4. **No stock audio:** zero project AudioClips outside `audio-manifest.json` (Editor scan).
5. **Heat felt not noticed:** chaos=1 heat adds ≤ +6 dB over chaos=0 Music RMS and never exceeds loop; crossfade slew ≥2 s, max step 1 dB/100 ms; human card reports heat not annoying.
6. **Ducking:** Vinny narration start ducks SFX+heat ≥6 dB within 250 ms; restore within 500 ms of end.
7. **WebGL-safe:** all clips Decompress-On-Load, uncompressed PCM, no streaming; audio unlocked on first player input.
8. **Performance:** zero per-frame allocations in audio path (pool 8 one-shots + 2 music sources); audio CPU < 1 ms/frame at max chaos, measured via Profiler in authorized Play Mode.
9. **Meow is synthesis, not sample:** meow/hiss/purr built from oscillators+noise in `build_audio.py` (no embedded audio), 3 baked variants.