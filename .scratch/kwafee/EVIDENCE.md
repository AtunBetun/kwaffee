# KWA FEE — Evidence log

Each RATER pass appends here: artifact, pass, scores, findings, fixes, status. Grows monotonically and truthfully. No entries yet — build has not started.

## Gates

- Artifact gate: 2 consecutive passes ≥8/10 on target dimensions, 0 blockers, 0 majors, min 3 passes.
- Full-session gate: 3 consecutive full-session passes ≥8/10 on Fun, Feel, Coherence, Boston Voice, NPC Aliveness, Visual Identity.
- Human ladder: fresh player URL → laughing in 60s, zero instruction.

## 2026-09-09 — Artifact 1 setup, no RATER pass

- Read PROMPT.md in full and required context/decision/evidence files. Created issues/01-core-loop.md and core-builder-request.md with the authorized first-artifact scope.
- Unity MCP project/info returned `/Users/albertodesaintmalo/work/kwaffee/kwaffee`, Unity 6000.6.0f1, StandaloneOSX. editor/state returned idle, SampleScene, not playing, no test job. No host gameplay execution occurred.
- Read-only module listing returned only MacStandaloneSupport. No WebGL module. License entitlement was not inspected or retried.
- Docker daemon reports 29.7.2. Installed image inventory has no Unity image. Container/VM Unity execution is not yet available through a verified setup.
- Configured cheap model probe through omp completed with BUILDER_READY. Initial sandbox attempt failed because omp's startup database was read-only; the tool-free probe succeeded with reviewed execution access.
- Automatic approval review REJECTED the actual builder invocation before execution: private project prompt/context/decisions/issue data would be sent to OpenRouter without the explicit payload authorization the reviewer requires. No bypass attempted. User authorization is needed to resume that invocation.
- Implementation: not started. Generated assets: none. /sim metrics: not run. Tests/builds: not run. Self-review: not run. RATER scores: not assigned. Artifact gate: pending, not passed.

## 2026-09-09 — MCP connectivity verification

- Unity: native MCP project/info and editor/state calls succeeded; active root `/Users/albertodesaintmalo/work/kwaffee/kwaffee`, 6000.6.0f1, SampleScene, idle, ready_for_tools true. Custom-tools inventory retrieved successfully.
- Blender: initial Codex configuration inspection contained Unity only, while repository .mcp.json contained both servers. Existing Blender process was listening on 127.0.0.1:9876.
- Added global Codex Blender registration with pinned cached blender-mcp 1.9.1 and telemetry disabled. CLI returned `Added global MCP server 'blender'.`
- End-to-end local stdio MCP verification exited 0: initialization succeeded, tools/list returned 28 tools, get_scene_info returned isError false and Scene with Cube, Light, Camera (3 objects, 2 materials). Server handshake reported add-on protocol 5 current and Blender 5.2.1 LTS. Log confirmed telemetry disabled. No scene mutation, render, game build, or model-provider call occurred.

## 2026-09-10 — Core loop artifact and editor verification

- Blender artifact build completed with `/opt/homebrew/bin/blender -b --factory-startup -P tools/kwafee/build_art.py -- --preview` (exit 0). It produced `kwaffee/Assets/KwaFee/Art/core.blend`, `core.glb`, `core-meshes.json`, and `.scratch/kwafee/art-preview.png`. The manifest validator found 8 required meshes, 11 materials, finite vertices/normals/bounds, and consistent triangle indices/counts. Measured geometry totals 12,424 triangles, below the 35,000-triangle cap.
- Unity MCP `KWA FEE/Import Authored Art` completed successfully and generated 8 prefabs, 8 meshes, and 11 URP/Lit materials under `kwaffee/Assets/KwaFee/Generated`. `KWA FEE/Create Core Loop Scene` completed successfully and saved `kwaffee/Assets/KwaFee/Scenes/CoreLoop.unity` with CoreGame, Main Camera, and Main Directional Light. CoreGame serialized references point to all eight generated prefabs.
- After refresh, Unity editor state reported `ready_for_tools: true`, `is_compiling: false`, and `is_playing: false`. `read_console` filtered to `Assets/KwaFee` returned zero errors/warnings. The pre-existing Burst linker permission errors remain outside this project code and are documented in DECISIONS.md.
- Added `KwaFee.EditModeTests` with three focused `FlingRules` tests. Unity MCP EditMode job `c6dad97a75054eb28a852d423e347789` completed successfully: 3 total, 3 passed, 0 failed, 0 skipped.
- Build settings now contain `Assets/KwaFee/Scenes/CoreLoop.unity` as the single enabled scene. No player build was attempted because the repository's documented Unity license and WebGL module prerequisites remain unavailable.
- Re-imported authored art after replacing high-detail convex MeshColliders with a sphere collider for Cup and capsule colliders for Barista/Customer; static Counter/Floor/Wall/Tray/Sign retain non-convex MeshColliders. The refreshed Unity console returned zero errors/warnings and one expected import log. EditMode job `a32e81720b1d41b4a1f8579d0a9617ce` again passed 3/3 rules tests.
- Unity asset inspection confirms `Cup.prefab` exposes `SphereCollider`, `Barista.prefab` and `Customer.prefab` expose `CapsuleCollider`, and each retains MeshFilter/MeshRenderer authored visuals.
- Static asset inspection confirms Counter, Floor, and Tray retain MeshCollider components for the authored environment and service surface.

## 2026-09-10 — Verb layer implementation

