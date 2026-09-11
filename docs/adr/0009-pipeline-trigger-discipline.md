# ADR-0009: When to run grilling, to-spec, and to-tickets

## Status

Accepted.

## Context

The backlog pipeline is three skills run in sequence: **grilling** (stress-test a plan into a resolved decision tree), **to-spec** (synthesize the conversation into a spec without interviewing), and **to-tickets** (break a spec into vertical-slice tickets with blocking edges). After the 2026-09-10 re-ticketing session, the repo settled the ticket taxonomy (ADR-0008): PROMPT artifacts are `epic` umbrellas, each with 2-3 vertical-slice `task` children, and the AFK loop consumes only `bd ready --exclude-type=epic`.

That session also exposed how expensive this pipeline is and how it can silently change tracker topology (it deleted and re-added every issue). It must NOT be re-run casually; it needs a trigger discipline, a parent/child contract, and an ID-preservation rule.

## Decision

### When the pipeline runs (trigger discipline)

The grilling → to-spec → to-tickets pipeline runs ONCE per top-level feature/artifact ticket before any implementation work on it, from a state that has enough context to synthesize. The front door is the Beads backlog:

- **A ticket that is a single PROMPT artifact umbrella gets sliced when its artifact is the next frontier.** Run the pipeline on an artifact when (a) its `Spec:` pointer spec exists and its Refined Gate is measurable, (b) its parent epic is the next undeferred frontier item, and (c) we have someone who can take the decisions (a human or, per owner ruling, an agent given leave to decide with the friendslop lens).
- **Do NOT run it on tickets that already carry implementation tickets.** A ticket with `task` children means slicing already happened; re-running is re-planning, which is only justified by a spec change, a discovered blocker, or an explicit owner request.
- **Do NOT run it on `kwaffee-6id`.** The shift-seam is a single slice with its own spec (`architecture-spec.md`) and every Artifact-10 child depends on it. It is implemented as one work ticket, not sliced.
- **Run it in ORDER along the artifact graph.** 01 precedes 02, etc. The frontier only ever advances one artifact chain at a time (see ADR-0008 blocking edges), so the pipeline is naturally serial: at most one artifact is being sliced at any moment, and grilling → to-spec → to-tickets for that artifact completes before its children become claimable.
- **If a decision is unsettled at the end of grilling, DO NOT paper over it in to-spec.** to-spec is synthesis, not a second grill. An unresolved branch means the pipeline stops and the question goes to the human; a decision left dangling at the end of to-spec is a bug.

### Parent/child contract (what to-tickets may and may not do)

- to-tickets creates `task` children under an epic and links them with `blocks` edges; it may also update the epic's description to carry the `Spec:` pointer and the children pointer list.
- to-tickets MUST NOT close, remove, or re- type a parent epic; parents stay open as the durable anchor (ADR-0008).
- Children carry the parent's original spec pointers verbatim; the last acceptance line of every child is `Spec: <path>`.

### ID preservation is mandatory

Every re-import/re-ticket must preserve original Beads IDs (`bd create --id kwaffee-<old>`), because:

- spec `Spec:` pointers reference `.scratch/kwafee/specs/<NN>-<slug>-spec.md` files, which encode `kwaffee-*` IDs in the parent/child bodies and in the `Blocked by` sections;
- the dependency edges in the tracker reference those same IDs;
- the AFK loop's claim line and the `bd ready --exclude-type=epic` frontier depend on the graph being intact.

If IDs are ever regenerated, every pointer and edge must be re-created atomically with the ID swap; do NOT leave dangling references.

### Where the pipeline writes

- Grind memos: `.scratch/kwafee/grilling/<NN>-<slug>.md`
- Specs: `.scratch/kwafee/specs/<NN>-<slug>-spec.md`
- Ticket plans: `.scratch/kwafee/ticket-plans/{plan-a,plan-b}.json` (and per-artifact slices)
- Beads: the tracker (children + epics) — always backed up to `.scratch/kwafee/beads-export-<date>.md` before any delete/re-add.

## Consequences

- The pipeline never re-plans settled work; re-running it requires a spec change, a discovered blocker, or an explicit owner request, and it always preserves IDs.
- Every work ticket is grabbable by the AFK loop and self-contained (body + `Spec:` pointer), so a fresh-context agent never needs to re-run the pipeline to know what to build.
- The tracker stays in the exact shape ADR-0008 expects: epics never claimable, children claimable, one frontier, no cycles.
- The session-memory files (`DECISIONS.md`, `EVIDENCE.md`) remain append-only records of pipeline runs and their outcomes; the pipeline itself is documented here, not re-derived from memory.