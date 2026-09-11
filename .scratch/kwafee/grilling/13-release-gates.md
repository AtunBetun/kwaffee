# Artifact 13 — Full-game approval and release

- Beads id: `kwaffee-4k0`
- Grilled scope: the release gate — who judges, on what evidence, in what order, and what ships. This ticket is **blocked by all of Artifacts 1-12**; it is a process/gate/release ticket and contains zero new game code, zero new art.

## Design tree (decisions resolved)

### 1. Contradiction: PROMPT requires ≥3 RATER passes per artifact, PROMPT+ADR-0002 cap RATER at 2 reviews per artifact
Options: (A) read the cap as per-SESSION — the 3+ passes span 3 distinct sessions; (B) treat the cap as the spending ceiling — gate closes on 2 consecutive ≥8/10 passes, a human waives the third.
**DECIDED: (A).** ADR-0002's sibling rule "at most 1 review per full-session evaluation" makes 3 full-session passes across 3 sessions the only reading under which both rules hold with zero waiver — and (B) makes the weakest, least-adversarial judgment ever the one that releases the game, saving one expensive review at exactly the moment proof matters most.
Refinements locked in:
- The 3 full-session passes are **distinct slow-tier RATER sessions** (GPT 6 Astra, `slow` role), appending to `/EVIDENCE.md`. One full-session review per session, so the 3-pass window spans ≥3 sessions by construction.
- The per-artifact "2 reviews" cap still binds **within a session's review cycle** (i.e., per approach). Two failing full-session reviews on the same approach → re-read DECISIONS + reapproach; further passes resume on the new approach, not on a waiver.
- **Never** does a BUILDER self-review, the code-review-skill verdict, or the two-axis adversarial review (all flash-tier, see EVIDENCE 2026-09-10) count toward the 3. The 3 are RATER verdicts only, each ≤200 lines, each with evidence citations.

