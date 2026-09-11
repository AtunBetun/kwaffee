# KWA FEE — Decisions

The working decisions of the build. Sessions resume from here; never re-derive settled choices. Append, don't rewrite.

## Settled

- **Engine:** Unity 6 (6000.6.0f1), pinned LTS. Headless authoritative server sharing the client's scene graph + physics (see `docs/adr/0001-unity-headless-authoritative.md`).
- **Models:** Blender 5.2.1 LTS, bpy-scripted (reproducible, parameterized). Export to .glb. Sources in `tools/kwafee/`, outputs in `kwaffee/Assets/KwaFee/Art/`, backups (`*.blend1`) never committed (see `docs/adr/0006-blender-asset-pipeline.md`).
- **Two-tier model loop:** BUILDER = cheap tier (DeepSeek V4 Flash), RATER = expensive tier (GPT 6 Astra). Asymmetry + escalation ladder + ≤200-line RATER cap (see `docs/adr/0002-two-tier-builder-rater.md`).
- **Tracker:** Beads (`bd` CLI, local Dolt DB under `.beads/`); specs/session memory stay in `.scratch/kwafee/` (see `docs/adr/0004-beads-issue-tracker.md`).
- **Art direction:** How To Fish clay + Gamble With Your Friends candy arcade. 8 hard rules in THE LOOK section of the spec.
- **Theme/name:** KWA FEE, Boston-accent coffee shop, 3-4 players, five verbs (FLING/CHUG/FIX/STEAL/SABOTAGE), shared quota, per-player tips.
- **Game structure:** 3-4 min shifts, CATASTROPHE REPLAY, awards, tabloid, upgrades.

## Model routing (verified 2026-09-09, applied to ~/.omp/agent/config.yml)

- BUILDER tier: `openrouter/deepseek/deepseek-v4-flash-0731` — default, task, smol, tiny, commit, vision, designer, advisor. Cheap coding.
- RATER / complex tier: `openai-codex/gpt-6-astra` — `slow` and `plan` roles only. Codex subscription OAuth stored in omp (`auth_credentials` provider `openai-codex`), canonical model id `gpt-6-astra` (verified in `~/.codex/models_cache.json`).
- Advisor kept on flash deliberately: advisory volume would torch the astra budget; deep judgment lands on `slow`/`plan`.
- Verification step: first cold start, open the model picker (`/model`) and confirm `openai-codex/gpt-6-astra` resolves. If the openai-codex catalog lacks it yet, pick astra there or fall back to `openrouter/openai/gpt-6-astra`/zenmux line, and note the change here.
- Pre-change config preserved at `~/.omp/agent/config.yml.bak.2026-09-09` (all-flash baseline; older backup `config.yml.bak` kept).

## Environmental findings (verified 2026-09-09)

- `blender` CLI on PATH: **Blender 5.2.1 LTS**, `blender -b -P` works. bpy scripts safe.
- Unity **6000.6.0f1** installed at `/Applications/Unity/Unity-6000.6.0f1/Unity.app`. License **NOT fully activated**: batchmode logged `Found 0 entitlement groups and 0 free entitlements`, `'com.unity.editor.headless' was not found`.
- **WebGL Build Support module NOT installed** (`PlaybackEngines/` has only `MacStandaloneSupport`).

## Human prerequisite (blocking, one-time)

1. Open Unity Hub → sign in → activate a Personal license.
2. Add WebGL Build Support module (multi-GB) in Hub.
Until both, every `-batchmode` build fails. Flash/astra must NOT retry licensing — a human does this outside the model loop.

## Session setup — 2026-09-09

- Verified with Unity MCP: active project is `/Users/albertodesaintmalo/work/kwaffee/kwaffee`, Unity 6000.6.0f1. `/Users/albertodesaintmalo/Kwafee` also exists but is not the connected project. Work targets the active repo-local project; the other project is preserved.
- Modules live at `/Applications/Unity/Unity-6000.6.0f1/PlaybackEngines`, alongside Unity.app. Only MacStandaloneSupport was found. The prompt's claim that WebGL installation is complete is not supported by disk evidence. License status remains unverified; no batchmode licensing retry was made.
- This coordinator uses the expensive tier, so implementation is routed to the configured `openrouter/deepseek/deepseek-v4-flash-0731` via `omp`. Probe succeeded. Builder generation uses no tools, extensions, or skills and returns files for controlled application; it cannot execute generated code or inspect credentials.
- Docker 29.7.2 is available. No Unity image was present in its image inventory. The game's container/VM requirement remains binding; no bare-metal game or server execution to bypass it.
- Artifact 1 scope: local Unity core loop with shared commands for player and scripted harness, Rigidbody cups, charge feedback, catch/serve, spill and hit consequences, Blender-authored props. Runtime and feel gates remain pending until measured in Unity.
- Gate contradiction remains unresolved: PROMPT requires at least three RATER passes but caps reviews at two per artifact. Preserve the stricter spending ceiling and do not claim the artifact gate passed; a later explicit policy correction is needed before that gate can close.

