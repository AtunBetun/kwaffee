# Artifact 10 — Authoritative multiplayer

- Beads: `kwaffee-7xn` (blocked by 09). Slug `10-multiplayer`.
- Grilled scope: pick the wire (transport), the state-sharing model, prediction/reconciliation + interpolation, reconnect + room-code protocol boundary, the /sim↔server seam contract, and the scripted-loss gate — all under the friendslop lens (cheapest version still fun, 3-4 friends, one way to do a thing).

## Design tree (decisions resolved)

**1. Transport — one WebSocket path everywhere.**
Options: WebSocket (TCP), UDP, Steam Sockets, WebRTC data channel, dual (UDP desktop + WebSocket browser).
DECIDED: **single WebSocket transport, one code path, on BOTH desktop and WebGL**, via Unity Transport (`com.unity.transport`, `WebSocketNetworkInterface`) used by both client and server roles. Rationale: browsers cannot do raw UDP, and the gate's remote case must run in a browser eventually; a second (UDP/Steam/WebRTC) transport doubles surface, drifts, and forks the test matrix — friendslop says one way to do a thing. Party-game latency budget (50–150ms) fits TCP fine at 30Hz + interpolation; small quantized snapshots make TCP HOL blocking negligible on LAN and tolerable on the 2-remote leg. Revisit UDP only if the remote gate shows visible rubber-banding. Steam Sockets/WebRTC rejected: party-of-friends needs no Steam dependency; WebRTC in Unity needs a JS bridge, complexity for a latency win a party game doesn't feel. Dual transport rejected: two code paths, two test surfaces.
Justification vs ADR-0001 ("justify transport"): ADR-0001 binds engine + authoritative model, not the wire; WebSocket satisfies it for both builds from one codebase. Fallback if UTP WebSocket proves broken: hand-rolled RFC6455 server thread — do NOT default to that.

**2. Shared-state model — no scene-graph deep copy; per-room fresh Shift instance.**
Prompt offered "same scene graph deep-copied per session". Options: deep-copy live scene per session vs instantiate fresh room state.
DECIDED: **no deep copy.** The kwaffee-6id seam makes the whole world a plain module — `Shift.Begin(seed, actors)` spawns fresh pooled state per room. Server = room factory; one `Shift` instance per room code; the shared scene graph (prefabs) is instantiated per room, not copied. Rationale: a live-scene deep copy invites divergence and is expensive; the seam's `Begin` already resets state — reuse it. Multi-room: one server process hosts many `Shift` instances (a dictionary keyed by code), each independent sim. Keep the scene minimal and fully runtime-spawned (existing CoreLoop pattern) so one server binary = N rooms.

**3. Who runs what — server is the ONLY world sim; clients do NOT re-sim the world.**
ADR-0001 "same scene graph + physics" = one codebase, one `Shift`, the SERVER runs it. It does NOT mean every client locksteps the full world.
DECIDED: **server is truth and runs the entire `Shift` + real physics once per room at fixed 30Hz** — all verbs, all NPCs, all cups/ragdolls, quota/tips/awards, seeded `System.Random`. Clients run a lightweight local-actor predictor (see 4) and render from server snapshots (see 6). No client world RNG, no client NPC logic — NPCs render only, killing divergence by construction. Rationale: full lockstep means every client waits on / predicts remote input arrival (rubber-band source) and re-simulates the whole cast (perf + RNG divergence); server-authoritative snapshots are the standard party model and the friendslop one.

