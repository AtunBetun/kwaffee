# KWA FEE core art builder

Generator: `tools/kwafee/build_art.py` (Blender 5.2.1, factory startup, standard library + `bpy`).

Generated outputs:

- `kwaffee/Assets/KwaFee/Art/core.blend`
- `kwaffee/Assets/KwaFee/Art/core.glb`
- `kwaffee/Assets/KwaFee/Art/core-meshes.json`
- `.scratch/kwafee/art-preview.png`

Manifest schema is `{materials, meshes}`. Materials contain `name`, RGBA `color`, and `roughness`. Each mesh contains `name`, flattened Unity-coordinate `vertices` and `normals` (`x,z,y` from Blender `x,y,z`), per-face `submeshes` with global material indices and reversed triangle winding, plus converted `bounds.min` and `bounds.max`.

Assets and measured geometry:

| Asset | Vertices | Triangles |
|---|---:|---:|
| Cup | 4,008 | 1,336 |
| Barista | 14,328 | 4,776 |
| Customer | 15,348 | 5,116 |
| Counter | 564 | 188 |
| Floor | 564 | 188 |
| Wall | 564 | 188 |
| Tray | 564 | 188 |
| Sign | 1,332 | 444 |
| Total | 37,272 | 12,424 |

Source test: `blender -b --factory-startup -P tools/kwafee/build_art.py -- --preview` completed successfully, exported both Blender and GLB files, wrote the JSON manifest, and rendered the 640x480 EEVEE preview. Dimensions follow the request: Counter 3x1x1, Wall 16x.25x4, Floor 16x12 with top at Blender Z=0, Cup .4 high, humanoids 1.8 high, Tray 1.4x.8x.1. Sign is a 2.9x.68 board with raised KWA FEE lettering and a slight tilt. The manifest validator reports 8 meshes, 11 materials, finite bounds, and consistent triangle indices/counts. The measured totals are 12,424 triangles, below the 35,000-triangle artifact cap.

SHA-256 (2026-09-10): `core.blend` `7154838732d042490e74da277df394f5bf74840fc81ed60f6da150dc539dfa41`; `core.glb` `4a331fd3b408ae4712541cf5d9cf392829167e7c0785347369f68e0b11773814`; `core-meshes.json` `0a37d1094ec64eacc37c8faaf71565073657f88db122f285a7931e706e334209`.

The preview pass stages the authored objects into a readable contact-sheet tableau while leaving exported local-origin geometry unchanged.