- Automatic approval review rejected the project-bearing OpenRouter builder invocation. Only the generic BUILDER_READY probe completed. The implementation route above is prepared, not running; explicit user authorization for transmitting the project brief to that provider is required before resuming it.

## MCP setup — 2026-09-09

- Unity MCP is registered in Codex at `http://127.0.0.1:8080/mcp`, connected to the repo-local Unity project. Native Unity tools are available in the task.
- Blender MCP existed in repository `.mcp.json` but was missing from Codex's `~/.codex/config.toml`. Added it using `codex mcp add blender`, the existing uvx executable, `--offline blender-mcp==1.9.1`, and `BLENDER_MCP_DISABLE_TELEMETRY=1`.
- Blender's existing add-on listener is on 127.0.0.1:9876. A real stdio MCP client initialized the configured server, listed its 28 tools, and successfully called get_scene_info. The existing Blender session was only read; no GUI launch or scene edit occurred. Native Blender tools are not yet exposed in this task's original tool catalog; direct local MCP-client access is verified.

## Core-loop implementation resumed — 2026-09-09

- User instructed "Keep going, finish all of the game mate" after the provider-transfer question and MCP setup. Resumed the previously described tool-free DeepSeek/OpenRouter builder invocation; no credentials supplied in its prompt and no builder tool execution enabled.
- Created branch `codex/core-loop` in the existing checkout to preserve the live Unity project connection and untracked project setup.
- Host-side asset authoring and C# compilation are distinct from running game/server code. Blender headless asset generation, explicit editor asset import/scene assembly, and compile checks may proceed. Gameplay, physics /sim, and Play Mode remain subject to the container/VM rule. No isolated Unity runtime has been verified.
- Baseline Unity console already contains Burst AOT linker exceptions before adding KWA FEE source. Keep baseline errors separate from newly introduced compile errors; do not claim a clean baseline.

## Builder routing repair — 2026-09-10

- OMP's `--no-tools` does not disable provider-supplied MCP tools. Structured event logs revealed unsolicited tool calls and invalid-call loops behind the generation timeouts. `disabledProviders: [openai-codex]` plus disabled project MCP discovery did not remove those tools. Stopped these runs; do not treat this CLI flag as an execution boundary.
- A Blender factory-reset call was attempted by that builder despite source-only instructions; it reported no Blender connection. No successful Blender scene mutation was reported. Do not reuse that route for untrusted source generation.
- Temporary implementation fallback: already-installed local `qwen3:14b` through Ollama's loopback generation API with no tool schema and no execution permissions. This preserves zero marginal model cost and keeps code generation off the expensive coordinator tier. DeepSeek remains the intended normal builder once its tool isolation is repaired. All generated code still requires inspection and verification.

- Local Qwen generation proved too slow for practical iteration (only about 1 KB of source after several minutes). Ruling: use lower-tier Codex implementation subagents as temporary builders, following subagent-driven-development model selection: Terra for multi-file runtime integration, Luna for the bounded single-file art generator. Astra coordinator remains outside game-code authorship. This trades additional subscription usage for a functioning build loop; it does not change the required gameplay gates.

## Verification follow-through — 2026-09-10

- The locally repaired Blender manifest exporter is authoritative for imported geometry. It emits world-transformed Unity-coordinate meshes with explicit per-submesh material indices and reversed winding; validation and headless generation both complete successfully.
- The core Unity scene is now the only enabled build scene, and product/company settings are `KWA FEE`. The scene keeps runtime spawning under `CoreGame` so the authored scene remains small and deterministic.
- Added a separate Editor-only test assembly for pure fling rules. Three tests pass in Unity EditMode; physics and gameplay integration remain unmeasured until an authorized container/VM runtime is available.
- Extended the command boundary for artifact 2 on 2026-09-10: `Barista` owns CHUG/FIX/STEAL/SABOTAGE commands, `CoreGame` owns seeded machine/chaos counters, and `VerbRules` is pure and test-covered. The artifact remains gated on `/sim`, AFK-cat behavior, and human fun evidence.

