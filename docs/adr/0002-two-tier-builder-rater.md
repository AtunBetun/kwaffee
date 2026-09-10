# ADR-0002: Two-tier BUILDER/RATER loop with budget discipline

## Status

Accepted.

## Context

This project is built by frontier AI sessions. There are two compute tiers: a cheap one (the BUILDER's runtime — DeepSeek V4 Flash via Oh My Pi) and a scarce expensive one (the RATER's runtime — GPT 6 Astra). Blasting the expensive tier on every edit would exhaust the budget. Quality still needs an adversarial judge on feel/fun/voice that cheap metrics can't supply.

## Decision

- **Asymmetry is the budget.** The BUILDER produces nearly all tokens; the RATER reviews rarely and briefly (hard ≤200-line output cap). The expensive tier never writes code.
- **Escalation ladder per artifact, never skipped:** (1) BUILDER implements in a cheap session. (2) The `/sim` metrics harness runs; numbers recorded. (3) BUILDER self-reviews against spec, fixes what it finds. (4) Only then an expensive RATER review — batched findings, ONE review per cycle, no ping-pong. (5) BUILDER fixes everything in one cheap pass. (6) Repeat until the artifact gate passes.
- **RATER budget ceiling:** at most 2 review cycles per artifact; at most 1 per full-session evaluation. A failing 2nd review forces a re-read of DECISIONS and a re-approach — not another review of the same approach.
- **`sim` harness answers numbers first.** Balance and pacing are decided by data (chaos events/min, downtime, shift length, tip economy, theft rate, NPC reaction latency); expensive judgment is reserved for what numbers cannot answer: feel, fun, voice.
- **The expensive tier is also the architecture rationalizer when a decision genuinely changes the stack** (e.g. the original Three.js→Unity flip), and should be spent there once.

## Consequences

- Cheap sessions carry the project; the expensive tier acts as a rare, strict quality incident.
- Files `/EVIDENCE.md` + `/DECISIONS.md` are the memory: every session starts by reading them, works the next unpassed artifact, ends by appending. Never re-derive settled decisions in a fresh blank session.
- If the RATER passes anything on the first review, the RATER is not being adversarial — restart with a stricter prompt.

## Alternatives considered

- Use the expensive tier for everything — rejected: burns the budget in hours, and quality review is diluted by volume.
- Cheap tier only — rejected: no adversarial gatekeeper, ships "compiles but slop".
- Equal weighting — rejected: unnecessary spend; the numbers-first ladder exists precisely to spend expensively only where judgment is genuinely required.