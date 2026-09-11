# KWA FEE — Artifact 11 Spec: Lobby & Onboarding

- Beads id: `kwaffee-vau` (blocked by: `kwaffee-7xn` / Artifact 10 netcode)
- Scope: room codes, name pick, ONE-SCREEN tutorial with a funny first action, room backend, 60-second laughter gate protocol.
- Ladder: comedy over fidelity, shrink not expand. Any screen between URL and first laugh is a tax on the 60-second gate.

## Problem Statement (from the user's perspective)

I open a link my friend sends me at a party. I have never played KWA FEE and nobody is going to read me instructions over the music. I should laugh inside a minute, without typing anything, without a menu, without a tutorial screen. My name should be picked in one tap and be funny on its own. And my other friends should be able to get in with nothing more than a four-letter code shouted across the room.

## Solution (user perspective)

Opening the URL drops you straight into the shop, a cup already wobbling in your hand, facing Danny the Regular whose bubble reads "FLING ME MY KWA FEE. HURRY." You hold and release — he gets it in the face, googly eyes cross, the whole thing replays in slow motion, Vinny's banner says it: "OW. Wicked splahst, kehd." That is the whole tutorial: five scripted beats, one per verb, each one funny, none of them a menu. The code chip and name bar live on the same candy HUD. Your name is one tap on a Boston name chip ("RANDO" if you can't decide). The host copies "SEND THE LINK, KEHD"; friends type a 4-letter code into one chunky bar when they don't have the link. No accounts, no menus, no settings, no skip. Nobody reads anything.

## User Stories

1. As a fresh player, I want URL → straight into the shop with a cup in my hand, so that I am performing the game's one real gesture inside 3 seconds without any instruction.
2. As a fresh player, I want the first thing I fling to splat the named customer in the face with a slow-mo replay and a Vinny line, so that my first action is funny no matter how badly I timed the release.
3. As a fresh player, I want the tutorial to teach each of the five verbs with a scripted in-world joke (face-splat, jitter ragdoll, cat outrage, fixed-with-a-clatter, steam yelp), so that I learn the whole game by laughing, never by reading keys.
4. As a fresh player, I want the shop cat to handle the first beat if I freeze for 8 seconds, so that the game is funny even if I touch nothing.
5. As a fresh player, I want zero "how to play" overlays, key hints, or help screens, so that nothing between me and the next laugh can flatten the mood.
6. As a fresh player, I want my name to be one tap on a preset chip or a "RANDO" shuffle, with no keyboard, so that I am never blocked by typing, moderation, or an indecision cursor.
7. As a fresh player, I want my name to float above my head next to my googly eyes, so that my identity is a joke I picked in one tap.
8. As a returning player, I want to land straight in the room in the ready state with the tutorial already behind me, so that the one-time script is never a second-run tax.
9. As the host at the party, I want a primary "SEND THE LINK, KEHD" button and a giant 4-letter candy code chip beside it, so that every way I invite friends is copy-paste or shoutable.
10. As a friend without the link, I want one single field — "GOT THE CODE, KEHD?" — that takes a 4-letter code, so that voice-only invites still work at party volume.
11. As a friend who fat-fingers a code, I want Vinny's line and a card shake instead of an error dialog, so that even failure stays in the room's voice.
12. As any player, I want only one screen between load and shift-ready, so that the lobby, tutorial, and pre-match room are the same place and nothing reads as a menu.
13. As a playtester, I want the whole first-use tutorial to take under 60 seconds end to end, so that the laughter gate is physically reachable inside its own protocol.
14. As a playtester, I want a recorded fresh player laughing within 60 seconds and still playing, so that the artifact's gate is evidenced, not claimed.
15. As an engineer, I want the tutorial to compose existing Barista/Cup/Customer/Counter/Machine/Cat prefabs and existing verb rules, so that no new simulation code or art pipeline work hides in onboarding.
16. As an engineer, I want the room backend to be one hosted server process mapping room codes to sessions, so that room isolation is state keying, not process juggling.
17. As the Shop Cat, I want to parade past with a tipped tray and get robbed on cue, so that the STEAL beat has a victim that cannot complain to a manager.

## Implementation Decisions

### D1. One screen, zero menus
- Exactly one scene exists between URL load and first shift-ready state. The pre-match room IS the tutorial IS the lobby. The only additional surface is the code input bar and the name chip row on that same scene (both are candy HUD, not menus).
- Any proposed screen or modal that sits between URL and the first FLING is rejected on sight. Room-not-found, invalid code, and connection failure all render as Vinny line + card shake inside the scene; no dialog boxes anywhere.
- Returning players (first-shift flag set) spawn in the room in the ready state. That flag is the only "skip" in the artifact.

