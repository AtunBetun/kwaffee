# ADR-0001: Unity as engine + headless authoritative server

## Status

Accepted.

## Context

KWA FEE must be a physics-comedy multiplayer party game deliverable as a browsable WebGL build, with a dedicated server authoritative over the simulation to prevent desync. Cheaper alternatives (Three.js + a JS physics engine) would have meant the server and client simulating different physics worlds — the classic desync pile-up this game specifically cannot afford.

## Decision

- Engine: **Unity 6 (6000.6.0f1), pinned LTS**, building both WebGL (browser party) and desktop (LAN party) from one gameplay codebase.
- Server: **a headless Unity build running the same scene graph and physics as the client** — no desync by construction. Fixed 30Hz simulation tick; clients send inputs, render interpolated 60fps state; own-input prediction + reconciliation only.
- Models: **Blender 5.2.1 LTS, bpy (Blender Python) scripts**, exporting .glb into Unity's pipeline. Every asset parameterized and reproducible.
- All music/sound: code-synthesized (WAV-authoring script at build time + runtime synthesis).

## Consequences

- Strongest: shared physics world server/client, no desync.
- Costs: Unity licensing gates every batchmode build until a human activates a Personal license in Hub; Unity's batching + compilation are slower than a pure-JS runtime. Acceptable for a party game.
- Risk: `com.unity.editor.headless` entitlement missing until licensing is fixed; WebGL Build Support module not yet installed.

## Alternatives considered

- Three.js + full physics on client, authoritative by server-side re-sim — rejected: physics drift/jitter without shared engine.
- Pure client-auth with good luck — rejected: griefing physics failures and tip theft are the game; trust breaks them.
- Godot headless — viable; not chosen because Unity Hub/license/toolchain is already present on the machine.