**4. Prediction scope — all five verbs for the LOCAL actor only, through the seam's `Command`.**
DECIDED: **client predicts its own actor + held/freshly-flung objects** (move, aim, chug, fling charge/release, grab/drop, fix, steal, sabotage) by feeding its own input through the SAME `Shift.Command` seam and stepping a local sandbox of the local actor against static colliders + snapshot-interpolated others (kinematic obstacles). Prediction is cosmetic; server is truth. No world RNG consumed client-side (no divergence). Everything else — remote players, all NPCs, free cups — is interpolated server state.
Scope cut fight: full client re-sim of the world for "perfect" local feel — CUT (correctness surface + perf + divergence, no party-gain). Prediction of remote actors — CUT (that's extrapolation; server interpolates them fine).

**5. Reconciliation — threshold snap with short blend; discrete verdicts hard-flip to server.**
DECIDED: when a server snapshot arrives for a predicted entity, compare; positional/rotational deviation > threshold snaps toward server with a ~100ms error-correcting lerp (no teleport); a discrete state flip (cup actually spilled, steal illegal, you got stunned, chug overdosed) hard-snaps immediately to server verdict. **Server verdicts always win; client never vetoes.** Thresholds: 0.25m position / 15° rotation / any carry-ID or verb-result flip. Rationale: server-authoritative means the client must never keep a contested outcome; hard-flip on discrete verdicts keeps comedy truth (you DID get bean-sacked) while the lerp hides position error.

**6. Interpolation on top of 30Hz ticks — 2-buffer client-side lerp, render-rate 60fps.**
DECIDED: **all non-predicted entities render from a 2-snapshot interpolation buffer at render rate** (~66ms depth), consuming two server ticks and interpolating by render time. The local actor is excluded (predicted). The freshly-flung cup is predicted until the first server snapshot for it arrives, then blends to the server arc (keeps fling feel, no visible jump). DECIDED 60fps render / 30Hz sim split is the frame budget; no per-frame allocation in the netcode path (reuse snapshot byte buffers + pooled lists, no LINQ in hot path) — GC is a blocker.

**7. Reconnect + room codes — protocol here, UI belongs to Artifact 11 (lobby).**
Boundary: Artifact 10 owns the **protocol + server-side room registry**; Artifact 11 owns all visible UI (code entry/display, name pick, lobby screen).
DECIDED:
- Room code: **5 chars from an unambiguous alphabet** (no 0/O/1/I/L). Server allocates on host "create"; clients "join" by code; collision → regenerate. Round-trip create/join/leave <100ms on loopback.
- Reconnect: client holds a **session token** issued on join. On WebSocket close/timeout, client reconnects to the same room with its token; server restores the actor to its slot, sends latest snapshot + resync. Server keeps a disconnected actor's slot **30s**, during which the existing AFK Shop-Cat coverage (Artifact 2 behavior, reused) mans the station; after timeout the slot is freed.
- Session model: no accounts, no auth beyond room code + token (friends party; spec forbids bridging game network to the internet — server runs on a host/container, remote friends connect to the host address over WebSocket).
- EXCLUDED (friendslop cuts): mDNS/UDP auto-discovery (address comes from UI/CLI arg — paste IP or localhost), NAT punchthrough/relay service, spectator mode, matchmaking, cross-region, encryption. Add none.

**8. /sim ↔ server share the SAME `Shift.Step` — the shift-seam contract (kwaffee-6id).**
DECIDED: the seam is the load-bearing contract. The authoritative server's tick = `Shift.Step(dt)` at fixed 30Hz; the `/sim` harness drives the SAME `Shift.Step` at the SAME 30Hz. Symmetry: **the server treats a remote player exactly like `/sim` treats a bot — both are `Command` producers into one `Shift`.** Netcode = transport of remote `Command`s into the seam; `Metrics()` = the snapshot source. Physics stepping stays owned by the sim adapter (kwaffee-6id's determinism invariant), not by netcode.
This ticket CONSUMES kwaffee-6id; two hard contract asks on it (see Dependencies): (a) expose a `WorldState` snapshot read (entity id → pose/carry/flags) — its current `Metrics()` surface has tips/machine/order/round stats but NO per-entity poses, which netcode needs; (b) pin the fixed step to 30Hz (current sim harness runs 50Hz — conflicts with ADR-0001's 30Hz; reconcile to 30Hz so /sim numbers equal the netcode world).

**9. Packet-loss test design — pluggable interference layer, headless, pre-hub.**
DECIDED: the transport carries a **reliability-interference layer** (drop/duplicate/delay by seeded %, configurable 0/5/10/15/25%) inside the transport so the same test runs with real sockets or loopback. Headless netcode test (N scripted bots + server, one process or loopback): (a) **server authority** — for the same seed, server metrics are byte-identical across all loss levels (loss never changes the authoritative outcome); (b) **convergence** — every client's rendered state lands within one quantization step of server snapshot; (c) **reconnect** — kill a client mid-shift, reconnect with token, state resyncs to threshold in <1s, no rubber-band after. This runs in editor/Docker pre-hub, pre-WebGL-module → gate proxies testable now. Live gate (3 LAN + 2 remote) still needs builds: desktop-first, see Dependencies.

**10. Voice/events — server broadcasts Shift events; clients render only.**
DECIDED: roasts, awards, chaos, CATASTROPHE triggers fire ONCE on the server (seam rule: voice only when Shift records the event) and are broadcast as an event channel alongside snapshots; clients render the banner/sting locally, never re-derive. Preserves "voice and gameplay cannot desync" under netcode; Big Vinny is single-sourced.

**11. Determinism — single-instance, not lockstep.**
DECIDED: determinism is required on the SERVER only (one sim per room). No cross-process determinism requirement, because clients never run world RNG. The /sim determinism gate (kwaffee-6id) still matters for balance/harness, but netcode does not add a lockstep determinism surface. This is a simplification, not a feature cut.

## Refined acceptance criteria (gate proxies, numeric, testable pre-hub)

In this artifact's own language — the artifact gate is met when ALL hold:

- **Single transport:** one WebSocket code path (UTP `WebSocketNetworkInterface`) used by both desktop and WebGL builds; no UDP/Steam/WebRTC/dual path present.
- **Server authority under loss:** headless netcode run, same seed, 5 loss levels (0/5/10/15/25%) via interference layer → server `Metrics()` byte-identical across levels; every client within 1 quantization step of server snapshot.
- **No visible rubber-banding:** under ≤100ms added latency + ≤10% loss, remote-actor render deviation <0.5m and <200ms lag (measured headless proxy); no interpolation gap >2 server ticks; no render jump >1 quantized step.
- **Prediction latency:** local input → visible response ≤1 server tick + 1 render frame; reconcile snap 0.25m / 15° / any state-flag flip; discrete verdicts hard-snap, positions blend ≤100ms.
- **Reconnect:** client killed mid-shift reconnects with token within 30s window; state resync <1s; no rubber-band post-resync; Shop Cat covers the slot meanwhile.
- **Room codes:** create/join/leave round-trip <100ms loopback; 5-char unambiguous alphabet; collision auto-regenerates.
- **60fps:** 4-bot run over loopback WebSocket — render frame <16.7ms p95, **0 bytes GC allocated per frame in the netcode hot path** (Unity Profiler allocator evidence).
- **Seam parity:** `/sim` and server call the same `Shift.Step` at the same 30Hz; a remote player and a /sim bot issue identical `Command` sequences and the server cannot tell them apart.
- **Live gate (deferred to human prereq):** 3 LAN + 2 remote machines, no visible rubber-banding, 60fps, server authoritative — evidence recorded in EVIDENCE.md.

## Blender art needs

**NONE.** This artifact is pure netcode/protocol/transport. No visible assets authored. The only new UI (netcode status line: ping/jitter/packets-lost/players) is 2D in code (UI Toolkit), consistent with the flat-candy-UI parent rule; the lobby/room screen is Artifact 11's asset surface, not this one's. No audio authored here (reuses Artifact 9 cues, server-broadcast as events).

## Dependencies + risks

**Inputs (artifacts 1–9):** the entire `Shift` content the server runs — core loop + five verbs, coffee drug/death, machines/sabotage, mule/dealers, NPC cast (must be fully sim-side; clients render only), visual identity (server broadcasts squash-stretch/animation params for the physics-comedy read), juice/delivery (replay + roasts fire server-side, broadcast), audio (cues triggered by server events). **kwaffee-6id shift-seam** is the hard prerequisite — MUST deliver before this lands.

**Contract asks on kwaffee-6id (state loudly):**
1. Add a `WorldState` snapshot read to `Metrics()` (entity id → pose, carry ID, flags) — the current surface lacks per-entity transforms netcode serializes.
2. Pin the shared fixed step to **30Hz** (ADR-0001). Current sim harness runs 50Hz; leaving it there means /sim numbers do not represent the netcode world. One fixed-step constant both server and /sim read.

**External / human prereqs:**
- Unity Personal license activation + WebGL Build Support module NOT installed. **Plan: desktop-first.** `MacStandaloneSupport` is present, so the live gate's "3 LAN + 2 remote" runs on desktop Standalone builds over the same WebSocket transport; WebGL build evidence is appended when the module lands. Because the transport is WebSocket, nothing changes when WebGL arrives — that is the reason for the single-transport decision.
- Spec/ops rule: never bridge the game network to the internet — server runs on a host/container LAN; the 2 remote clients reach the host over WebSocket. No NAT service; friends connect to the host address.

**Risks:**
- TCP HOL blocking on the 2-remote leg — mitigated by 30Hz small quantized snapshots + 2-buffer interpolation + separate event channel; gate measures it (no-rubber-band criterion). Escalation if it fails: UDP for desktop only, WebSocket still for browser — a deliberate second path, only if evidence forces it.
- kwaffee-6id may not land the `WorldState`/30Hz asks → netcode blocked on the seam; flag to main coordinator to sequence 6id before 7xn.
- Interpolated rendering of physics-comedy (ragdolls, squash-stretch) — if server-broadcast pose/animation params read janky, tune thresholds/quantization; visual-only, not correctness.
- Multi-room server = one process hosting N `Shift` instances: watch per-room pooled-buffer isolation so rooms don't share snapshot buffers (a cross-room GC/race bug is the kind of subtle blocker to test explicitly).
