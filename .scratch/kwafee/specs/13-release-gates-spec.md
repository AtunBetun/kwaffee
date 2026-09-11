# Artifact 13 — Full-game approval and release

Beads id: `kwaffee-4k0` · Grilled memo: `.scratch/kwafee/grilling/13-release-gates.md`

This is the release gate. It is **blocked by all of Artifacts 1-12**. It contains zero new game code, zero new art. It decides who judges the finished game, on what evidence, in what order, and what ships.

## Problem Statement (from the user's perspective)

I have built all 12 artifacts and they each passed their gates. But I still cannot call the game done: I do not have a defined, adversarial, un-fakeable way to approve the *whole* finished game, and I do not have a fixed order of operations for what must close before I can honestly say "KWA FEE ships." Right now the approval rule is contradictory (three full-session passes required, but the review budget caps at two per artifact), there is no evidence standard that prevents fabricated results, and no release checklist that stops me from shipping too early or with a hole. I need a hard, measurable gate: who judges, on what captured evidence, in what order, and what exact artifact set releases — with no waivers, no silent scope cuts, and no claimable state until every prior artifact queue is closed.

## Solution (user perspective)

A release process that cannot be gamed and cannot be claimed early. The whole finished game is judged by the RATER over three distinct slow-tier full-session passes, each on a separate session, each against one continuous unedited captured recording plus a seed-matching SimHarness sidecar. No human waiver, no self-review, no config review. A hard checklist runs in a fixed order: all 12 tickets closed → evidence audit → code freeze → three passes → both builds green + launch note → release. Releasing is structurally impossible while any artifact ticket is open, and a partial release (desktop only, or WebGL as a patch) is never claimed. When done, a ≤10-line launch note says what KWA FEE is, and ships as both a desktop Build-1 and a WebGL Build-2 launch URL. This artifact adds no Blender art.

## User Stories

1. As the BUILDER, I want the three full-session passes to be three distinct slow-tier RATER sessions (one full-session review per session), so that the three-pass requirement and the two-per-artifact review cap both hold with zero waiver.
2. As the BUILDER, I want the two-review cap to still bind within a single review cycle (per approach), so that two failing full-session reviews on the same approach force a re-read of DECISIONS and a reapproach rather than a waived third.
3. As the BUILDER, I want to know that self-reviews, code-review-skill verdicts, and two-axis adversarial reviews never count toward the three passes, so that only genuine RATER verdicts release the game.
4. As the BUILDER, I want each pass to be judged against a captured recording rather than live play by the RATER, so that the read-only RATER tier never touches the runtime and can scrub and rewind the evidence.
5. As the BUILDER, I want each pass's evidence to be one continuous unedited screen capture (single file, checksummed, on-screen FPS counter) of a complete shift, so that cut or fabricated footage is impossible.
6. As the BUILDER, I want each recording to carry a seed-matching SimHarness sidecar metrics JSON, so that recording and metrics provably come from the same session and a mismatch blocks the pass.
7. As the BUILDER, I want each session to declare its human/bot composition header, so that honest labeling replaces faking and bots-only sessions cannot earn top Fun/NPC scores.
8. As the BUILDER, I want at least one of the three passes to include ≥2 real humans, so that the game's live fun is proven by real people, not only sims.
9. As the BUILDER, I want release to be unclaimable while any of the 12 artifact tickets is open, so that the gate is structural (Beads dependencies) and not a verdict-time check.
10. As the BUILDER, I want the checklist to run in fixed order — all 12 closed, evidence audit, code freeze, three passes, builds + launch note, release — with each step gated on the previous, so that a release cannot skip its prerequisites.
11. As the BUILDER, I want the code freeze to block gameplay/art/audio/netcode commits during the three-pass window, so that all three passes judge the same build and "consecutive" is meaningful.
12. As the BUILDER, I want a hard ≤10-line launch note with a fixed content contract, so that the release says what KWA FEE is and stops, in the game's voice.
13. As the BUILDER, I want both builds — desktop Build-1 (evidence fixture, buildable now) and WebGL Build-2 (launch URL) — to be required for release, so that a partial release is never claimed.
14. As the RATER, I want each full-session verdict capped at ≤200 lines with evidence citations, so that brevity and traceability hold at the release gate exactly as they do per artifact.
15. As the RATER, I want the evidence audit to verify entries are monotonic, failures included, and every claimed metric traceable to a recorded artifact, so that no fabricated number survives.
16. As the BUILDER, I want the human ladder evidence to ride alongside from artifact 11's protocol, so that a fresh player's "URL → laughing ≤60s → again again" is on file.
17. As the human owner, I want the Unity Personal license and WebGL module to be the only human prerequisites, parked as "human prerequisite pending" with all non-blocking work proceeding, so that batchmode licensing is never retried and tokens are not wasted.

## Implementation Decisions

