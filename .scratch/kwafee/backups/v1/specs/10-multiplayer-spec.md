# Artifact 10 — Authoritative Multiplayer (Spec)

Beads: `kwaffee-7xn` (blocked by 09). Slug: `10-multiplayer`.
Owns: transport, shared-state model, prediction/reconciliation, interpolation, reconnect + room-code **protocol**, the `/sim`↔server seam contract, and the scripted-loss gate. Consumes `kwaffee-6id` (the shift-seam refactor) — does NOT absorb its work.

## Problem Statement (from the user's perspective)

Three or four friends want to run the shop together — some on the same couch, some at home — and see the exact same stupid thing happen at the same time. When I fling a cup at my friend and it smacks them in the face, it has to smack them on their screen too, not desync into them dodging it or me "stealing" a tip I never got. My button press has to feel instant, the coffee has to land where physics says it lands, and when I lag out or my laptop hiccups, I need to land back in my barista's body without the game teleporting or rubber-banding me around. Nobody wants to read a "who's the server?" manual; I type a 5-character code and we're pouring.

## Solution (user perspective)

One room code, one shared world, one server that is always right. I join by pasting a short code, my input feels immediate (my own actions are predicted locally), and everything I'm not directly controlling arrives smoothly at 60fps — remote friends, the NPC cast, cups flying through the air. When the server disagrees with me it wins, but it blends the fix in so I barely notice position errors, and hard-flips only the comedy verdicts that matter (yes, you really did get bean-sacked). If my connection drops I have 30 seconds to get back, my barista slot is kept warm (the Shop Cat mans it meanwhile), and I resync without rubber-banding. One transport does it all on desktop and browser, so the game I test on a laptop is the game that ships to the web.

## User Stories

1. As a host, I want to create a room and get a 5-character code, so that I can paste it to my friends and they can join without reading a networking manual.
2. As a friend joining remotely, I want to enter that code and connect over the same WebSocket path the LAN players use, so that the game works whether I'm on the couch or at home.
3. As a player, I want my movement and all five verbs (FLING, CHUG, FIX, STEAL, SABOTAGE) to respond to my input within one sim tick plus one frame, so that my actions feel instant even though the server is authoritative.
4. As a player, I want my held cup and the cup I just flung to track my input locally, so that throwing feels immediate and I don't see my own actions lag.
5. As a player, I want remote friends and all NPCs rendered from smooth interpolated server state, so that they move fluidly at 60fps even though the server ticks at 30Hz.
6. As a player, I want small position/rotation disagreements with the server to blend in over ~100ms rather than teleport me, so that I never see rubber-banding from ordinary latency jitter.
7. As a player, I want discrete verdicts (I got stunned, the cup actually spilled, the steal was illegal, I overdosed) to snap immediately to the server's answer, so that the comedy outcome is the truth for everyone at once.
8. As a player, I want the server to be the only world sim, so that the physics, quota, tips, machines, and NPCs are identical for every client and nobody can disagree about what happened.
9. As a player, I want my connection to drop and reconnect with a session token into the same barista slot, so that I don't lose my body and my state when my Wi-Fi blips.
10. As a player who is briefly disconnected, I want the Shop Cat to cover my station for up to 30 seconds, so that the shift keeps moving and my slot isn't taken while I'm gone.
11. As a player, I want the server to broadcast events (roasts, awards, chaos, catastrophe triggers) once and have clients render them, so that Big Vinny's narration and the game's voice never desync with what actually happened.
12. As a developer, I want server metrics to be byte-identical across scripted packet-loss levels for the same seed, so that loss can never change the authoritative outcome.
13. As a developer, I want every client's rendered state to land within one quantization step of the server snapshot, so that I can measure convergence objectively under loss.
14. As a developer, I want `/sim` bots and remote players to be indistinguishable `Command` producers into one `Shift.Step` at the same 30Hz, so that the harness numbers equal the netcode world.
15. As a developer, I want zero bytes GC-allocated per frame in the netcode hot path, so that the game holds 60fps on a mid laptop without a single allocation stutter.

## Implementation Decisions

