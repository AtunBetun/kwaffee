# KWA FEE — Decisions

The working decisions of the build. Sessions resume from here; never re-derive settled choices. Append, don't rewrite.

## Settled

- **Engine:** Unity 6 (6000.6.0f1), pinned LTS. Headless authoritative server sharing the client's scene graph + physics (see `docs/adr/0001-unity-headless-authoritative.md`).
- **Models:** Blender 5.2.1 LTS, bpy-scripted (reproducible, parameterized). Export to .glb.
- **Two-tier model loop:** BUILDER = cheap tier (DeepSeek V4 Flash), RATER = expensive tier (GPT 6 Astra). Asymmetry + escalation ladder + ≤200-line RATER cap (see `docs/adr/0002-two-tier-builder-rater.md`).
- **Tracker:** local markdown `.scratch/` (see `docs/adr/0003-local-markdown-issue-tracker.md`).
- **Art direction:** How To Fish clay + Gamble With Your Friends candy arcade. 8 hard rules in THE LOOK section of the spec.
- **Theme/name:** KWA FEE, Boston-accent coffee shop, 3-4 players, five verbs (FLING/CHUG/FIX/STEAL/SABOTAGE), shared quota, per-player tips.
- **Game structure:** 3-4 min shifts, CATASTROPHE REPLAY, awards, tabloid, upgrades.

## Environmental findings (verified 2026-09-09)

- `blender` CLI on PATH: **Blender 5.2.1 LTS**, `blender -b -P` works. bpy scripts safe.
- Unity **6000.6.0f1** installed at `/Applications/Unity/Unity-6000.6.0f1/Unity.app`. License **NOT fully activated**: batchmode logged `Found 0 entitlement groups and 0 free entitlements`, `'com.unity.editor.headless' was not found`.
- **WebGL Build Support module NOT installed** (`PlaybackEngines/` has only `MacStandaloneSupport`).

## Human prerequisite (blocking, one-time)

1. Open Unity Hub → sign in → activate a Personal license.
2. Add WebGL Build Support module (multi-GB) in Hub.
Until both, every `-batchmode` build fails. Flash/astra must NOT retry licensing — a human does this outside the model loop.