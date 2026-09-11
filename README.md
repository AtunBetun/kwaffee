# KWA FEE

A raucous online party game: 3-4 friends run a barely-legal Boston coffee shop, flinging physics coffee, serving demanding customers, and wrecking each other for tips. Built in Unity + Blender, authored entirely by an autonomous BUILDER/RATER agent loop.

## Quick start

```sh
./afk-loop.sh 20      # claim + run one Beads ticket per omp iteration
```

The loop picks the highest-priority unclaimed ticket, streams the agent's work live, saves the full event stream to `.scratch/kwafee/convos/`, and closes the ticket only on `<promise>DONE</promise>`.

## Layout

- `kwaffee/` — Unity project (game, server, tests: EditMode unit + PlayMode integration via SimHarness)
- `tools/kwafee/` — Blender bpy asset scripts (sources of all 3D art)
- `docs/adr/` — architecture decisions; `CONTEXT.md` — domain vocabulary
- `.scratch/kwafee/` — spec, decisions, evidence, session memory
- Issues: Beads (`bd ready`, `bd show <id>`); syncs via `refs/dolt/data` on `origin`

## Session rules

Every session starts by reading `CONTEXT.md` + `.scratch/kwafee/DECISIONS.md` + `EVIDENCE.md`. ADR-0001 (Unity shared-physics), ADR-0002 (two-tier BUILDER/RATER), and ADR-0005 (work on `main`, no branches) are load-bearing. See `AGENTS.md`.