**D1 — One WebSocket transport, everywhere (no dual path).**
Single code path via Unity Transport (`com.unity.transport`, `WebSocketNetworkInterface`) used by both client and server roles, on both desktop Standalone and WebGL. No UDP, no Steam Sockets, no WebRTC, no dual transport. Browsers cannot do raw UDP and the remote gate must eventually run in a browser; one path = one test surface, and the party-game latency budget (50–150ms) fits TCP at 30Hz + interpolation with small quantized snapshots. Justifies ADR-0001 (which binds engine + authoritative model, not the wire). Fallback if UTP WebSocket proves broken: hand-rolled RFC6455 server thread — do NOT default to it.

**D2 — No scene-graph deep copy; per-room fresh Shift instance.**
The world is a plain module via the kwaffee-6id seam. Server is a room factory: one `Shift` instance per room code (`Shift.Begin(seed, actors)` spawns fresh pooled state), instantiated per room, never copied from a live scene. Multi-room = one server process hosting a dictionary of `Shift` instances keyed by code, each independent. Scene stays minimal and fully runtime-spawned (existing CoreLoop pattern) so one server binary hosts N rooms. Per-room snapshot buffers MUST be isolated (see D11 risk).

**D3 — Server is the only world sim; clients do not re-sim.**
Server is truth and runs the entire `Shift` + real physics once per room at fixed 30Hz — all verbs, all NPCs, all cups/ragdolls, quota/tips/awards, seeded `System.Random`. Clients run only a lightweight local-actor predictor and render server snapshots. No client world RNG, no client NPC logic (NPCs render only) — divergence killed by construction. Not lockstep: clients never simulate the full world, so no waiting on / predicting remote input.

**D4 — Prediction scope: local actor + held/freshly-flung objects only, via the seam's `Command`.**
Client predicts its own actor and its held/freshly-flung objects (move, aim, chug, fling charge/release, grab/drop, fix, steal, sabotage) by feeding its own input through the SAME `Shift.Command` seam and stepping a local sandbox of the local actor against static colliders + snapshot-interpolated others as kinematic obstacles. Prediction is cosmetic; server is truth. No world RNG consumed client-side. Everything else (remote players, all NPCs, free cups) is interpolated server state. Explicitly CUT: full client re-sim of the world, and prediction/extrapolation of remote actors.

**D5 — Reconciliation: threshold snap + short blend; discrete verdicts hard-flip.**
On a server snapshot for a predicted entity: deviation > 0.25m position / 15° rotation / any carry-ID or verb-result flip → snap toward server with ~100ms error-correcting lerp (no teleport) for continuous values. Discrete state flips (cup actually spilled, steal illegal, you got stunned, chug overdosed) hard-snap immediately to the server verdict. Server verdicts always win; client never vetoes.

**D6 — Interpolation: 2-buffer client-side lerp at render rate.**
All non-predicted entities render from a 2-snapshot interpolation buffer at render rate (60fps), ~66ms depth, interpolating between two server ticks by render time. Local actor excluded (predicted). Freshly-flung cup predicted until first server snapshot for it arrives, then blends to the server arc. 60fps render / 30Hz sim split is the frame budget. No per-frame allocation in the netcode path: reuse snapshot byte buffers + pooled lists, no LINQ in the hot path — GC is a blocker.

**D7 — Reconnect + room-code protocol (UI belongs to Artifact 11).**
Artifact 10 owns protocol + server-side room registry; Artifact 11 owns all visible UI (code entry/display, name pick, lobby).
- Room code: 5 chars from an unambiguous alphabet (no 0/O/1/I/L). Server allocates on host "create"; clients "join" by code; collision → regenerate. Create/join/leave round-trip <100ms on loopback.
- Reconnect: client holds a session token issued on join. On WebSocket close/timeout, reconnects to the same room with the token; server restores the actor to its slot, sends latest snapshot + resync. Server keeps a disconnected actor's slot 30s, during which existing AFK Shop-Cat coverage (Artifact 2 behavior, reused) mans the station; after timeout the slot is freed.
- Session model: no accounts, no auth beyond room code + token. Never bridge the game network to the internet — server runs on a host/container LAN; remote friends reach the host address over WebSocket.