- **Three passes = three distinct slow-tier sessions.** The PROMPT requires ≥3 full-session passes; ADR-0002 caps the RATER at 2 reviews per artifact and at 1 full-session review per session. Resolved (A): the cap is per-session, so three full-session passes span three distinct slow-tier RATER sessions by construction. Each session appends one full-session review to `/EVIDENCE.md`. No human waiver of the third pass exists; option (B) is rejected because it lets the weakest judgment release the game at exactly the moment proof matters most.
- **Per-cycle cap still binds.** Within a session's review cycle, the two-review cap holds: two failing full-session reviews on the same approach → re-read DECISIONS and reapproach; further passes resume on the new approach, never on a waiver.
- **What counts as a pass.** Only RATER verdicts count. BUILDER self-review, the code-review-skill verdict, and the two-axis adversarial review (all flash-tier) never count. Each counted verdict: slow-tier RATER, ≤200 lines, evidence citations.
- **Evidence standard per pass (all three).** (1) One continuous, unedited screen capture — no cuts, single file, checksum recorded in EVIDENCE.md — of a complete shift: open → shift → catastrophe replay → awards → tabloid, on-screen FPS counter for the whole file. (2) Sidecar SimHarness metrics JSON from the seeded run of the same session (seed, chaos/min, downtime, shift length, tip economy, theft rate, NPC reaction latency), checksummed, hash beside the pass entry; recording and metrics must agree on seed — mismatch is a blocker (fabrication gate). (3) Declared human/bot composition header; bots-only sessions cap Fun/Boston Voice/NPC Aliveness at ≤5. At least one of the three passes must include ≥2 real humans. RATER never plays live.
- **Recording toolchain.** The machine-capture at real 60fps is the one piece of new tooling this ticket adds; it is prepared and smoke-tested before session 1. A broken recorder invalidates an expensive pass slot.
- **Checklist ordering (hard).** 1) `bd` proves all 12 artifact tickets `closed` (query output committed to EVIDENCE.md; blocker if any is open). 2) EVIDENCE.md audit: every artifact shows its gate — ≥2 consecutive ≥8/10, 0 blockers, 0 majors, ≥3 real RATER passes including the sessions-span rule — plus the human ladder card and the 60fps/4-player evidence. 3) Code freeze from claim: only EVIDENCE.md and release-note commits during the pass window. 4) Sessions 1→3 recorded and reviewed, one per session. 5) Both builds green + launch note committed + final truthful-entry check. 6) Release artifact set assembled and published.
- **Structural claim precondition.** `kwaffee-4k0` encodes "Blocked by: 12" in its body AND as Beads dependencies, so `bd` refuses any open/claim transition while 12 artifact tickets are open. Status flow: `open → claimed → in_progress (passes 1-3) → released`. Never `claimed` with a sibling ticket open; `in_progress` commits limited to EVIDENCE.md + release note.
- **Launch note.** Hard ≤10 lines, committed as `README.md` top section "LAUNCH NOTE", line count machine-verifiable. Content contract enforced by RATER: (1) title, (2) player count + co-op/grief framing, (3) the five verbs, (4) the loop (serve/quota), (5) the voice (Vinny/Boston; no compliments), (6) replay+awards delivery axis, (7) at least one world feature (Mule/dealers), (8) pacing, (9) platforms, (10) sign-off in voice.
- **Release artifact set.** Desktop = Build-1, the evidence fixture and first buildable artifact (MacStandaloneSupport present), unblocked once the human activates the Personal license. WebGL = Build-2, the public launch URL, pending the WebGL Build Support module. Release requires BOTH; a partial release is never claimed. Non-goal: "release with desktop now, WebGL as patch" — that is silent scope shrinking of DONE item 4.
- **Human prerequisites (out-of-loop).** Unity Personal license activation and WebGL Build Support module are the only human steps; never retry batchmode licensing. Park on "human prerequisite pending"; all reachable work (recordings that don't need the module, note, evidence audit) proceeds.
- **Determinism.** SimHarness seed must reproduce; any runtime nondeterminism (unseeded RNG, wall-clock drift) invalidates the sidecar metrics contract — fixed at the source, not in the evidence.
- **WebGL renderer risk.** Lit/URP materials + 12-mesh scene must render on WebGL's GL; unverified until the module lands. Post-module WebGL build problems are fixable within the freeze ONLY if build/renderer issues, not gameplay deltas; otherwise the human reopens the freeze decision.
- **Blender art needs: NONE.** This artifact is pure process/gate/release work — evidence, review passes, builds, launch note. It produces no visible assets, authors no models, imports nothing. Artifacts 7/9 carry the art; 13 only verifies their gates closed.
- **External dependency.** If the memo references a pre-existing ticket `kwaffee-6id` / "shift-seam" (a separate refactor owning the Shift.Command seam), record it as an external dependency; do not absorb its work here.

## Testing Decisions

- **What a good test is here:** the release gate is verified by machine-checkable evidence and adversarial review, not by unit tests. The meaningful "tests" are the gate checklist's blockers, each of which fails loudly and blocks release.
- **EVIDENCE.md audit** is the primary review: entries monotonic, failures included, no fabricated numbers, every claimed metric traceable to a recorded artifact. Any untraceable number is a blocker.
- **Fabrication gate:** recording and sidecar metrics must agree on seed; cut footage, missing metrics, or seed mismatch are blockers.
- **Consecutive-passes gate:** the three passes span ≥3 distinct sessions and are 3 consecutive passes; a failed full-session pass sitting between two ≥8 passes is a blocker.
- **Human gate:** at least one pass has ≥2 real humans; bots-only across all three, or undisclosed bot substitution, is a blocker.
- **Composition gate:** bots-only sessions cannot score above 5 on Fun/Boston Voice/NPC Aliveness.
- **Performance gate:** p95 FPS ≥ 58 at 4 players on both builds during a full shift including catastrophe replay, measured from the recordings; any sustained drop below 60 or GC-storm artifact is a blocker.
- **Code-freeze gate:** replayable git log shows no gameplay/art/audio/netcode commits between claim and release; any content diff between passes 1 and 3 is a blocker.
- **Prior art:** the human ladder protocol and feedback card from artifacts 8/11; the `/sim` SimHarness output format. This artifact inherits and gates on them; it does not re-create them.

## Out of Scope

- Any new game code, gameplay, art, audio, or netcode. This ticket builds nothing.
- Any Blender art or visible assets (Artifacts 7/9 carry the art; 13 only verifies their gates closed).
- Live play by the RATER — explicitly out; recordings plus the mandatory live human ladder component are the evidence.
- Any waiver path: no human waiver of a pass, no re-roll, no mulligan.
- Partial release: "desktop now, WebGL as patch" is explicitly a non-goal.
- Reworking any artifact's own gate, the launch-note prose itself beyond the content contract, or the shift-seam refactor (external dependency if present).
- Retrying batchmode Unity licensing or any human prerequisite.

## Further Notes

- **The RATER is read-only and never touches the runtime** (ADR-0002); the recording is the scrutiny surface and is strictly stronger than a single live pass because it can be scrubbed and rewound.
- **Recording is a scripted one-command capture, not infrastructure** (friendslop default).
- **The human ladder's live human is the live component and is mandatory**; it cannot be synthesized or sped up.
- **Budget discipline:** 3 slow-tier sessions minimum; ADR-0002 allows at most 1 full-session review per session. Budget exhaustion is a schedule risk, never an excuse to waive a pass.
- **Assumption stated loudly:** "consecutive" full-session passes mean consecutive verdicts from real sessions against the same frozen build; the code freeze is the load-bearing mechanism, and the RATER is expected to diff the build being reviewed against the frozen commit.
- **The human owner's playtest cohort** provides the ≥2 humans for at least one session recording; cannot be synthesized.

## Refined Gate

Release is claimed only when ALL of the following hold (checklist in this artifact's language):

1. `bd` shows zero open artifact tickets (1-12 all `closed`); query output committed to EVIDENCE.md. (Blocker if any artifact open.)
2. EVIDENCE.md contains ≥3 full-session RATER pass entries, each: slow-tier RATER verdict, ≤200 lines, 0 blockers, 0 majors, ≥8/10 on all six dimensions (Fun, Feel, Coherence, Boston Voice, NPC Aliveness, Visual Identity), with cited evidence (file/recording/line). Any pass not from a slow-tier RATER session is a blocker.
3. The three passes span ≥3 distinct sessions (one full-session review per session) and are 3 consecutive passes — no failed full-session pass interleaves between them. (Blocker: a failed pass sits between two ≥8 passes.)
4. At least one of the three passes has ≥2 real humans in the session; composition header recorded. (Blocker: bots-only across all three, or undisclosed bot substitution.)
5. Every session recording: one continuous unedited capture (checksummed), on-screen FPS counter, seed-matching sidecar SimHarness metrics JSON. (Blocker: cut footage, seed mismatch, missing metrics.)
6. Performance: p95 FPS ≥ 58 at 4 players on both builds during a full shift including catastrophe replay, measured from the recordings. (Blocker: any sustained drop below 60, any GC-storm artifact.)
7. Human ladder on file: fresh player, URL or LAN link, laughing ≤60s, zero instruction, "again again", feedback card recorded. (Blocker: missing card or no evidence of a real human.)
8. Code freeze holds: replayable git log shows no gameplay/art/audio/netcode commits between claim and release. (Blocker: content diff between passes 1 and 3.)
9. WebGL build produces a playable URL and desktop build produces a runnable artifact; both listed in the release set. (Blocker: either missing.)
10. Launch note committed: ≤10 machine-verified lines, all content slots present. (Blocker: line count over, missing title/verbs/voice/platform.)
11. EVIDENCE.md audit: entries monotonic, failures included, no fabricated numbers; every claimed metric traceable to a recorded artifact. (Blocker: any untraceable number.)
12. Release set assembled: WebGL URL, desktop artifact + launch script, README launch note, EVIDENCE.md final state, human feedback card. Ticket then `released`.
