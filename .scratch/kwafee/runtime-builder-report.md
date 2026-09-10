# Runtime builder report — artifact 1

Implemented the six requested source artifacts under `kwaffee/Assets/KwaFee/Runtime/`:

- `KwaFee.Runtime.asmdef` references the Input System.
- `FlingRules.cs` supplies the pure 5–12 speed curve over 1.1 seconds and liquid-scaled tips.
- `CoffeeCup.cs`, `Barista.cs`, and `ServeZone.cs` own cup, player-command, and trigger behavior.
- `CoreGame.cs` composes the authored prefab world, 16-cup pool, input, HUD, counters, gameplay ticking, recycling, and manual simulation entrypoint.

## Editor integration

Create a scene object with `CoreGame`, assign all eight required Blender prefabs, and set the scene camera to `(0,12,-12)` looking toward `(0,0,1)`. `CoreGame.Start()` calls `Initialize(7, false)` only while in Play Mode. An unassigned prefab reports one actionable `Debug.LogError` and does not create a substitute.

All barista visual children receive local yaw 180 degrees because the generated art faces Blender `-Y`; command-facing gameplay remains Unity `+Z` forward. The runtime does not make preview objects or alter editor state.

## Public manual-simulation API

`Initialize(int seed, bool botMode)` creates the complete instance-local world and deterministic bot randomness. `Step(float dt)` advances game-owned state without calling global physics. `Tick(float dt)` is an equivalent harness-friendly alias. `Simulate(PhysicsScene scene, float dt)` advances state and then calls `scene.Simulate(dt)` for an explicitly configured local physics scene. Command a barista via `Players[index].Move(Vector2)`, `.Aim(Vector3)`, `.TryGrab()`, `.Drop()`, `.BeginCharge()`, and `.ReleaseCharge()`. Inspect `Cups`, `Shots`, `Serves`, `Misses`, `Catches`, `Hits`, and `TotalTips` for harness assertions.

## Verification boundary

Unity MCP refreshed the project with the runtime and test assemblies present; the editor reached `ready_for_tools: true` with no KWA FEE console errors or warnings. Generated dynamic prefabs use primitive colliders (sphere/capsule) to avoid Unity's convex MeshCollider hull limit; static props use non-convex MeshColliders. EditMode jobs `c6dad97a75054eb28a852d423e347789` and `a32e81720b1d41b4a1f8579d0a9617ce` each passed all three `FlingRules` tests. Physics, Play Mode, deterministic `/sim`, and player builds remain unverified because the required isolated Unity runtime, activated license, and WebGL module are not available.
