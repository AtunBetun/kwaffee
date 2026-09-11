# ADR-0008: Ticket taxonomy — umbrellas, vertical slices, and the AFK loop contract

## Status

Accepted.

## Context

The build loop (`afk-loop.sh`) consumes Beads the same way every session: `bd ready --claim` picks exactly ONE ready ticket, spawns one fresh-context agent per ticket, the agent reads the ticket body via `bd show <id>` plus the standing context files (`CONTEXT.md`, `DECISIONS.md`, `EVIDENCE.md`, `spec.md`, `architecture-spec.md`), and the ticket closes ONLY on a `<promise>DONE</promise>` from that agent. Failed tickets stay claimed for human triage.

The re-ticketing session (grilling → to-spec → to-tickets, 2026-09-10) initially created one Beads ticket per PROMPT artifact (the 13 originals plus the shift-seam). That shape fails the loop: a fresh agent gets handed a 16-24 KB spec in one ticket, which exceeds a single context window, and several artifacts remain claimable at once so the loop cannot serialize them.

Mechanism experiment (2026-09-10): deferring umbrellas to 2099-12-31 was tried first, but `bd ready` (and `bd ready --claim`) hide children of a deferred parent, so the umbrella deferral cascaded and hid the grabbable child tickets too. The native Beads pattern is type-based: umbrellas are `epic` issues, and the loop claims with `bd ready --exclude-type=epic`. Verified live: with parents typed `epic`, `bd ready --exclude-type=epic` surfaces exactly the implementation tickets (task children + `kwaffee-6id`) and never the umbrellas.

Decisions this ADR locks in:

- **Tickets must link back to the original spec.** The PROMPT (`PROMPT.md`) defines 13 artifacts with gates; the ticket graph exists to drive them to done, and every ticket must be traceable to its artifact, its spec, and (for visible art) the Blender bpy pipeline rule (ADR-0006).
- **The loop, not the ticket, is the consumer.** Ticket granularity, body shape, and blocking edges must satisfy `afk-loop.sh`: one grabbable implementation ticket per iteration, self-contained body, deterministic frontier, and it must never claim a spec or umbrella.
- **The loop picks tasks, not spec tickets.** Umbrellas are `epic` type; the loop runs `bd ready --exclude-type=epic`, so specs/umbrellas are unreachable by the loop by construction.

## Decision

Two ticket kinds in one Beads DB:

1. **Epic tickets (umbrellas)** — the 13 PROMPT artifact tickets: `kwaffee-uq2` … `kwaffee-4k0` (Artifacts 1-13), typed `epic` (`bd update <id> --type epic`). Each epic:
   - carries the artifact name, the PROMPT artifact pointer, the per-artifact spec pointer (`.scratch/kwafee/specs/<NN>-<slug>-spec.md`), and a line stating it is an umbrella whose work lives in children.
   - is never claimed, worked, or closed by the loop: `bd ready --exclude-type=epic` filters it out. Epics stay open and undeferred — they are the durable traceability anchor back to PROMPT.md and the spec files.
   - keeps its `ready-for-agent` label as an informational signal; the loop reads `bd ready`, and the type exclusion is the gate.

   The Shift-seam ticket **`kwaffee-6id` is NOT an umbrella**. It is grabbable implementation work in its own right (`architecture-spec.md` is its spec, and Artifact 10's children consume its seam), so it stays `feature` type, frontier-eligible, and it blocks every Artifact-10 child.

2. **Child tickets (vertical slices, the frontier)** — 2-3 per artifact, each a narrow but complete tracer bullet through its slice (schema → logic → interface → evidence), sized to one fresh context window. Each child:
   - is typed `task` (implementation work) and is the unit the loop claims.
   - has a self-contained body (`What to build` + `Acceptance criteria`) so `bd show <id>` alone is a sufficient brief; the last acceptance line is always `Spec: .scratch/kwafee/specs/<NN>-<slug>-spec.md`.
   - carries acceptance lines cut from its parent spec's Refined Gate, phrased as observable behavior (the player or the `/sim` sees it).
   - carries the Blender bpy acceptance line on exactly the children whose slice authors visible assets (per ADR-0006; "NONE" artifacts state so).
   - has blocking edges: within an artifact, children chain (`PREV_CHILD`); the first child of artifact N+1 is blocked by the LAST child of artifact N, so the `bd ready --exclude-type=epic` frontier is exactly one artifact's worth of work ahead, and artifact 13's children are the only ones ever blocking release. Within the graph this means the frontier is always small: the shift-seam plus one artifact's chain.
   - is labeled `ready-for-agent` unless claimed.

Traceability chain (any ticket → original decision):

```
PROMPT.md Artifact N
  └─ epic ticket kwaffee-*   (umbrella; type=epic; spec pointer)
       └─ spec  .scratch/kwafee/specs/<NN>-<slug>-spec.md  (to-spec; Refined Gate)
            └─ grind .scratch/kwafee/grilling/<NN>-<slug>.md  (decision tree)
                 └─ child tickets  (vertical slices; Spec: line points back)
```

Every ticket body ends with a `Spec:` pointer, so an agent that reads `bd show <id>` can climb the chain to the full spec and, through it, to the PROMPT gate.

## Consequences

- `bd ready --exclude-type=epic` is the sole frontier for `afk-loop.sh`; the claim line in the loop MUST keep the `--exclude-type=epic` flag or the loop will start claiming 20 KB umbrellas.
- Umbrellas are never deferred. Deferral cascades to children in `bd ready` and would hide the frontier — do not reintroduce it for taxonomy.
- Re-running the re-ticketing flow (grilling/deleting/re-adding) must preserve original parent IDs (`bd create --id kwaffee-<old>`) or every `Spec:` pointer and dependency edge dangles.
- The `Spec:` pointer is load-bearing for agents; if the per-artifact specs ever move, the pointer in every parent and child must move with them.
- Epic labels stay informational; do not "clean up" an epic's `ready-for-agent` to `needs-triage` — the loop never sees it either way, and dropping it would misstate the artifact's intent to human readers.