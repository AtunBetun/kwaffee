# Grilling Memo — Artifact 11: Lobby & Onboarding

- Beads id: `kwaffee-vau` (blocked by: `kwaffee-7xn` / Artifact 10 netcode)
- Grilled scope: room codes, name pick, ONE-SCREEN tutorial with a funny first action, room backend, 60-second laughter gate protocol. Friendslop lens: cheapest genuinely fun path, comedy over fidelity, shrink not expand.

---

## Design tree (decisions resolved)

### 1. Room backend — which transport architecture?
Options: (a) hosted relay — headless Unity authoritative server (ADR-0001) as a reachable session host, clients connect outbound; (b) host-P2P — one player's machine hosts; (c) LAN-first only until WebGL module lands.
**DECIDED: hosted relay, consistent with ADR-0001.** WebGL can only connect outbound (browser sockets); the authoritative server is already the mandated architecture; P2P adds NAT traversal + signaling for zero benefit and breaks server authority over the theft/griefing simulation. LAN-first rejected as a *final* state — it cannot satisfy the WebGL gate — but valid as a *staging* shortcut while the WebGL module prereq is pending. A room = one authoritative server session; room code = session id. [ASSUMPTION, loud: Artifact 10's exact wire transport is unsettled (GrillNetcode is resolving it in parallel). Lobby surface assumes WebSocket relay; if netcode lands on something else, only the connect call changes, no lobby surface does.]

### 2. Tutorial placement — menu screen or folded into the lobby?
Options: (a) separate tutorial scene before the lobby; (b) no separate scene — the pre-match room IS the tutorial IS the lobby.
**DECIDED: one screen, zero menu screens.** URL lands straight in the room; the tutorial script runs in that same scene; the room code chips and name bar live in the same flat candy HUD. Anything between URL and first laugh is a tax on the 60-second gate.

### 3. The FIRST action — what does a fresh player physically do first?
Options: (a) click something, (b) walk, (c) read a key list, (d) fling a cup.
**DECIDED: first input = FLING gesture (hold-to-charge, release-to-launch) at a named customer ("Danny the Regular") whose demand bubble reads "FLING ME MY KWA FEE. HURRY." — in-world demand bubble, not UI text, so zero instruction was given (artifact 6 customer-bubble pattern, proven readable).** Wind-up to launch is the exact gesture the game lives on; the tutorial teaches the game's one-sentence control by making the player perform it 3 seconds in. A cup is already in-hand and wobbling when the scene loads — anything wiggling-in-front-of-you gets clicked. ANY release timing/charge lands the scripted outcome: cup splats Danny in the face (squash, splash, googly eyes cross, slow-mo micro-replay, Vinny banner+TTS "OW. Wicked splahst, kehd."). Funny by construction because the outcome is scripted for the first fling only; the charge gesture still *feels* real. **Fail-safe: if the player touches nothing for 8s, the Shop Cat walks over and bats the cup into Danny's face — the tutorial is funny even with zero input.**

### 4. Learning the five verbs on ONE screen — contextual in-world lines or key-hint overlay?
Options: (a) key-hints overlay listing WASD/E/L/R/SPACE/V; (b) contextual one-liners fired by scripted beats in the live scene.
**DECIDED: contextual in-world one-liners, no overlay, no help screen.** Key lists are silent UI — they teach nothing funny. The tutorial is 5 ordered beats (~8-10s each, ~45-60s total) in the same scene, each a scripted micro-outcome tied to a verb: FLING (beat 1 above) → CHUG (Danny's offered spare cup; chug → 1s jitter ragdoll + Vinny) → STEAL (the cat parades past carrying a tipped tray; bubble "Rob the cat." → swipe → cat outrage) → FIX (Grinder belch + smoke; bubble "Give it the wrench." → fixed with a clatter) → SABOTAGE (Steam Wand idles next to Danny; bubble "Warm him up." → steam + Danny yelp). Each beat's first use produces comedy, not a checkmark. Proximity-triggered bubbles; Vinny prods with rotating (non-instructional) lines after 5s idle. No skip button: tutorial is one-time (flag set when first shift starts), 60s max, faster to play than to skip.

### 5. Room-code flow — 4 chars? who reads it? exact copy pattern?
Options: (a) 6-char alphanumeric; (b) 4-char with confusables removed; (c) no code at all, URL-only.
**DECIDED: 4 uppercase chars from confusable-free alphabet {A,C,D,E,F,H,J,K,M,N,P,R,T,U,V,W,X,Z}, URL is primary carrier, code is human-pass fallback.** Copy pattern, host-facing on the room: primary button "SEND THE LINK, KEHD" (copies URL) + giant candy chip of the code beside it with a copy icon; secondary line "or just OH-vah the code". Friends' home screen shows ONE input: "GOT THE CODE, KEHD?" Single field, chunky candy bar, bounce on focus, invalid code = Vinny line + card shake ("What's that s'pposed ta be?"). The first player never types anything — the URL *is* the room — which guarantees the 60s path has no text-entry step. 4 chars keeps it shoutable across the room (party); confusable-free because remember-and-retype at a party.

### 6. Name pick — preset list or free text?
Options: (a) free text field; (b) preset chips; (c) preset chips + shuffle.
**DECIDED: preset chips (≥12 Boston-flavored names) + "RANDO" shuffle button; zero free text.** One tap = name; names are their own first joke ("Two-Timmy", "Dunkin' Donna", "Vinny Jr.", "The Kehd", "Big Nose Sal", "Chowdah Charlie", "Bunny from Braintree", "Ricky from Revere", "Slaps", "Cah-Mon Derek"). No keyboard, no profanity moderation at a party, no rage-typing delays. Selection persists for the session; name floats above the barista's head (googly eyes + name = the identity, per LOOK rule 2 — eyes carry the soul, name carries the ego).

### 7. Avatar customization in the lobby?
Options: (a) color/paint picker; (b) full avatar editor; (c) none.
**DECIDED: none.** Customization buys nothing at this stage — the joke is the name and the clay body that's already authored; a paint picker is a menu (violates branch 2) for zero comedy. Name chips + googly eyes ARE the identity. Cut entire.

### 8. What else gets CUT (scope guardrails)?
Options: keep accents vs delete weightless features.
**DECIDED — cut: accounts, login, friends lists, matchmaking, region select, voice chat, text chat, pause/settings menu, difficulty, tutorial skip, error screens as modal dialogs, all "how to play" overlays.** All of it is weightless for a 3-4 friend room-link flow; a party game's server browser is a URL. Room-not-found = Vinny line + card shake, no dialog box. Returning players (first-shift flag set) land in the room in the ready state, tutorial skipped — that is the only "skip".

### 9. Backend lifecycle — who runs the session host, where?
Options: (a) per-room ephemeral server process on the same box/container; (b) one server process hosting many rooms.
**DECIDED: one headless Unity authoritative server process hosting many rooms (session map room-code → RoomState), running in the container/VM per the security rule; desktop staging can launch a local server process with the identical room-code flow.** One process = one deployable = laziest ops; room isolation is state keying, no process juggling. Ops note: remote browser play needs wss + TLS on a reachable host — flagged in risks.

### 10. The 60-second laughter gate — protocol, evidence, what counts.
Options: (a) simulated/self-reported; (b) one real fresh player, recorded; (c) staged actor.
**DECIDED: real fresh player (never played KWA FEE), recorded screen+audio; tester speaks exactly one instruction — "open it" — and touches nothing after.** Protocol: (1) tester hands over URL (or opens it for them); (2) clock starts at page-load; (3) player free-play, zero further words, tester does not point/gesture; (4) recording captured (OBS or phone, video file joins EVIDENCE.md, no fabricated transcript); (5) pass = audible laugh ≤60s from load AND continued engagement after the laugh (player keeps interacting voluntarily past 60s — "again again" or re-engagement; a courtesy laugh dies in 5s of stillness and does not count); (6) laugh = audible vocalization of amusement, coded by the tester from the recording with a timestamp; borderline cases ruled by "did they keep playing"; (7) failure → redesign the failing beat, re-test with a DIFFERENT fresh player; (8) second fresh player confirms once the first passes (2 data points minimum, mirroring artifact 8's friend-laugh precedent, but the gate is the first 60s laugh). No friend required by definition — but a second real human in the room raises laugh odds and is allowed; the recorded subject is always the fresh player. Gate blocked until a URL exists (WebGL module prereq) — staging rule in risks.

---

## Refined acceptance criteria (artifact's own language)

1. **One screen:** exactly one scene between URL load and first shift-ready state; zero modal menu screens; lobby, tutorial, and pre-match room are the same screen. Counted as screens rendered in a session trace.
2. **First action funny by construction:** the first valid input is a FLING (hold/release); ANY input completes the beat; outcome is a scripted face-splat + slow-mo micro-replay + Vinny line (banner+TTS). If no input in 8s, the Shop Cat performs the beat — comedy with zero input. No instruction text anywhere; the only guidance is the customer demand bubble.
3. **Five verbs, one screen, no menus:** 5 scripted beats (FLING/CHUG/STEAL/FIX/SABOTAGE), each first-use yielding a scripted funny outcome; bubbles are in-world customer/cat/prop speech, never UI key lists; total tutorial ≤60s from first input (measured mean of 3 tester runs, each recorded).
4. **Room code:** 4-char, confusable-free alphabet, session-mapped; URL carries the room (first player types nothing); "SEND THE LINK" primary + code chip fallback; invalid code → Vinny line + card shake, no dialog. Reconnect: reloading the URL returns to the room ≤5s (contract with Artifact 10).
5. **Name pick:** ≥12 preset chips + "RANDO" shuffle; no free text; selection persists for session and shows above the barista.
6. **Candy UI (LOOK rule 8):** all lobby/onboarding UI is SVG/code — chunky rounded chips, candy gradients, bounce on hover/focus, giant icons; zero Blender assets introduced; every string is Boston-voice (Vinny never compliments).
7. **Gate protocol evidence:** one real fresh player per test, screen+audio recording filed in EVIDENCE.md, one-instruction maximum, pass = audible laugh ≤60s from load + voluntary continued play; failure = different fresh player retest after beat redesign; no fabricated or impersonated human test, ever.
8. **Performance:** lobby/tutorial holds 60fps with 4 baristas + tutorial physics (cat, cup wobble, steam), pooled/reused props, no per-frame allocation; measured frame-time log in evidence. Regressions are blockers.
9. **DRY with artifacts 1-7:** tutorial reuses existing Barista/Cup/Customer/Counter/Machine/Cat prefabs and existing verb rules — the tutorial is composition, not new simulation code.

## Blender art needs

**None.** The tutorial, lobby, and onboarding compose exclusively from art surface already delivered by Artifacts 1-7: Barista, Cup, Customer (Danny = tinted existing Customer prefab via the parameterized material tint — no new mesh), Counter, Grinder/SteamWand, Shop Cat. All new visual surface is flat candy UI in code/SVG (chips, banner, code card, name chips) per the ART rule's 2D-in-code clause. Avatar customization = none (decided, branch 7). If any acceptance criterion above ever needs visible art, that asset MUST be authored via the Blender bpy pipeline (manifest-validated, origins y=0, submesh materials, ≤35k triangles) — but none is required.

## Dependencies + risks

**Dependencies (upstream artifacts):**
- Artifact 10 (`kwaffee-7xn`) — room-code↔session map, reconnect, transport, prediction/interpolation. **Open question passed upstream:** the lobby needs the room-code join API surface defined; surface here assumes WebSocket relay (branch 1).
- Artifact 8 — Vinny banner+TTS, slow-mo micro-replay (the face-splat replay IS the laugh driver), hitstop.
- Artifacts 1-7 — verbs, machines, cat (fail-safe beat), customers, visual identity, round loop as tutorial substrate.
- Artifact 9 — stings for splat/cat-outrage/steam (no silent state rule).
- Human prereqs — Unity Personal license + WebGL Build Support module: no URL exists until these land; NEVER retry batchmode licensing (DECISIONS.md).
- Security rule — server runs in container/VM; host Play Mode remains authorized for playtesting.

**Risks (stated loudly):**
- **No URL, no gate:** the 60s-laugh gate is un-runnable until the WebGL build exists. Staging mitigation: local-server desktop runs exercise the identical lobby/tutorial surface, so the tutorial beats and gate protocol are testable and shippable before the module lands; only the URL delivery step waits. Do NOT claim the gate until a real fresh player laughs at a real URL.
- **Transport unset:** if GrillNetcode lands WebRTC or raw UDP instead of WebSocket, branch 1's connect layer changes; the lobby surface (chips, copy, name bar, one-screen rule) is transport-agnostic.
- **"Zero instruction" behavioral assumption:** the wiggle-in-front-of-you cup is the only nudge; the 8s cat fail-safe guarantees comedy anyway, but could read as the player "missing" — acceptable, the replay re-laughs it.
- **One screen ≠ clutter:** 5 beats + code chips + name bar on one screen risks visual noise at 3am; mitigated by sequential beats (only one bubble live at a time) and the candy-chunk visual language.
- **Party PA volume:** code must be readable/sayable (confusable-free 4-char); copy-paste-link primary reduces human transcription to zero.
- **Seeded RNG vs scripted tutorial:** tutorial beats are scripted (deterministic outcomes), never seeded-random, so "funny by construction" cannot roll a non-funny outcome; seeded RNG resumes after first shift (global game rule).