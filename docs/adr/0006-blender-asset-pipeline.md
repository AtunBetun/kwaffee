# ADR-0006: Blender asset pipeline — where sources and outputs live

## Status

Accepted.

## Context

All game geometry is authored in Blender 5.2.1 LTS via bpy scripts (ADR-0001: Models). The authorship is code, not manual modelling: every asset is parameterized and reproducible from a headless run. With parallel BUILDER sessions (ADR-0002, ADR-0005) the pipeline needs one canonical answer for where asset sources live, where their outputs land, and who may write where — otherwise two agents author to different paths and Unity imports drift.

The convention is already in use; this ADR records it as the decision so future sessions stop re-deriving it.

## Decision

- **Sources: `tools/kwafee/`.** Canonical, versioned bpy scripts, one per asset bundle, importing shared helpers from the same directory. The current single script `tools/kwafee/build_art.py` is the reference implementation.
- **Scratch: `.scratch/kwafee/` is NOT a source home.** `*-art.py` files there are one-off BUILDER experiments; anything that must survive is promoted into `tools/kwafee/` and the scratch copy deleted.
- **Outputs: `kwaffee/Assets/KwaFee/Art/`.** Every produced artifact of one asset bundle lands here, flat, named after the bundle:
  - `<bundle>.blend` — Blender project/source save.
  - `<bundle>.glb` — exported geometry, imported into Unity.
  - `<bundle>-meshes.json` — material + mesh manifest consumed by the Unity-side validator (12 required meshes, 35 000-triangle cap, grounded origins).
  - Preview renders go to `.scratch/kwafee/<name>-preview.png`, never into Assets.
- **Generated outputs are committed.** The `.blend`/`.glb`/manifest in Assets are the shipped pipeline state; a fresh checkout of `main` contains them and Unity imports from the committed tree. They are regenerable — re-run the owning script — which is exactly why the build scripts are the durable source.
- **Blender autosave backups (`*.blend1`) are never committed** (gitignored). They are stale snapshots of derived files, zero information beyond the `.blend` itself.
- **Unity `Assets/` is import-only for Blender output.** No manual `.blend`/`.glb` authoring inside the Unity project; the exporter is the only writer.
- **Export route:** Blender writes `.blend` + manifest; the repaired manifest exporter emits world-transformed, Unity-coordinate meshes with explicit per-submesh material indices and reversed winding. Unity imports the `.glb` + manifest. Re-export from Blender is expected whenever the script changes — never hand-edit the imported mesh.
- **Concurrency (from ADR-0005):** asset bundles own disjoint output paths; parallel runs are separate `blender -b -P` processes. The Blender MCP listener (127.0.0.1:9876) is one live scene — never shared between parallel agents.

## Consequences

- A new asset = new per-bundle script in `tools/kwafee/`, new output names under `kwaffee/Assets/KwaFee/Art/`, manifest validator updated to require the new names.
- Binary conflicts are avoided by disjoint ownership (ADR-0005); when they still collide, `.blend`/`.glb` do not merge — re-run the owning script.
- `.scratch/kwafee` stays a working area; promoted scripts are the durable record.

## Alternatives considered

- Authoring directly in Unity — rejected: bpy scripts are reproducible and diffable; manual editor sculpting is not (ADR-0001).
- Outputs under `.scratch/kwafee/` — rejected: Unity must import from inside its own project; scratch is not a real asset source and Unity would not see it.