### D2. Room backend — hosted relay
- One headless Unity authoritative server process hosts many rooms: a session map `room code → RoomState` where the room code IS the session id. One process = one deployable = laziest ops; room isolation is state keying.
- Server runs in the container/VM per the security rule. Desktop staging may launch a local server process with the identical room-code flow as a staging shortcut while the WebGL prereq is pending — never as the final state, because LAN cannot satisfy the WebGL gate.
- Room lifecycle: a room spawns with the first join request, persists while occupied, teardown on last leave is Artifact 10/ops territory. Reconnect = reloading the URL returns to the room ≤5s (contract with Artifact 10).

### D3. Wire transport
- The lobby surface is transport-agnostic. It assumes Artifact 10's relay; this spec writes calls as connect/join/send/receive against a session handle. If GrillNetcode lands WebRTC or raw UDP instead of WebSocket, only the connect call changes — chips, copy, name bar, and the one-screen rule do not.
- Room-join API surface (posted upstream to Artifact 10 as open question): `CreateRoom() → { code, sessionHandle }`, `JoinRoom(code) → { sessionHandle | invalid }`, URL form carries the room (e.g. `…/?room=ABCD`); clients resolve the URL's room before the scene loads. The room code is the human-pass fallback, never the primary.
- Remote browser play requires wss + TLS on a reachable host (ops risk, flagged in Further Notes).

### D4. Room code
- 4 uppercase chars from the confusable-free alphabet `{A,C,D,E,F,H,J,K,M,N,P,R,T,U,V,W,X,Z}`. Shoutable at party volume, unambiguous to remember-and-retype.
- Host-facing copy: primary button "SEND THE LINK, KEHD" (copies URL, clipboard has no UI), giant candy code chip beside it with a copy icon, secondary line "or just OH-vah the code".
- Friend-facing copy: one input, "GOT THE CODE, KEHD?" — chunky candy bar, bounce on focus. Invalid code → Vinny line ("What's that s'pposed ta be?") + card shake. Case-insensitive input, normalized to uppercase.
- The first player never types: the URL is the room. This guarantees the 60s path contains no text-entry step.

