# ADR-0004: Beads issue tracker (`.beads/`)

## Status

Accepted. Supersedes ADR-0003.

## Context

ADR-0003 chose local markdown tickets because the repo had no remote and agents needed zero-setup native read/write. The Beads CLI (`bd`, Dolt-backed local DB) entered the repo on 2026-09-11 and covers the same requirements with first-class labels, dependencies, claim/defer, and batch import/export. The 13 markdown tickets were imported verbatim the same day; `Status:` lines became Beads labels.

## Decision

- **Tickets:** Beads issues via `bd` (local Dolt DB under `.beads/`, CLI at `~/.local/bin/bd`).
- **Documents:** specs and session memory stay as markdown at `.scratch/kwafee/` (`spec.md`, `PROMPT.md`, `DECISIONS.md`, `EVIDENCE.md`). Tickets are Beads issues; documents are files.
- **Archive:** `.scratch/kwafee/issues/` is removed; the snapshots live in git history and the Dolt DB.

## Consequences

- First-class labels (`bd tag`), dependencies (`bd link` / `bd dep`), claim, defer, and a JSONL migration path (`bd export` / `bd import`).
- Dolt sync rides `refs/dolt/data` on the git remote; `.beads/` is gitignored.
- Full operation map lives in `docs/agents/issue-tracker.md`.