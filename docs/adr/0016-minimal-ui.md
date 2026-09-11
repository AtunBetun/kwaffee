# ADR-0016: Minimal UI — one screen to start, HUD stays thin

## Status

Accepted (2026-09-10, owner-directed).

## Context

Owner directive: "We also need an EXTREMELY simple UI so we can start the game, etc..." v2 adds a lobby (name, room code, start) that v1 never had, and the HUD must stay readable in a fast co-op party game.

## Decision

- **One start screen, three inputs:** enter name → enter/shorten a room code (or create a room) → press START → join the room's lobby → shift starts when the host starts (or when full). That is the entire pre-game surface.
- **HUD, top priority thin:** shared till + shared quota/clock (one line), order board (compact), crosshair + ammo + interact prompt when holding a gun, player HP only when hurt. Everything else is world-space (prompts) or hidden until events.
- **Prompts are world-space text** ("[E] Pick up", "[E] Serve", "[Anyone] tip") to avoid a settings screen; gamepad/keyboard sprites adapt automatically.
- No menus/graphs/settings screens in the MVP; debug console overlays; DOTween for all UI motion.

## Consequences

- Lobby spec owns onboarding (laugh-in-60s: name + code + start + first order visible within a minute).
- HUD spec owns in-shift readability (till/quota/order board/crosshair).
- No inventory-grid/map/social screens in scope.

## Alternatives considered

- Matchmaking without codes — rejected: 3-4 friends use a short code; simpler to build and to understand.
- Traditional main menu — rejected: one screen, per owner.