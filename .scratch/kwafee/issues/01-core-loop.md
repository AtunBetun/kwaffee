# Core loop prototype

Status: ready-for-human

Implement artifact 1 of ../PROMPT.md. Gate remains pending until actual Unity simulation and observed play support charge readability, floaty flight, spill legibility, self-teaching controls, and consequential hits.

## Implementation plan

Goal: One playable local core-loop scene with a charge/fling/catch/serve cycle, authored clay/candy props, real Rigidbody impacts, and a seeded CLI harness exercising the same gameplay components.

Architecture: Unity 6000.6.0f1, URP, Input System already installed. Separate gameplay components from input and presentation. Simulation drives the same commands and physics used by the player at 30 Hz. Artifact 1 is explicitly local; netcode remains artifact 10.

Spec: ../PROMPT.md, artifact 1 and global art, security, evidence, and budget constraints.

- [x] Cheap BUILDER route was attempted, then replaced with reviewed local implementation after provider tool-isolation failures. Source, tests, reproducible Blender assets, and setup instructions are present under the approved workspace paths.
- [x] Inspected generated paths and scripts. Generated code has no automatic editor execution, external downloads, or credential access.
- [x] Ran the authored Blender script headlessly and recorded actual asset measurements in `../art-builder-report.md` and `../EVIDENCE.md`.
- [ ] Compile C# and assemble assets/scene through the connected editor. EditMode rule tests pass, but gameplay tests and deterministic `/sim` remain blocked until a suitable container or VM runtime is available. Host Play Mode is intentionally not used.
- [ ] BUILDER self-review fixes implementation findings before RATER review.
- [ ] Submit to separate RATER only when /sim evidence exists. Do not award feel/fun scores from source inspection.

## Comments

2026-09-09: Session started. No game code existed in the active Unity project. Unity MCP identifies the active root as /Users/albertodesaintmalo/work/kwaffee/kwaffee. Docker 29.7.2 is reachable, but listed local images contain no Unity runtime. WebGL module remains absent. Core-loop gate is pending.

2026-09-09: Builder readiness probe returned BUILDER_READY, but automatic approval review rejected the subsequent project-bearing invocation: explicit authorization is required to transmit the private prompt, context, decisions, issue, and implementation brief to OpenRouter. No project-bearing invocation ran. Await that authorization before retrying; do not route the payload indirectly. Brief is saved at ../core-builder-request.md. No source, assets, test results, or RATER scores were generated.

2026-09-09: User instructed continuation after the transfer question. Tool-free cheap builder invocation resumed on codex/core-loop. Implementation output pending. Runtime gate remains blocked by missing verified Unity isolation and WebGL module; asset authoring and compilation can proceed.

2026-09-10: Core implementation is present. Blender build and Unity authored-art import completed; CoreLoop scene references all eight generated prefabs and is the only enabled build scene. Unity EditMode rule tests pass 3/3 and project-filtered console is clean after refresh. This ticket remains `ready-for-human` because `/sim`, Play Mode physics, player build, and feel gates require the documented Unity license/WebGL setup and an authorized isolated runtime.