### D5. Name pick
- ≥12 preset Boston-flavored name chips + a "RANDO" shuffle button. Zero free text. No keyboard, no profanity moderation, no rage-typing delay — names are their own first joke (Two-Timmy, Dunkin' Donna, Vinny Jr., The Kehd, Big Nose Sal, Chowdah Charlie, Bunny from Braintree, Ricky from Revere, Slaps, Cah-Mon Derek, plus reserve chips to reach and hold ≥12).
- Selection persists for the session in a client-side session profile `{ name: string }`. "RANDO" selects a different name than the currently selected one. The name floats above the barista's head — googly eyes carry the soul, name carries the ego (LOOK rule 2).

### D6. First action — funny by construction
- On scene load the player's barista holds a cup, already in-hand and wobbling. Anything wiggling in front of you gets clicked — that is the only nudge.
- First valid input = FLING gesture (hold-to-charge, release-to-launch). ANY release timing or charge lands the same scripted outcome: cup splats Danny the Regular in the face — squash, splash, googly eyes cross, slow-mo micro-replay (Artifact 8's replay system, the laugh driver), Vinny banner + TTS "OW. Wicked splahst, kehd." The charge still feels real; only the outcome is scripted, and only for the first fling.
- Demand bubble is in-world customer speech ("FLING ME MY KWA FEE. HURRY."), the artifact 6 pattern proven readable. Zero instruction text anywhere.
- Fail-safe: no input within 8s → the Shop Cat walks over and bats the cup into Danny's face. Comedy with zero input; the replay re-laughs it.

### D7. Five verbs, five scripted beats
- Five ordered beats, ~8-10s each, ~45-60s total, one screen. Each beat is a scripted micro-outcome tied to a verb; first use produces comedy, never a checkmark:
  1. FLING — Danny face-splat (D6).
  2. CHUG — Danny offers a spare cup; chug → 1s jitter ragdoll + Vinny line.
  3. STEAL — the cat parades past carrying a tipped tray; bubble "Rob the cat."; swipe → cat outrage.
  4. FIX — Grinder belch + smoke; bubble "Give it the wrench."; fixed with a clatter.
  5. SABOTAGE — Steam Wand idles next to Danny; bubble "Warm him up."; steam + Danny yelp.
- Beats are scripted and deterministic — never seeded-random — so "funny by construction" cannot roll a non-funny outcome. Seeded RNG resumes after first shift (global game rule).
- Bubbles are proximity-triggered, one live at a time (sequential beats keep one screen from becoming a 3am billboard). Vinny prods with rotating non-instructional lines after 5s idle on any beat.
- No skip button. One-time: the first-shift-start flag short-circuits the script for everyone else in the room; late joiners land ready.

### D8. Tutorial as composition
- The tutorial is a script over existing systems, not new simulation code. It reuses the Barista/Cup/Customer/Counter/Machine/Cat prefabs and the existing verb rules from Artifacts 1-7; beat state is a thin scripted state machine (beat id, trigger, outcome, next) over those verbs. Any future visible art need must route through the Blender bpy pipeline — none is required here.
- Danny = the existing Customer prefab with the parameterized material tint. No new mesh.

### D9. Candy UI, all code/SVG
- All lobby/onboarding visual surface is flat candy UI in SVG/code per LOOK rule 8: chunky rounded chips, candy gradients, giant icons, bounce on hover/focus. Zero Blender assets introduced by this artifact.
- Every string is Boston-voice (Vinny never compliments). Bubbles = in-world speech; HUD = candy chips; the code card shakes; the name chips bounce on focus.

### D10. Audio
- All tutorial cues come from Artifact 9's code-synthesized stings: splat, cat outrage, steam hiss, wrench clatter, Vinny TTS (styled text-banner + sting fallback, per the voice rule). No silent state anywhere in the one-screen flow.

### D11. Performance
- The lobby/tutorial holds 60fps with 4 baristas plus tutorial physics (shop cat, cup wobble, steam). Pooled/reused props, no per-frame allocation, no GC storms — global performance rule. Frame-time log goes in evidence; regressions are blockers.

### D12. Scope guardrails
- Cut entirely: accounts, login, friends lists, matchmaking, region select, voice chat, text chat, pause/settings menu, difficulty, tutorial skip, modal error screens, all "how to play" overlays, avatar customization (color paint pickers included). A party game's server browser is a URL.

## Testing Decisions

- **Human gate is the artifact's centerpiece test, not automation.** Protocol (fixed): (1) one real fresh player — never played KWA FEE; (2) tester hands over the URL (or opens it) and speaks exactly one instruction — "open it" — then touches nothing, points at nothing, gestures at nothing; (3) clock starts at page load; (4) screen+audio recording captured (OBS or phone); the video file joins EVIDENCE.md; no fabricated or impersonated human test, ever; (5) pass = audible laugh ≤60s from load AND continued engagement after the laugh — player still playing voluntarily past 60s ("again again" or re-engagement); a courtesy laugh that dies in 5s of stillness does not count; (6) laugh = audible vocalization of amusement, coded from the recording with a timestamp; (7) borderline → ruled by "did they keep playing"; (8) failure → redesign the failing beat, retest with a DIFFERENT fresh player; two data points minimum (the passing fresh player + a second fresh player confirming once the first passes). Second human in the room is allowed; the recorded subject is always the fresh player. Prior art: artifact 8's friend-laugh precedent (a real laugh outranks any metric).
- **Gate is un-claimable without a URL.** The 60s gate is not "done" until a real fresh player laughs at a real WebGL URL. Until the WebGL module lands, desktop local-server runs (identical lobby/tutorial surface) exercise beats and protocol, and that staging evidence is recorded as staging — never as the gate.
- **Scripted-beat determinism (unit/sim):** run each beat's script headless and assert the scripted outcome flags (splat happened, jitter ragdoll, cat outrage, clatter, steam yelp) with no randomness — outcome is a function of the beat, not the seed. Prior art: existing beetle verb rules; beats must not re-enter them through RNG.
- **Timer contract (sim + recorded runs):** full 5-beat tutorial ≤60s from first input, mean of 3 recorded tester runs; each run's timestamp log in evidence.
- **Fail-safe (sim):** idle client, no input; assert the Shop Cat performs beat 1 at 8s and the beat completes. This one test protects the zero-input comedy guarantee.
- **One-screen invariant:** session trace counts rendered scenes between load and first shift-ready; assert exactly one scene node and zero modal frames. Keep the existing EditMode test conventions green (21 EditMode tests are prior art for UI-state assertions).
- **Room code (unit):** generated codes are length-4, uppercase, membership in the confusable-free alphabet; generation over the session map is collision-free; invalid/normalized input → Vinny line + card shake path, never a dialog; reload-URL returns to room ≤5s (contract with Artifact 10 tests).
- **Name pick (unit):** ≥12 chips present; RANDO changes selection to a different name; selection persists across a scene reload within the session; chosen name renders over the barista.
- **Performance (sim/metrics):** frame-time log over a 4-barista tutorial run with cat + steam + cup wobble; assert 60fps budget with pooled props and no per-frame allocation churn. Regression = blocker.
- **DRY review gate:** tutorial must compose (script references existing verb rules and prefabs), not copy logic. Code review asserts no new simulation paths exist in the tutorial module — the RATER checks this against Artifacts 1-7.
- **Audio:** no silent state in the one-screen flow; stings ride the existing Artifact 9 fixtures.

## Out of Scope

- Accounts, login, friends lists, matchmaking, region select.
- Voice chat, text chat, pause/settings menu, difficulty levels.
- Tutorial skip button, replayable tutorial, "how to play" overlays, key-hint UI.
- Modal/dialog error screens — all failures are Vinny line + card shake in-scene.
- Avatar customization of any kind (color pickers included); the name + googly eyes are the identity.
- Any new Blender art — the artifact composes existing prefabs; Danny is a tint of the existing Customer. New visible art needs bpy pipeline, none required.
- LAN as the final hosting state (rejected; cannot satisfy the WebGL gate) — allowed only as staging while the WebGL module is pending.
- The Shift.Command seam / `kwaffee-6id` refactor: if that separate ticket owns the seam, it is an external dependency, not absorbed here; this artifact consumes whatever shift/verb entry points exist after it.

## Further Notes

- **Blocked states, stated loudly:** the 60s-gate is un-runnable until the Unity Personal license + WebGL Build Support module land and a URL exists. Never retry batchmode licensing (DECISIONS.md). Local-server desktop staging exercises the identical surface so beats and protocol ship before the module; only the URL delivery step waits.
- **Transport unset (Artifact 10 open question):** the lobby needs the room-code join API surface defined upstream; this spec assumes WebSocket relay. If netcode lands elsewhere, the connect call changes and nothing on the lobby surface does.
- **Risks, owned:** "zero instruction" is a behavioral assumption — the wobbling cup is the only nudge; the 8s cat fail-safe guarantees comedy either way, and the replay re-laughs a "miss". One screen risks 3am clutter — mitigated by sequential beats (one bubble at a time) and candy-chunk visual language. Party PA volume is why the code is 4 confusable-free chars and copy-paste is primary.
- **Scripted vs seeded:** tutorial beats are deterministic script (no seeded randomness can roll a non-funny outcome); seeded RNG resumes at first shift under the global game rule.
- **Ops:** remote browser play needs wss + TLS on a reachable host; server lives in the container/VM per the security rule; host Play Mode remains authorized for playtesting. A room with no members is reclaimed by the server (state keyed, process shared).

## Refined Gate

Artifact 11 passes when ALL hold:

1. **One screen:** exactly one scene rendered between URL load and first shift-ready state in a session trace; zero modal menu screens; lobby, tutorial, and pre-match room are the same screen; returning players land ready.
2. **First action funny by construction:** first valid input is a FLING at Danny the Regular ("FLING ME MY KWA FEE. HURRY."); ANY input completes a scripted face-splat + slow-mo micro-replay + Vinny line (banner+TTS); no input in 8s → the Shop Cat performs the beat; zero instruction text anywhere.
3. **Five verbs, one screen, no menus:** 5 scripted deterministic beats (FLING/CHUG/STEAL/FIX/SABOTAGE), each first-use yielding a scripted funny outcome, bubbles are in-world speech, tutorial ≤60s from first input (mean of 3 recorded runs); no skip, no key hints, no help screen.
4. **Room code:** 4-char confusable-free alphabet `{A,C,D,E,F,H,J,K,M,N,P,R,T,U,V,W,X,Z}`, session-mapped; URL is primary (first player types nothing); "SEND THE LINK, KEHD" + code chip fallback; invalid code → Vinny line + card shake, no dialog; URL reload rejoins ≤5s (Artifact 10 contract).
5. **Name pick:** ≥12 preset chips + "RANDO", zero free text, session-persistent, rendered above the barista.
6. **Candy UI / DRY / perf:** all new UI is SVG/code per LOOK rule 8 with zero new Blender assets (Danny = tinted Customer); tutorial composes existing prefabs and verb rules; 60fps with 4 baristas + tutorial physics, pooled props, measured frame-time log; all strings Boston-voice, Vinny never compliments; no silent audio state.
7. **Gate evidence — real and recorded:** one real fresh player per test, screen+audio recording in EVIDENCE.md, tester says only "open it" and touches nothing; pass = audible laugh ≤60s from load AND voluntary continued play past 60s; second fresh player confirms; failures → beat redesign + retest with a different fresh player; no fabricated or impersonated human test, ever; the gate is not claimed until the laugh happens at a real URL.