**D8 — `/sim`↔server share the same `Shift.Step` — the shift-seam contract (kwaffee-6id).**
This is the load-bearing contract. Authoritative server tick = `Shift.Step(dt)` at fixed 30Hz; `/sim` drives the SAME `Shift.Step` at the SAME 30Hz. Symmetry: server treats a remote player exactly like `/sim` treats a bot — both are `Command` producers into one `Shift`. Netcode = transport of remote `Command`s into the seam; `Metrics()` = snapshot source. Physics stepping stays owned by the sim adapter (kwaffee-6id's determinism invariant), not by netcode.

**D9 — Voice/events: server broadcasts Shift events; clients render only.**
Roasts, awards, chaos, catastrophe triggers fire ONCE on the server (seam rule: voice only when Shift records the event) and broadcast on an event channel alongside snapshots. Clients render the banner/sting locally, never re-derive. Big Vinny is single-sourced.

**D10 — Determinism: single-instance, not lockstep.**
Determinism required on the SERVER only (one sim per room). No cross-process determinism requirement because clients never run world RNG. The `/sim` determinism gate (kwaffee-6id) still matters for balance/harness, but netcode adds no lockstep determinism surface. This is a simplification, not a feature cut.

**D11 — Scripted-loss interference layer.**
Transport carries a reliability-interference layer (drop/duplicate/delay by seeded %, configurable 0/5/10/15/25%) inside the transport, so the same test runs with real sockets or loopback. Deliberately test per-room pooled-buffer isolation so rooms don't share snapshot buffers (cross-room GC/race is an explicit test target).

**External dependency — `kwaffee-6id` (shift-seam), MUST land before this.**
Two hard contract asks, stated loudly:
1. Add a `WorldState` snapshot read to `Metrics()` (entity id → pose, carry ID, flags) — current `Metrics()` has tips/machine/order/round stats but NO per-entity poses, which netcode serializes.
2. Pin the shared fixed step to **30Hz** (ADR-0001). The sim harness currently runs 50Hz, which conflicts with ADR-0001's 30Hz; **resolve to 30Hz** — one fixed-step constant both server and `/sim` read, so /sim numbers equal the netcode world. This is the resolution of the 50Hz conflict stated in the memo.

## Testing Decisions

A good netcode test proves server authority, convergence, and no-visible-jank with numbers, headless, pre-hub — not eyeballs. It runs the interference layer over real sockets or loopback and asserts the same outcomes. All tests live in the headless netcode harness (`/sim` + N scripted bots + server, one process or loopback), runnable in editor/Docker, pre-WebGL-module.

- **Server authority under loss:** same seed, 5 loss levels (0/5/10/15/25%) via interference layer → server `Metrics()` byte-identical across levels; every client within 1 quantization step of the server snapshot. Proves loss never changes the authoritative outcome.
- **No visible rubber-banding (headless proxy):** under ≤100ms added latency + ≤10% loss, remote-actor render deviation <0.5m and <200ms lag; no interpolation gap >2 server ticks; no render jump >1 quantized step.
- **Prediction latency:** local input → visible response ≤1 server tick + 1 render frame; reconcile snap 0.25m / 15° / any state-flag flip; discrete verdicts hard-snap, positions blend ≤100ms.
- **Reconnect:** kill a client mid-shift, reconnect with token within the 30s window → state resync <1s, no rubber-band post-resync, Shop Cat covers the slot meanwhile.
- **Room codes:** create/join/leave round-trip <100ms on loopback; 5-char unambiguous alphabet; collision auto-regenerates.
- **60fps / GC:** 4-bot run over loopback WebSocket → render frame <16.7ms p95, **0 bytes GC allocated per frame in the netcode hot path** (Unity Profiler allocator evidence). GC is a blocker.
- **Seam parity:** `/sim` and server call the same `Shift.Step` at the same 30Hz; a remote player and a `/sim` bot issue identical `Command` sequences and the server cannot tell them apart.
- **Per-room isolation:** two rooms on one server do not share snapshot buffers (assert no cross-room GC/race symptom).

Prior art: existing 21 green EditMode tests and 12 landed Blender meshes establish the repo's evidence discipline; the netcode harness extends the `/sim` pattern rather than inventing a new test surface. The live gate (3 LAN + 2 remote) is a human prereq, recorded in EVIDENCE.md, not a pre-hub unit test.

## Out of Scope

- UDP/Steam Sockets/WebRTC/dual transport (explicitly rejected; revisit UDP ONLY if the live remote gate shows visible rubber-banding, as a deliberate second path).
- mDNS/UDP auto-discovery (address comes from UI/CLI arg — paste IP or localhost), NAT punchthrough/relay service, spectator mode, matchmaking, cross-region, encryption. Add none.
- Lockstep determinism / cross-process determinism requirement.
- Full client re-sim of the world; prediction/extrapolation of remote actors.
- All room-code / lobby / name-pick / status UI beyond the minimal netcode status line (ping/jitter/packets-lost/players) — the lobby surface is Artifact 11's.
- Any Blender art — NONE is authored here (see below).
- Any audio authoring — reuses Artifact 9 cues, triggered by server-broadcast events.
- WebGL build evidence until the WebGL Build Support module lands (desktop-first gate).

## Further Notes

**Blender art needs: NONE.** This artifact is pure netcode/protocol/transport. No visible assets authored. The only new UI is a 2D-in-code (UI Toolkit) netcode status line, consistent with the flat-candy-UI parent rule.

**Inputs consumed:** the entire `Shift` content the server runs — core loop + five verbs, coffee drug/death, machines/sabotage, mule/dealers, NPC cast (fully sim-side; clients render only), visual identity (server broadcasts squash-stretch/animation params for the physics-comedy read), juice/delivery (replay + roasts fire server-side, broadcast), audio (cues triggered by server events). kwaffee-6id is the hard prerequisite.

**External / human prereqs:** Unity Personal license activation + WebGL Build Support module NOT installed. Desktop-first: `MacStandaloneSupport` present, so the live gate's "3 LAN + 2 remote" runs on desktop Standalone builds over the same WebSocket transport; WebGL build evidence appended when the module lands. Because the transport is WebSocket, nothing changes when WebGL arrives — the reason for the single-transport decision. Server runs in a container/host LAN; never bridge to the internet.

**Risks:** TCP HOL blocking on the remote leg — mitigated by 30Hz small quantized snapshots + 2-buffer interpolation + separate event channel; gate measures it (no-rubber-band criterion), escalation to desktop-only UDP only if evidence forces it. kwaffee-6id may not land the WorldState/30Hz asks → netcode blocked; flag to main coordinator to sequence 6id before 7xn. Interpolated physics-comedy (ragdolls, squash-stretch) may read janky — tune thresholds/quantization; visual-only, not correctness. Multi-room pooled-buffer isolation is an explicit test target (D11).

## Refined Gate

Artifact 10 is done when ALL hold (headless, pre-hub, numeric):

1. **Single transport** — one WebSocket code path (UTP `WebSocketNetworkInterface`) on both desktop and WebGL; no UDP/Steam/WebRTC/dual path present.
2. **Server authority under loss** — headless run, same seed, 5 loss levels (0/5/10/15/25%) via interference layer → server `Metrics()` byte-identical across levels; every client within 1 quantization step of the server snapshot.
3. **No visible rubber-banding** — under ≤100ms added latency + ≤10% loss: remote-actor render deviation <0.5m and <200ms lag; no interpolation gap >2 server ticks; no render jump >1 quantized step.
4. **Prediction latency** — local input → visible response ≤1 server tick + 1 render frame; reconcile snap 0.25m / 15° / any state-flag flip; discrete verdicts hard-snap, positions blend ≤100ms.
5. **Reconnect** — client killed mid-shift reconnects with token within 30s window; state resync <1s; no rubber-band post-resync; Shop Cat covers the slot meanwhile.
6. **Room codes** — create/join/leave round-trip <100ms loopback; 5-char unambiguous alphabet; collision auto-regenerates.
7. **60fps** — 4-bot loopback run: render frame <16.7ms p95, 0 bytes GC allocated per frame in the netcode hot path (Unity Profiler allocator evidence).
8. **Seam parity** — `/sim` and server call the same `Shift.Step` at the same 30Hz; a remote player and a /sim bot issue identical `Command` sequences and the server cannot tell them apart.
9. **Live gate (deferred to human prereq)** — 3 LAN + 2 remote machines, no visible rubber-banding, 60fps, server authoritative; evidence recorded in EVIDENCE.md.

Blender art gate: N/A — no visible assets authored by this artifact.
