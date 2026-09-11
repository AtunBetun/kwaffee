# ADR-0010: v2 restart — first-person co-op pivot, wipe and rebuild

## Status

Accepted (2026-09-10, owner-directed).

## Context

The v1 codebase (archived at `.scratch/kwafee/backups/v1/` and Beads backup `beads-export-20260910-preWipe.jsonl`) was a third-person top-down-ish party barista sim with hard-coded custom physics verbs and no guns. The owner directed a complete pivot: **first-person** controls (each player truly first person), **guns** in the game loop, **co-op** (shared objective, shared till), minimal UI, and maximum reuse of free Unity Asset Store assets so the build is mostly wiring code and assembling scenes. The owner directed the coordinator to take all design decisions ("work by yourself", "take the decisions yourself") and to delete everything.

## Decision

- **Wipe:** all v1 Beads issues deleted (backed up first), all v1 authored Unity code deleted (`kwaffee/Assets/KwaFee`, `Scenes`, `TutorialInfo`, `Screenshots`, v1 input actions; stale folder metas removed). `ProjectSettings/` + `Packages/` + `manifest.json` kept so Unity Hub reopens the project without human re-setup.
- **Cherry-picked carry-over (v1 → v2):** Unity 6 (6000.6.0f1 LTS), Blender 5.2.1 bpy pipeline, Beads tracker, main-branch-only workflow, ADR-0001 headless-authoritative architecture, round structure (3-4 min shifts, quota, Big Tony, catastrophe replay, awards), Boston accent satire, EditMode test conventions.
- **Redefined:** the game is now **first-person co-op** ("the shop runs together"): shared quota, shared till, co-op first, friendly griefing second. Guns are a core toy, not optional.
- **Being-sourced not built:** replacing the v1 custom physics-verb stack with Unity Asset Store free assets (see ADR-0015); the codebase is now the wiring layer over third-party art/audio/VFX (adapter pattern per ADR-0015).

## Consequences

- v1 code is unrecoverable from `main` except through the two snapshot commits (`40e0ca6`, `9f678a5`) and backups. That is intentional.
- A fresh Unity scene/room will be assembled from store assets; the v1 `CoreGame`/`VerbRules`/`RoundRules`/`SimHarness` architecture is abandoned — new seams per spec.
- Every future feature spec starts from `v2-design.md`; tickets are re-created from scratch in Beads.

## Alternatives considered

- Rebuild from the surviving v1 loop — rejected: the v1 loop is not first-person, has no guns, and its verbs/units contradict the co-op pivot; porting would strand the custom physics stack we are deliberately deleting.
- Partial wipe (keep machines/Shift) — rejected: the owner said "all of that codebase is useless", and the custom physics stack is the first thing to go per ADR-0013.