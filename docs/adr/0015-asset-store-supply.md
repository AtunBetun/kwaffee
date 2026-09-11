# ADR-0015: Asset Store free assets as the art/audio/VFX supply — wiring over building

## Status

Accepted (2026-09-10, owner-directed).

## Context

Owner directives: "There is a shitload of free assets in here", "we build as little as possible, just wire up code", "All of our effort just needs to be on stitching all of these assets together to make the gameplay loop, like we are mostly just writing code and putting the scenes together." Verified on the store 2026-09-10: First Person + Third Person | Character Controllers (Unity Starter Assets), Low Poly Shooter Pack - Free Sample, Low Poly Pistol/SMG/Shotgun packs, Simple Crosshair Generator, DOTween, In-game Debug Console, Cartoon FX Remaster Free, Footsteps, Regular Impact Sounds, Coffee Shop Props (free), Food/Furniture/Weapons FREE low-poly packs, Fish/Boats low-poly, Particle Pack | Starter Assets.

## Decision

- **Source everything replaceable from the Unity Asset Store (free)** before building it: movement controller, weapons, characters, low-poly furniture/food/props, water/fish/boats, crosshair UI, DOTween, particle packs, impact/footstep/gun audio, debug console.
- **Adapters, never ownership:** game code depends on our own seams (`PlayerMotor`, `WeaponController`, `HealthComponent`, `IInteractable`, `ImpactSystem`, `PlayerInventory`, `CurrencyWallet`…); each imported asset hangs behind our API. Asset upgrades/replacements never rewrite game code.
- **One authority per capability:** exactly one movement controller (Unity Starter Assets), exactly one networking framework (FishNet, ADR-0014). No competing frameworks glued together.
- **Human-install gate:** exact package names/IDs are verified store listings (`.scratch/kwafee/research/assets-2026.md`); the human installs/samples them in Unity (asset licensing requires an account agreement); the build then wires prefabs from those installed packages.
- **What we still build ourselves:** the gameplay loop code (shift/orders/till), interaction raycast system, health/damage, gun fire control (thin), physics carrying (thin over Rigidbody), UI HUD, lobby screen, and the Boston voice layer. Small components, ScriptableObject definitions, testable logic, replaceable assets.
- **Blender bpy pipeline (ADR-0006) remains** for anything the store cannot supply (e.g., a signature espresso machine or the shop cat), never as the default for generic props.

## Consequences

- Asset installs happen once per capability, human-confirmed; tickets reference store names + IDs, not internal prefab paths.
- Imports live under `Assets/ThirdParty/`; our code under `Assets/_Game/` (v1's `KwaFee/` layout is retired).
- No stability guarantee from third-party assets: adapters are the contract.

## Alternatives considered

- Keep authoring all art via Blender — rejected: owner wants store assets first, wiring second; authored art is only for gaps.
- Pull paid assets — rejected: owner said free assets; free packs verified.
- Tight-couple to asset architecture — rejected: owner's own rule "third-party assets must not dictate the codebase".