# Issue tracker: Beads

Issues live in a local Dolt DB under `.beads/`, driven by the `bd` CLI (installed at `~/.local/bin/bd`). Specs stay as markdown under `.scratch/`; only tickets are Beads issues.

## Conventions

Operation → command mapping. Issue IDs look like `kwaffee-ab12` (prefix per repo).

| Operation | Command |
| --------- | ------- |
| Create | `bd create "<title>" --body-file <path> --silent` (captures ID) |
| Create (one-line) | `bd create "<title>" --description "..."` |
| List | `bd list` / `bd query` / `bd search <text>` |
| Read | `bd show <id>` |
| Comment | `bd comment <id> --message "..."` |
| Claim | `bd update <id> --claim` |
| Set status | `bd update <id> --status <state>` |
| Close | `bd close <id>` |
| Label (triage roles) | `bd tag <id> <label>` (alias for `bd label add <id> <label>`) |
| Dependency | `bd link <depended-on> <blocker>` — second arg **blocks** first (`bd dep <blocker> --blocks <blocked>` is the explicit form) |
| Hierarchy | `bd create --parent <id>` / `bd children <id>` / `bd link --type parent-child` |
| Blocking view | `bd blocked` |
| Batch import | `bd import < file.jsonl` (JSONL, IDs preserved, upsert; from `bd export`) |
| Available work | `bd ready` (open, unclaimed, undeferred) |

Triage roles are first-class labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix` (see `docs/agents/triage-labels.md`).

## When a skill says "publish to the issue tracker"

Run `bd create` (capture the ID with `--silent`), then apply labels (`bd tag <id> <label>`), dependencies (`bd link`), and parent (`--parent <id>`) as needed. Specs themselves stay as markdown files — only tickets are issues.

## When a skill says "fetch the relevant ticket"

`bd show <id>`. The user normally passes the ID, or the `bd query`/`bd search` that finds it.

## Wayfinding operations

Used by `/wayfinder`. The **map** is a markdown file; each **decision ticket** is a Beads issue.

- **Map**: `.scratch/<effort>/map.md` (the Notes / Decisions-so-far / Fog body).
- **Node**: one Beads issue per ticket. Create with `bd create --parent <map-epic-id> --type decision --title "<question>"`. There is no `Type:`/`Status:`/`Blocked by:` file line — type and status are Beads fields, blocking is `bd link`.
- **Blocking**: `bd link <dependent> <prerequisite>` (prerequisite blocks dependent), or `bd dep <prerequisite> --blocks <dependent>`. A ticket is actionable when nothing blocks it (`bd blocked` shows them).
- **Frontier**: `bd ready` — open, unclaimed, undeferred issues; first by ID wins.
- **Claim**: `bd update <id> --claim` before working.
- **Resolve**: `bd comment <id> --message "<answer>"`, `bd close <id>`, then append a context pointer to the map's Decisions-so-far in `map.md`.

## This repo (KWA FEE)

Prompt + spec live at `.scratch/kwafee/` (`PROMPT.md`, `spec.md`). Tickets for each artifact in the prompt's ARTIFACTS & GATES list are Beads issues (IDs `kwaffee-*`).

**`.scratch/kwafee/issues/*.md` are archived snapshots only.** They were imported into Beads on 2026-09-11 (bodies verbatim, `Status:` line became the `ready-for-human`/`needs-triage` label); keep them as history, never create tickets there, and don't rely on them for current state — `bd show` wins.