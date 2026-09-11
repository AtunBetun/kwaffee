# ADR-0003: Local markdown issue tracker (`.scratch/`)

## Status

Superseded by [ADR-0004](0004-beads-issue-tracker.md).

## Context

KWA FEE's work (13 artifacts, each gated by adversarial review) needs tickets. This repo had no remote, no `gh` auth, and would be worked by AI agents in sandboxes. The tracker must be something both tiers can read and write natively, with zero third-party accounts or API tokens.

## Decision

- Tracker: **local markdown**, one feature per directory `.scratch/<feature-slug>/`, one file per issue at `.scratch/<feature-slug>/issues/<NN>-<slug>.md`.
- Statuses and triage roles recorded as plain `Status:` lines (see `docs/agents/triage-labels.md`).
- The KWA FEE prompt/spec lives at `.scratch/kwafee/spec.md`; the 13 artifacts map to tickets under `.scratch/kwafee/issues/`.

## Consequences

- Agents read/write tickets natively; zero accounts, zero API, zero setup, zero sync story.
- A UI board (e.g. "beads") was rejected: agents cannot write to a human-only surface without integration glue, so it would add prompt baggage and token burn.
- Can migrate to GitHub later by copying files if a remote appears.

## Alternatives considered

- GitHub Issues — rejected: no repo/remote, no `gh` auth, and a public issue board is the wrong home for internal build content.
- GitLab — same rejection reasons.
- "Beads" UI board — rejected: human-only surface, integration cost outweighs benefit.