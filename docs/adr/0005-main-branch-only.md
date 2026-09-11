# ADR-0005: Work directly on `main` — no branches, no worktrees

## Status

Accepted.

## Context

The studio runs parallel BUILDER sessions (cheap tier) plus occasional RATER reviews against one shared checkout. Feature branches and worktrees were considered for isolation. They cost more than they protect here: this repo's caches (`Library/`, `Temp/`, `Logs/`, `UserSettings/`) are gitignored, so a worktree is a full reimport + shader compile on first open; every real conflict in this project is a binary `.unity` scene or `.blend` file that a branch or worktree does not merge for you; and multi-agent coordination in the adversarial loop already has a coordination mechanism (Beads issue ownership, per-file ownership). The owner directed: no branches, no worktrees, work on `main` directly.

## Decision

- **All work lands on `main`.** No feature branches, no `git worktree add`, unless the owner explicitly overrides for a specific task.
- **Isolation is by ownership, not by branch:** agents claim tickets in Beads; each file has a single owner at a time; assets are authored as per-asset bpy scripts that import shared helpers from `tools/kwafee/`, each writing distinct output paths under `kwafee/Assets/KwaFee/Art/`.
- **Blender runs headless and parallel:** `blender -b -P script.py` per asset, separate processes. The Blender MCP listener (127.0.0.1:9876) is one live scene — never shared between parallel agents.
- **Commits are small and frequent** so concurrent work lands with minimal merge surface; a dirty tree blocks everyone, so commit or stash before switching tasks.

## Consequences

- Zero branch-merge ceremony; `git log` on `main` is the full history.
- Any agent touching a file another agent is editing will conflict — ownership discipline is mandatory, tracked via Beads claims.
- Binary conflicts (`.unity`, `.blend`) are resolved by hand when they do collide; keeping disjoint file ownership makes them rare.