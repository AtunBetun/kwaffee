# Editor builder report

Created `kwaffee/Assets/KwaFee/Editor/KwaFee.Editor.asmdef` and `CoreSceneBuilder.cs`.

The importer is available only from `KWA FEE/Import Authored Art`. It reads `Assets/KwaFee/Art/core-meshes.json`, validates required model names, finite vertex and normal data, triangle and material indices, creates URP Lit materials with manifest color/roughness, creates mesh assets and MeshFilter/MeshRenderer/MeshCollider prefabs under `Assets/KwaFee/Generated`, and overwrites existing assets at stable paths. Mesh normals are preserved when supplied and recalculated only when absent; large meshes use UInt32 indices. No primitive fallback, callbacks, initialization hooks, process/network access, or gameplay execution is used.

The scene command is `KWA FEE/Create Core Loop Scene`. It refuses to proceed when any open scene is dirty, creates `Assets/KwaFee/Scenes/CoreLoop.unity`, preserves `SampleScene`, assigns all eight public `CoreGame` prefab fields, and adds the requested camera, directional light, and warm flat ambient lighting. It does not add an editor preview hierarchy because runtime preview coupling was unresolved; the scene is preparation only.

Verification: Unity MCP executed both menus successfully. After a refresh, editor state reported `ready_for_tools: true` and no compilation in progress; `read_console` filtered to `Assets/KwaFee` returned zero errors or warnings. The generated CoreLoop hierarchy contains CoreGame, Main Camera, and Main Directional Light, and CoreGame serialized references resolve to all eight generated prefabs. Play Mode and player builds remain intentionally unverified due to the documented isolated-runtime and Unity license prerequisites.