### 2. Full-session pass fixture: live play or captured recordings — evidence standard
Options: RATER plays live through the runtime; RATER reviews captured recordings (video + sidecar metrics); hybrid.
**DECIDED: captured recordings, with a mandatory declared composition and anti-fabrication checks.** The RATER tier is read-only (ADR-0002: never touches the runtime), live play adds scheduling/tooling cost with zero evidential gain, and a recording can be scrubbed and rewound — strictly stronger adversarial scrutiny than one live pass. Friendslop: recording is a scripted one-command capture, not infrastructure.
Evidence standard per pass (all three passes):
- **One continuous, unedited screen capture** (no cuts; single file, checksum recorded in EVIDENCE.md) of a complete shift: open → shift → catastrophe replay → awards → tabloid. FPS counter on-screen for the whole file.
- **Sidecar metrics JSON** from the seeded SimHarness run of the same session (seed, chaos/min, downtime, shift length, tip economy, theft rate, NPC reaction latency), checksummed, hash recorded next to the pass entry. Recording and metrics must agree on seed — a mismatch is a blocker (fabrication gate).
- **Declared human/bot composition header** (e.g. `players: 2 human + 2 sim`). Bots-only sessions do not get Fun/Boston Voice/NPC Aliveness scores above 5; at least one of the three passes MUST include ≥2 real humans (owner's authorized playtest cohort). Honest labeling beats faking.
- Human ladder evidence (spec DONE #3) rides alongside, from artifact 11's protocol: URL or LAN link → laughing ≤60s → "again again", feedback card on file.
Live play by the RATER is explicitly OUT. The ladder's live human is the live component and is mandatory.

### 3. Launch checklist ordering — all 12 artifact gates first
Options: gate 13 on 12 open tickets slowly closing under it; gate 13 on all 12 closed; allow partial (some artifacts deferred to post-release patches).
**DECIDED: hard order — release cannot be claimed with a single artifact ticket open.**
Order within the ticket (each step gated on the previous):
1. `bd` query proves all 12 artifact tickets are `closed` (evidence: query output committed to EVIDENCE.md).
2. EVIDENCE.md audit pass: every artifact shows its gate (≥2 consecutive ≥8/10, 0 blockers, 0 majors, min 3 real RATER passes incl. the sessions-span rule from decision 1), the human ladder card, and the 60fps/4-player evidence.
3. **Code freeze:** from claim of this ticket, no gameplay/art/audio/netcode commits during the 3-pass window — only EVIDENCE.md and release-note commits. Three passes must judge the same build; "consecutive" is meaningless otherwise.
4. Sessions 1→3 recorded and reviewed (decision 2), one per session.
5. Both builds green (decision 5) + launch note committed (decision 4) + final EVIDENCE.md truthful-entry check.
6. Release artifact set assembled and published (decision 5).

### 4. The 10-line launch note
Options: no note (release speaks for itself); a long README; a hard 10-line note.
**DECIDED: a hard ≤10-line note, committed as `README.md` top section "LAUNCH NOTE", line count machine-verifiable, content contract enforced by RATER.** Per spec: "say what KWA FEE is. Stop." Draft candidate (adopt or rewrite; must keep every content slot, must stay ≤10 lines):
```
KWA FEE is a raucous party game for 3-4 friends: you run a barely-legal
Boston coffee shop and absolutely wreck each other while doing it.
The whole game is five verbs: fling, chug, fix, steal, sabotage.
Every kehd who walks in is another chance to serve, betray, or blame the cat.
The city never sleeps and never forgives — quotas, health inspectors,
vendettas, and a landlord with a shame list.
Every catastrophe gets replayed in slow motion and roasted by Big Vinny,
who has never once been impressed.
Beans run dry? Drive the Mule, haggle with Ma Paddy, and watch out for the pigeon.
Shifts are 3-4 minutes. The only rule is that it ends funny.
Play it in your browser (WebGL) or over LAN with your friends (desktop).
KWA FEE. Go on, get outta heah. Shift's ovah.
```
Content contract: (1) title, (2) player count + co-op/grief framing, (3) the five verbs, (4) the loop (serve/quota), (5) the voice (Vinny/Boston; no compliments), (6) replay+awards delivery axis, (7) at least one world feature (Mule/dealers), (8) pacing, (9) platforms, (10) sign-off in voice. RATER checks line count ≤10 and each slot present.

### 5. Release artifact set — which ships first with WebGL module pending
Options: ship WebGL only (browser party = primary), desktop only (LAN = what's buildable now), both with WebGL first, both with desktop first.
**DECIDED: desktop is Build-1 and the evidence fixture; WebGL is Build-2 and the launch URL; the release requires BOTH, and a partial release is never claimed.**
- Desktop (MacStandaloneSupport present) is the recording/ladder fixture and the first buildable artifact once the human activates the Personal license — it unblocks sessions immediately, no module wait.
- WebGL (WebGL Build Support module pending) is the public launch target; a release without a playable URL is not a release, so ship state is only asserted when both are green.
- Explicit non-goal: no "release with desktop now, WebGL as patch" — that is silent scope shrinking of DONE item 4.
- The human module/license prerequisite is out-of-loop (never retry batchmode); the ralph loop parks on "human prerequisite pending", all reachable work (recordings that don't need the module, note, evidence audit) proceeds.

### 6. Ralph-loop sequencing — 13 must not be claimable until every artifact queue closes
Options: claimable early with gate check at verdict time; claimable only when all prior queues are empty.
**DECIDED: claim precondition is structural, not a verdict-time check.** Ticket `kwaffee-4k0` encodes its dependency in the body ("Blocked by: 12"); the loop MUST encode the same as Beads dependencies so `bd` refuses any open/claim transition while 12 artifact tickets remain open. Claim additionally requires: zero open artifact tickets, code freeze declared, and the freeze respected (see decision 3). Rule for the ralph loop: 13's status goes `open → claimed → in_progress (passes 1-3) → released`; it may never be `claimed` with a sibling ticket open, and `in_progress` commits are limited to EVIDENCE.md + release note by construction.

## Refined acceptance criteria (gate checklist, in this artifact's language)

1. `bd` shows zero open artifact tickets (1-12 all `closed`); query output committed. (Blocker if any artifact open.)
2. EVIDENCE.md contains ≥3 full-session RATER pass entries, each: slow-tier RATER verdict, ≤200 lines, 0 blockers, 0 majors, ≥8/10 on all six dimensions (Fun, Feel, Coherence, Boston Voice, NPC Aliveness, Visual Identity), with cited evidence (file/recording/line). Any pass not from a slow-tier RATER session is a blocker.
3. The three passes span ≥3 distinct sessions (one full-session review per session) and are 3 consecutive passes — no failed full-session pass interleaves between them. (Blocker: a failed pass sits between two ≥8 passes.)
4. At least one of the three passes has ≥2 real humans in the session; composition header recorded. (Blocker: bots-only across all three, or undisclosed bot substitution.)
5. Every session recording: one continuous unedited capture (checksummed), on-screen FPS counter, seed-matching sidecar SimHarness metrics JSON (chaos/min, downtime, shift length, tip economy, theft rate, NPC latency). (Blocker: cut footage, seed mismatch, missing metrics.)
6. Performance: p95 FPS ≥ 58 at 4 players on both builds, during a full shift including catastrophe replay; measured from the recordings. (Blocker: any sustained drop below 60, any GC-storm artifact.)
7. Human ladder on file: fresh player, URL or LAN link, laughing ≤60s, zero instruction, "again again", feedback card recorded. (Blocker: missing card or no evidence of a real human.)
8. Code freeze holds: replayable git log shows no gameplay/art/audio/netcode commits between claim and release. (Blocker: content diff between passes 1 and 3.)
9. WebGL build produces a playable URL and desktop build produces a runnable artifact; both listed in the release set. (Blocker: either missing.)
10. Launch note committed: ≤10 machine-verified lines, all content slots present (decision 4). (Blocker: line count over, missing title/verbs/voice/platform.)
11. EVIDENCE.md audit: entries monotonic, failures included, no fabricated numbers; every claimed metric traceable to a recorded artifact. (Blocker: any untraceable number.)
12. Release set assembled: WebGL URL, desktop artifact + launch script, README launch note, EVIDENCE.md final state, human feedback card. Ticket then `released`.

## Blender art needs

**NONE.** This artifact is pure process/gate/release work: evidence, review passes, builds, launch note. It produces no visible assets, authors no models, imports nothing. (Artifacts 7/9 carry the art; 13 only verifies their gates closed.)

## Dependencies + risks

- **Inputs (hard):** every Artifact 1-12 ticket closed with evidence; their EVIDENCE.md entries; the `/sim` SimHarness output format; the human ladder protocol and card from artifacts 8/11; the existing Unity project (`/Users/albertodesaintmalo/work/kwaffee/kwaffee`, Unity 6000.6.0f1) and Blender pipeline state.
- **Human prerequisites (blocking, out-of-loop):** Unity Personal license activation (every batchmode build), WebGL Build Support module install (WebGL build specifically). Never retry batchmode licensing. Parked state is not failure.
- **Human playtest cohort:** one fresh player for the ladder and ≥2 humans for at least one session recording — cannot be synthesized, cannot be sped up.
- **RATER budget:** 3 slow-tier sessions minimum; ADR-0002 gives no more than 1 full-session review per session. Budget exhaustion is a schedule risk, not an excuse to waive a pass (decision 1 forbids waivers).
- **Determinism drift:** SimHarness seed must reproduce; any nondeterminism in the runtime (unseeded RNG, wall-clock drift) invalidates the sidecar metrics contract. Fix at the source, not in the evidence.
- **WebGL renderer compatibility:** Lit/URP materials + 12-mesh scene must render on WebGL's GL; unverified until the module lands. Mitigation: treat WebGL build problems discovered post-module as fixable within the freeze ONLY if they are build/renderer issues, not gameplay deltas; otherwise the freeze decision is re-opened by the human.
- **Recording toolchain:** machine capture at a real 60fps for a 3-4 min session is the new tooling this ticket adds; prepare and smoke-test it before session 1 — a broken recorder invalidates a pass slot (expensive tier burned for nothing).
- **Assumption stated loudly:** "consecutive" full-session passes mean consecutive verdicts from real sessions, judged against the same frozen build; the code freeze (criterion 8) is the load-bearing mechanism, and the RATER is expected to diff the build being reviewed against the frozen commit.