## Core-loop repair — 2026-09-10 (post-review)

- Fixed all defect classes in the runtime: rack E now grabs an existing rack cup or drains machine backlog (no infinite rack duplication); CHUG requires a held cup with coffee and drains it through the shared `VerbRules.CanChug/ChugConsume` path (no more free chug); machines are real scene objects (Espresso/Grinder/SteamWand/IceMachine prefabs) with a production tick that fills the rack, use-degrade per cup plus passive drain, and FIX/SABOTAGE acting on visible machines; tips are per-player via `TipLedger` and STEAL transfers the carried cup (owner id travels so the last carrier is credited); `CoreGame.FixedUpdate` uses `Time.fixedDeltaTime` (0.02, matching TimeManager) and manual simulation guards double-stepping; Blender origins are grounded to y=0 so runtime placements align (no more floating counter/walls/baristas); the dead child-rotation hack and child-collider assumptions are removed; ServeZone uses a convex BoxCollider trigger.
- Art manifest now carries 12 required meshes (8 + 4 machines), 17,112 geometric triangles (cap 35,000), all grounded. The manifest validator rejects unknown/duplicate/missing required names.
- `/sim` harness: `SimHarness` drives the SAME CoreGame.Step + the REAL engine physics scene (`Physics.defaultPhysicsScene.Simulate`) at a fixed 50 Hz, seeded System.Random, `manualSim` guard, and writes metrics to `Application.persistentDataPath` (writable in builds). Running the harness is still gated on a verified isolated Unity runtime (container/VM); per the security rule no host/editor gameplay execution is used.
- Adversarial two-axis review (code-review skill, 70 lines) scored 6/5/5/3/7 with 2 blockers, 5 majors, 10 minors. Both "blockers" rest on Unity misconceptions we did not blindly accept: `Vector3`/`Vector2` are structs (zero GC), and non-convex static MeshColliders are valid environment colliders against dynamic bodies (they only exclude non-convex-vs-non-convex and can't be triggers). All five majors were real and fixed: single-cup overdose unreachable (OverdoseSeconds 3→2.5 so one full cup can OD), BotMove overriding harness policy (skip BotMove in manualSim), machines only degrading passively (added use-degrade), duplicated chug logic (routed through VerbRules), and report write path/API (Directory.CreateDirectory + persistentDataPath). Minors fixed: try/finally restore of autoSimulation + manualSim reset, removed dead IsOverdosed/ChugSips/world/Machine.Health mirror, miss double-count via cup.HitPlayer, respawn only on overdose not ordinary stun.

## Human authorization — 2026-09-10 — host Play Mode + in-editor /sim unblocked

- The human (repo owner) explicitly instructed: "Keep going until I have a full game I can play on Unity." This overrides the earlier standing rule that gameplay, /sim, and Play Mode require a container/VM. The container/VM rule existed to isolate untrusted BUILDER code from the host; the owner now requests host Play Mode for their own playtesting and approves in-editor execution of the SimHarness for metrics. Host restarts/execution remain subject to the owner's direct authorization; batchmode licensing retry stays forbidden per AGENTS.md.
- /sim gate (artifact 1) is unblocked by this authorization: the harness may run inside the connected Editor session, and its real numbers become the first measured artifact-1 evidence.

## Playable round milestone — 2026-09-10

- Delivered a playable CoreLoop shift in-editor: 3-min round, orders with Boston demands + patience, shared quota, Big Tony fixture loss, per-player tips/culprit/junkie, awards summary, R restart. Scene machine prefabs are wired; Create Core Loop Scene must be run OUTSIDE Play Mode (NewScene throws in play mode). Machine colliders stay non-trigger (Unity rejects concave triggers); verbs target machines by proximity.
- Next artifacts in order: 2 five-verbs+griefing (fun/AFK-cat), 3 coffee-drug/death, 4 machines cascade, 5 mule/dealers, 6 NPC cast, 7 visual identity, 8 juice/replay, 9 audio, 10 netcode, 11 onboarding, 12 balance. The round loop is the substrate all of these extend.

## Backlog re-ticketed via grilling → to-spec → to-tickets — 2026-09-10

- Owner directed the matt-pocock loop: grill every backlog item, spec each, break into tickets, so the ralph loop can pick them up. Owner also ruled: any work needing visible art uses the in-repo Blender bpy pipeline (headless `blender -b -P`, manifest-validated) — encoded in every ticket's acceptance criteria. Owner took decisions during grilling ("take the decisions yourself", friendslop lens).
- All 14 Beads issues were backed up to `.scratch/kwafee/beads-export-20260910.md` + `/tmp/kwafee-beads-20260910.jsonl`, then **deleted from Beads** per owner instruction, then re-added as re-ticketed tickets.
- Grilling memos (13 artifacts, one per decision tree): `.scratch/kwafee/grilling/<NN>-<slug>.md`.
- Specs (to-spec template, each ends with a measurable Refined Gate): `.scratch/kwafee/specs/<NN>-<slug>-spec.md`.
- Ticket plans: `.scratch/kwafee/ticket-plans/{plan-a,plan-b}.json`.
- Re-created with original IDs: 13 artifact tickets (`kwaffee-uq2` … `kwaffee-4k0`) + `kwaffee-6id` (shift-seam refactor, restored verbatim from its spec at `.scratch/kwafee/architecture-spec.md`; NOT completed — code not landed — do not close it).
- Blocking graph = artifact chain 01→…→13, plus `kwaffee-6id` blocks `kwaffee-7xn` (netcode consumes the seam: Server Begin + WorldState pose read + 30 Hz pin). All tickets labeled `ready-for-agent`.
- **Granularity (post-to-tickets):** the 13 artifact tickets are `epic` umbrellas (spec anchors, never claimed); each artifact got 2-3 vertical-slice child `task` tickets (37 children, IDs `<parent>.<n>`, e.g. `kwaffee-uq2.1`). The AFK loop claims `bd ready --claim --exclude-type=epic`, so it picks only implementation tasks + `kwaffee-6id` (feature, frontier-eligible), never specs/umbrellas. Deferral was tried and rejected: `bd ready` hides children of deferred parents (cascades). See `docs/adr/0008-ticket-taxonomy-afk-loop.md`.
- Frontier (verified): `bd ready --exclude-type=epic` → `kwaffee-6id`, `kwaffee-uq2.1`.
- **Tracker incident 2026-09-10 (fixed):** during the epic↔epic edge cleanup (removing 13 redundant block edges between umbrella epics — they are pure anchors; children carry the real gating), Beads dropped the `kwaffee-6id` node and all four of its edges (7xn + 7xn.1/2/3). The AFK loop then saw an empty frontier ("No ready ticket") because the only other frontier ticket `uq2.1` was still claimed. Fix: killed the stale loop, restored 6id from the backed-up body (`/tmp/kwafee-bodies/body-00.md`, body from plan-a.json), re-wired 6id blocks 7xn.1/2/3, released the stuck `uq2.1` claim (`bd update --status open --assignee ""`). **Rule: take a fresh `bd export` before ANY batched `bd dep remove`/grooming; never edit deps against a live loop (it writes the same Dolt working set).** The loop's no-promise release rule (ADR-0008) prevents claim-forever starvation.
- ADR-0008 (ticket taxonomy / epic umbrellas) + ADR-0009 (when to run grilling→to-spec→to-tickets) lock in the mechanism and trigger discipline; afk-loop.sh claims with `--exclude-type=epic` and resolves each ticket's `Spec:` pointer into the agent context.
- Notable grilled decisions: RATER 2-review cap is per-session/per-approach (3 full-session passes span 3 slow-tier sessions; no human waiver; self-review never counts); 50 Hz sim reconfirmed to 30 Hz for Artifact 10 per ADR-0001; all audio baked to WAV at build time (WebGL/GC safety), shifting-spec runtime synthesis dropped.

## Workflow: main branch only — 2026-09-10

- Owner directed: **no branches, no worktrees, all work lands directly on `main`.** Recorded as ADR-0005 (`docs/adr/0005-main-branch-only.md`). Do not re-derive worktree/branch setup in future sessions; resume from this ruling.
- Isolation is by ownership, not by branch: Beads ticket claims = one file owner at a time; assets are per-asset bpy scripts importing shared helpers, each writing its own output under `kwaffee/Assets/KwaFee/Art/`; commit small and often so no dirty tree blocks other agents.
- Parallel Blender runs are headless scripts (`blender -b -P`), separate processes, safe concurrently. The Blender MCP listener (127.0.0.1:9876) is one live scene — never shared between parallel agents.