- Added deterministic `VerbRules` for CHUG boost/overdose, FIX repair bounds, STEAL eligibility, and SABOTAGE damage. Extended `Barista` with command methods and seeded-time jitter, and `CoreGame` with machine health, chaos, verb counters, Boston roasts, and HUD controls (`SPACE`, `L`, `Q`, `P`).
- EditMode job `78818d05f4644e9093ed0095f555108a` passed all 6 tests, including the three new verb-rule suites. Unity editor state remained ready after refresh and the project-filtered console remained clean.
- Artifact 2 gate is not claimed: the required `/sim` chaos-rate/theft-distribution evidence and human fun check are still unavailable until the isolated runtime prerequisite is resolved.

## 2026-09-10 — Core-loop repair, verification, and review (no RATER pass claimed)

- Art regeneration after adding 4 machines and grounding origins: 12 required meshes (Cup/Barista/Customer/Counter/Floor/Wall/Tray/Sign/Espresso/Grinder/SteamWand/IceMachine), 17,112 geometric triangles (≤35,000 cap), every non-floor mesh bounds.min.y == 0.0, finite vertices/normals, submesh material indices in bounds. Art manifest contract check (python) verified OK.
- Runtime repair landed: rack grab semantics + machine backlog drain; CHUG requires/consumes coffee via VerbRules; machines as real scene objects with production + use-degrade + FIX/SABOTAGE; per-player TipLedger tips with thief-credited owner; FixedUpdate uses Time.fixedDeltaTime (0.02) with manualSim guard; placement origins aligned; child-rotation hack removed; ServeZone convex BoxCollider trigger; InitializeSim + metrics surface (PlayerTips/BrokenMachines) added.
- Editor repair: CoreSceneBuilder Names extended to 12; Validate now rejects unknown/duplicate/missing required names with messages; scene builder wires the 4 machine prefab fields.
- Repro repair: .gitignore now tracks the full Unity project (ProjectSettings, Packages, all Assets), so a fresh checkout reproduces the scene/render/project config.
- Unity MCP verification sequence (all via mounted MCP tools): editor idle+ready; `KWA FEE/Import Authored Art` produced all 12 prefabs (8 originals + 4 machines) with zero KWA FEE console errors; `KWA FEE/Create Core Loop Scene` rebuilt the scene (clean, baseline Burst AotLinkerExceptions only — pre-existing root-owned burst-lld-21-hostmac failures, outside project code); EditMode test job `dad8fa8539ea4a9fbed00af91af751b4`: 15 total, 15 passed, 0 failed, 0 skipped (6 prior rules + 9 new VerbAndMachineRules tests).
- Compile cleanup history (all real, all fixed): missing `using System.Collections.Generic`/InputSystem on CoreGame, missing CupState enum on CoffeeCup, BoxCollider.halfExtents (Unity 6 property is `size`), CoreGame.Roast private-to-internal, SimHarness File/CreateDirs API, editor Validate `.isEmpty()` → `missing.Count > 0`. Final compile: zero KWA FEE errors.
- Two-axis adversarial review (code-review skill, 70 lines, '/Users/albertodesaintmalo/.omp/agent/sessions/-work-kwaffee/2026-09-10T21-54-56-162Z_01a08d51-1422-75ca-bf15-7cd691a04214/local/code-review-verdict.md'): scores Fun 6, Correctness 5, Architecture 5, Unity best practice 3, Determinism 7; 2 blockers (both contested on Unity facts — structs don't allocate, non-convex static MeshColliders are valid), 5 majors (overdose unreachable, BotMove override, machines countdown-only, dead chug rules, sim report path/API), 10 minors. All 5 majors and the actionable minors fixed in a single pass. NOT a RATER gate pass; no gate score awarded.
- /sim numbers gate remains open: SimHarness is built and compiles, but no verified isolated Unity runtime (container/VM) exists to execute it; host/editor gameplay execution stays prohibited per DECISIONS. Real chaos/min, serve, tip, and machine-cascade numbers will be recorded when that runtime is prepared. This is the only blocked item on the core-loop artifact.

## 2026-09-10 — Playable round shipped + first live Play Mode validation (no RATER pass)

- Human authorized host Play Mode / in-editor execution (recorded in DECISIONS). Round system landed: 3-minute shift clock, order spawn (12s interval, 3 max, 40s patience, Boston demand lines), shared quota (8 serves), Big Tony fixture loss on miss, per-player culprit/junkie/tip counters, round-end awards summary, R restart.
- RoundRules pure module (TDD, 6 tests) + core integration. EditMode job `a0bd09579680410bbeb8c9ab626a3537`: 21 total, 21 passed, 0 failed.
- Live Play Mode (via MCP manage_editor play, host authorized): first clean run — 0 runtime errors after 15s; live-state probe at t=3.58s showed 4 players, 16 cups, machines degrading (98.6%), shift stepping; order board confirmed with 1 active order at t=0.02s. Two fixes found by the live run: machine prefabs weren't wired into the saved scene (Create Core Loop Scene must run OUTSIDE Play Mode — re-ran, all 4 guids now serialized), and "Triggers on concave MeshColliders not supported" (Machine no longer sets isTrigger; FIX/SABOTAGE use position math, not colliders).
- Game view screenshot captured to Assets/Screenshots/screenshot-20260910-174345.png (editor unfocused → black capture is a render-capture artifact, not a scene failure; live-state probes are the authoritative runtime evidence).
