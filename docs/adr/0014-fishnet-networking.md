# ADR-0014: FishNet for networking — free, prediction built in, Unity 6 + WebGL

## Status

Accepted (2026-09-10). Evidence: `.scratch/kwafee/research/netcode-2026.md`.

## Context

v2 goes first-person co-op multiplayer with a headless authoritative server (ADR-0001). The owner wants multiplayer foundations now, not later. The one hard requirement is server-authoritative **physics prediction + reconciliation** (ADR-0001's own-input prediction model) with a WebGL-capable client and a headless dedicated server — from one Unity 6 codebase, free, no vendor lock-in.

## Decision

- **FishNet: Networking Evolved** is the networking framework (free; no CCU; active 2026 — 4.7.3R Sep 2026; explicit Unity 6/6.5 support; ~9.4k Discord).
- `PredictionRigidbody` wraps Unity Rigidbody with server-authoritative prediction + reconciliation; `TimeManager` takes over the physics tick at the fixed 30Hz — direct mapping onto ADR-0001. Headless Dedicated Server + `ServerManager.Start on Headless`; WebGL client via WSS transports (Bayou/PlayFlow) against a native Linux server.
- Gameplay systems (health, till, orders, guns) stay networking-agnostic behind seams; FishNet lives behind the netcode spec's adapter layer.

## Consequences

- v1's "custom headless app later" option in ADR-0001 is superseded: FishNet ships the server-loop and prediction pieces.
- Room-code lobby (one-screen UI) sits on FishNet.
- The Blender art pipeline is untouched: FishNet tags scene objects/prefabs with `NetworkObject`/`NetworkTransform` + a predicted collision body; no re-export.

## Alternatives considered

- **PurrNet** (MIT, free): excellent, Composite Transport (one server, Web Transport + UDP) is arguably better for mixed WebGL+desktop — runner-up; younger and much smaller community, prediction is a separate evolving addon (higher risk on the hardest part). Revisit if FishNet prediction proves insufficient.
- **Photon Fusion 2:** CCU billing + Photon Cloud lock-in, rejected.
- **Unity NGO + Relay:** official/free but no built-in client prediction/reconciliation — the hardest part would be hand-rolled, rejected.