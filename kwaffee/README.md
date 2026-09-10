# KWA FEE

A raucous online party game: 3-4 friends run a barely-legal Boston coffee shop,
flinging physics coffee, serving demanding customers, and wrecking each other
for per-player tips. Unity 6 + Blender 5.2, fully authored by script.

The current playable foundation is in `Assets/KwaFee/Scenes/CoreLoop.unity`.

## Editor setup

1. Open the `kwaffee` folder in Unity 6000.6.0f1.
2. If generated assets are missing, run `KWA FEE > Import Authored Art`.
3. Open `Assets/KwaFee/Scenes/CoreLoop.unity`.

The enabled build scene is already `CoreLoop.unity`. The scene contains
`CoreGame`, a main camera, and a directional light. `CoreGame` spawns the four
baristas, four machines, pooled cups, customer, counter, tray, sign, and walls
at runtime.

## Controls

`WASD` moves, the mouse aims, `E` grabs a cup off the rack (or a nearby cup),
`Q` steals a nearby carried cup, hold left click to charge and release to
fling, `SPACE` chugs (requires holding a cup of coffee; it drains the cup),
`L` fixes the nearest machine, `P` sabotages it, and `R` drops a carried cup.
The HUD shows charge, serves, tips, hits, machine health, chaos, and verb
counters. `FlingRules`, `VerbRules`, and `MachineRules` are covered by the
Editor-only test assembly under `Assets/KwaFee/Tests/EditMode`.

## Rebuilding art

Run from the repository root:

```sh
/opt/homebrew/bin/blender -b --factory-startup -P tools/kwafee/build_art.py -- --preview
```

The script writes `Assets/KwaFee/Art/core.blend`, `core.glb`, and
`core-meshes.json`, plus the preview at `.scratch/kwafee/art-preview.png`.
Import the manifest through `KWA FEE > Import Authored Art` after
regeneration. The manifest validator rejects missing/duplicate/unknown
required meshes.

## Deterministic simulation

`KwaFee.SimHarness` (`Assets/KwaFee/Runtime/SimHarness.cs`) drives the same
gameplay commands and physics as the player at a fixed 50 Hz (matching
`TimeManager`'s 0.02 s fixed step) and writes metrics to `sim-report.json`.
See `SimHarness.cs` for the run recipe; it reports chaos events/min, serves,
tips per player, hits, misses, chugs/overdoses, machine health and backlog,
and verb tallies. The `/sim` run and the player build remain gated on the
documented Unity license, WebGL module, and container/VM prerequisites; no
host Play Mode workaround is used.