# AGENTS.md — How to work in this repo

## Agent skills

### Issue tracker

Issues and specs live as markdown files under `.scratch/<feature-slug>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Five role labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` at repo root, ADRs in `docs/adr/`. Read `CONTEXT.md` before exploring. See `docs/agents/domain.md`.

## Project: KWA FEE

A raucous online party game (Unity + Blender + Boston-accent satire) built by an adversarial-loop agent studio. Full spec: `.scratch/kwafee/spec.md`.

## Build prerequisites (HUMAN, one-time, do not retry)

- **Unity license**: batchmode builds fail with `Found 0 entitlement groups and 0 free entitlements` / `'com.unity.editor.headless' was not found` until a human opens Unity Hub, signs in, and activates a Personal license. Do not retry batchmode builds until this is done.
- **WebGL module**: `PlaybackEngines/` has only `MacStandaloneSupport`. The browser party build needs `WebGL Build Support` added in Unity Hub (multi-GB download).
- Blender 5.2.1 LTS CLI works (`blender -b -P script.py`) — bpy asset scripts are safe.

## Working agreements

- Every session begins by reading `CONTEXT.md`, `/DECISIONS.md`, and `/EVIDENCE.md` (in `.scratch/kwafee/`). Resume from them; never re-derive settled decisions.
- Ticket files are one per issue under `.scratch/kwafee/issues/<NN>-<slug>.md`.