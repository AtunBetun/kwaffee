# Artifact 7 — Visual identity

- Beads: `kwaffee-x0i` (Blocked by: 06)
- Grilled scope: the LOOK pass — every model/prop/screen vs the 8 hard rules, palette lock, silhouette test, wobble-language consistency — as a design adversary. No interviews; all branches DECIDED.

---

## Design tree (decisions resolved)

### B1. What ships vs what is audited-only
**Q:** Artifact 7's gate demands 5 traceable characters, but only Barista + Customer exist; the Shop Cat is not authored and the neon sign / van / dealers are declared future artifacts. Expand now or audit on paper?
**Options:**
- (a) Ship only the 12 existing meshes + audits on paper — gate's "5 characters" unmet; fails RATER.
- (b) Author every planned asset now (van, neon, 3 dealers, inspector, landland) — violates friendslop / steals artifact 5/6/8 scope.
- (c) Ship what exists + the minimum missing cast that the gate itself requires, and audit future assets via pipeline conventions (the validator is the convention).
**DECIDED:** (c). Ships 12 existing + Shop Cat + Big Vinny bust + ONE Dealer base = 5 characters (Barista, Customer, ShopCat, VinnyHead, Dealer) + the 4 machines as living props. Neon sign, van, inspector, landlord, street NPCs, 3-dealer tint variants: audited only, through the same `look` fields + validator they MUST pass when authored (B3). Rationale: any smaller roster cannot clear the gate; any larger roster steals future artifacts' work.

### B2. Are the 4 machines "characters"?
**Q:** The silhouette blocker is "two characters reading as same silhouette." Do machines enter that test?
**Options:** (a) machines are characters (they vibrate with life, have voices, are named); (b) machines are props and only the 5 beings are characters.
**DECIDED:** (b) roster-class "machine": tested pairwise against each other AND against characters in the numeric silhouette gate, but the gate's "5 characters" is met by the 5 beings (B1). Machines also get one googly eye puck each (rule 2 — "everything that lives" includes the machine quartet's souls) and a `vibrate` bobble class (rule 6). This matches the spec's own cast taxonomy (machines live under World Rules, not NPC Cast).

### B3. Audit shape — where the LOOK check lives
**Q:** "EVERY model, prop, screen checked against the 8 rules" — doc checklist, machine check, or both?
**Options:** (a) prose doc only; (b) extend the art manifest + `CoreSceneBuilder.Validate` with per-mesh `look` fields; (c) both, machine-enforced minimum + rendered table as RATER evidence.
**DECIDED:** (c). Each mesh carries `look: { parent: "clay"|"candy", rules: [1..8], trace: [{rule, why}], silhouetteId?, bobble?, eyes? }` in `core-meshes.json`. Two enforcement points: build-time `tools/kwafee/audit_look.py` (cheap, catches drift at authoring, prints the mesh×rule traceability table) and import-time `Validate` (Unity refuses to import a non-compliant manifest). The narrative "why each decision traces to a named parent" lives in EVIDENCE.md + the RATER-facing sheets, not in code. Rationale: prose rots; the validator is the only thing a future asset author cannot skip.

### B4. Palette lock mechanism
**Q:** Colors live inline in `build_art.py` as float tuples. Lock them how, where, enforced how?
**Options:** (a) leave inline, trust authors; (b) `tools/kwafee/palette.py` (bpy helper, hex→float) + committed `palette.json` (hex, canonical), validator diffs manifest colors vs json; (c) (b) plus the flat-UI side consumes the same json.
**DECIDED:** (b) + (c) hook. `palette.json` is the single source of truth: `palette.py` imports it for all bpy scripts (deletes the inline `PALETTE` dict — one convention, no second source); `CoreSceneBuilder.Validate` loads `Art/palette.json` and rejects any manifest material whose color deviates > 0.01 per channel from its named palette key; artifact 11's CandyUI consumes the same json so screens and meshes cannot drift. Material naming encodes the key (`<class>_<paletteKey>`: `clay_cream`→`cream`), so validation is a name→hex map, no heuristics. Rationale: a committed json both bpy and Unity read is the smallest thing that cannot drift.

### B5. Silhouette test method
**Q:** The gate needs "no two characters read as the same silhouette" + "any 5 chars/5 props trace to a parent rule." What is the verification artifact and what is the pass condition?
**Options:** (a) RATER eyeball on a rendered sheet, no numbers; (b) numeric pairwise silhouette IoU from orthographic renders + the sheet as evidence; (c) both.
**DECIDED:** (c). Extend the existing `preview()` renderer into `tools/kwafee/silhouette_check.py`: renders (1) a full-lit color contact sheet (palette/rule eyeball, RATER evidence) and (2) a flat-black silhouette sheet (all materials forced to one ink material, ortho front 3/4 view, cropped to bbox). Numeric gate: normalized pairwise silhouette IoU < 0.60 for character pairs, < 0.50 for machine pairs, both also checked characters-vs-machines. Thresholds are tune-and-lock: first run calibrates on the real renders, then the locked numbers are the gate. Sheet PNGs land in `.scratch/kwafee/` as the artifact's evidence. Rationale: the blocker is *objective* shape similarity — numbers catch it every pass; the sheet is what the expensive RATER can actually see.

### B6. Wobble / bobble language
**Q:** "Spring-and-bobble at rest" + "wobble language consistent" — animate in Blender (keyframe rigs exported to GLB) or in Unity (procedural)?
**Options:** (a) bpy keyframes + GLB animation export — heavy, brittle across glTF triangulation, fights rule 7 ("squash & stretch is physics", i.e. runtime deformation); (b) bpy authors only RIG HINTS (pivot, axis, class) in the manifest; one shared runtime `BobbleController` consumes them procedurally — idle bobble (humans), spring sway (cat tail), vibrate (machines) from the same component; (c) both.
**DECIDED:** (b). Blender's job: per-mesh `bobble: {pivot:[x,y,z], axis, class: "bobble"|"spring"|"vibrate", amp, rate}` + named `eyes` slots (position/scale per puck) + `squashPivot` (so future physics scales around the right origin). Runtime: one `BobbleController` + `GooglyEyes` — deterministic (seeded phase), zero-alloc delta sines on Transforms, 60fps-safe. "Wobble language consistent" = every living entity and every machine carries hints and *only* these components move them; no hand-rolled idle anims. Rationale: procedural is cheaper, deterministic, replayable, and the deformation language stays physics-side where rule 7 lives.

### B7. Re-authors in this ticket
**Q:** Which of the 12 existing meshes get touched, and how hard?
**Options:** (a) full remake of all 12; (b) targeted fixes; (c) no touch.
**DECIDED:** (b), targeted: (i) humanoid head proportions retuned to the bobblehead band (head diameter ≥ 0.35 × body height — current Barista ≈ 0.41 already inside, tune not rebuild); (ii) one googly eye puck added to each machine (rule 2); (iii) all 12 backfilled with `look`/`bobble`/`eyes` fields. NOT re-authored: Barista/Customer bodies (already clay + eyes + felt apron — compliant), neon sign (audited: candy glossy + soda-blue + crooked, authored at its own artifact), the van (audited; artifact 5 authors within remaining triangle budget), dealer tint variants (dealer BASE now, tints at artifact 5), inspector/landlord/street NPCs (audited; must pass the same validator when authored).

### B8. Screens
**Q:** Artifact 7 checks "every … screen," but the UI build is artifact 11 (onboarding/lobby) and the HUD currently is IMGUI.
**Options:** (a) restyle everything now; (b) define the LOOK contract for screens now, restyle in 11; (c) ignore screens here.
**DECIDED:** (b). Artifact 7 locks the `CandyUI` token contract: palette from `palette.json`, corner radius ≥ 8% of element min side, chunky border ≥ 3px at 1080p, one shared bounce tween curve, huge icons. The existing IMGUI HUD and awards text screen are checked against the tokens and flagged for restyle; the restyle itself is tracked work for artifact 11 (its named scope). Rationale: deriving the tokens is this artifact's job; building every screen is artifact 11's.

### B9. Build pipeline shape
**Q:** ADR-0005 mandates per-asset bpy scripts importing shared helpers; today `build_art.py` is one monolithic script producing all 12 meshes + manifest in one run.
**Options:** (a) keep one file, add 3 builders; (b) split shared helpers (`kwafee_art.py`) + per-asset scripts + a manifest merge step.
**DECIDED:** (b). New layout: `tools/kwafee/kwafee_art.py` (palette import + cube/uv/cyl/torus/join/eyes + look-field helpers), `build_art.py` (12 existing), `build_cat.py`, `build_vinny.py`, `build_dealer.py` (each writes its own output + own manifest fragment), `assemble_manifest.py` merges fragments into the one `core-meshes.json` the Unity importer consumes. Matches ADR-0005, keeps parallel headless Blender runs safe (separate processes, per-file ownership), and the importer contract is unchanged.

### B10. Material-class guard (rule 3)
**Q:** "Three materials, nothing else" — how does the validator know a mesh isn't sneaking a 4th material class?
**Options:** (a) trust the manifest's `materials` array; (b) name-prefix + whitelist enforcement.
**DECIDED:** (b). Material names must be `clay_*`, `plastic_*`, `felt_*`, or on the explicit specials whitelist (`coffee`, `ink`, `sign_red` — candy-class). `audit_look.py` and `Validate` both reject unknown prefixes, and one shared 3-material-class matrix (clay = matte bodies/furniture/env, plastic = glossy props/eyes/machines, felt = cat/aprons) is documented in the `look` schema. Rationale: prefix rules are the cheapest guard against scope-creep materials.

### B11. Triangle budget
**Q:** New assets (cat ~3k, Vinny ~1.5k, Dealer ~2k, 4 eye pucks ~0.5k) push 17,112 triangles toward the 35,000 cap.
**Options:** (a) no policy; (b) allocate and state remaining headroom.
**DECIDED:** (b). Forecast ≈ 24k after artifact 7 (≤ 35k cap, validator-enforced). Remaining ≈ 11k is reserved for the van (artifact 5), dealer tint variants (cheap — tint swaps, not geometry, per rule 1 "painted-on tint"), and inspector (artifact 6). Future asset scripts MUST report triangle deltas at merge; a cap breach blocks import. This is the loud assumption future artifacts build against.

---

## Refined acceptance criteria (additions to the artifact gate)

1. Roster: manifest must contain exactly the 15 required names (Cup, Barista, Customer, Counter, Floor, Wall, Tray, Sign, Espresso, Grinder, SteamWand, IceMachine, ShopCat, VinnyHead, Dealer); unknown/duplicate/missing → import fails.
2. Every mesh carries `look.parent ∈ {clay, candy}`, non-empty `rules ⊆ {1..8}`, `trace` with ≥1 named rule for the 10 gate entries (5 characters + 5 named props: Cup, Tray, Sign, Espresso, Grinder), and a valid material-class prefix.
3. Palette lock: `palette.json` canonical; every manifest material color within 0.01/channel of its palette key; validator rejects drift. Material additions require a `palette.json` edit first.
4. Silhouette: 5 characters each have a unique `silhouetteId`; pairwise normalized IoU (ortho, ink-silhouette sheet) < 0.60 among characters, < 0.50 among the 4 machines, characters-vs-machines both; sheets written to `.scratch/kwafee/` as evidence. Thresholds tune-and-lock on first run, then fixed.
5. Bobble: every living entity (Barista, Customer, ShopCat, VinnyHead, Dealer) + every machine carries `bobble` hints; heads diameter/height ≥ 0.35 on all humanoids; Vinny ≥ 1.25× Barista's head ratio (the "20% head" showcase); runtime uses only `BobbleController`/`GooglyEyes` — zero idle anims outside them.
6. Rule-3 classes: exactly 3 classes + specials whitelist; felt is now a real body material (ShopCat), not just the apron trim.
7. Triangle cap: total ≤ 35,000 after additions (forecast ≈ 24k); validator-enforced; future deltas reported at merge.
8. Screens: CandyUI token contract documented and consumed from `palette.json`; existing HUD/awards screens flagged against tokens; restyle tracked to artifact 11.
9. Grounding + finiteness: new meshes keep bounds.min.y == 0 (cat paws, Vinny bust base, dealer feet) — existing validator rule extends to new names with no change.

## Blender art needs (bpy-pipeline authored, manifest-validated)

1. `tools/kwafee/palette.json` + `tools/kwafee/palette.py` — canonical palette module (all scripts import; Unity `Validate` reads the json).
2. `ShopCat` mesh — felt body, googly eyes, spring-pivot tail, `bobble: spring`, `squashPivot`.
3. `VinnyHead` bust mesh — oversized head (the 20%-head rule), clay + candy eyes; consumed by the artifact-8 narration banner.
4. `Dealer` base mesh — one body, tint-slot convention (3 palette variants at artifact 5), `bobble: bobble`.
5. 4 machine googly-eye pucks (rule 2) + `bobble: vibrate` hints on Espresso/Grinder/SteamWand/IceMachine.
6. Head-proportion retune on Barista/Customer; `look`/`bobble`/`eyes` field backfill on all 12 existing meshes.
7. `tools/kwafee/kwafee_art.py` shared helper module; `build_art.py` refactor onto it; `build_cat.py` / `build_vinny.py` / `build_dealer.py`; `assemble_manifest.py` merge.
8. `tools/kwafee/audit_look.py` (trace table + rule-3/prefix/palette compliance) and `tools/kwafee/silhouette_check.py` (color + ink-silhouette contact sheets, pairwise IoU, numbers to stdout + PNGs).
9. `CoreSceneBuilder.Validate` extension: look fields, silhouette uniqueness, palette drift, cap (triangle count already enforced).

NOT authored now (audited via conventions only): neon sign, the van, inspector, landlord, street NPCs, dealer tint variants.

## Dependencies + risks

- **Inputs (this audits them):** Artifacts 1–4 outputs — the 12 meshes, 11 materials, manifest, validator, import tool. Artifact 6 informs ShopCat/Vinny/Dealer character design (cat behavior states → which body parts move; Vinny's narrator slot). No /sim or netcode dependency.
- **Human prereq:** Unity Personal license + WebGL module still pending — the art import runs via in-editor MCP (`Import Authored Art`), which is not a batchmode build and is unblocked (host Play Mode already authorized). Re-import + CoreSceneBuilder rebuild required after mesh changes; zero gameplay-code change expected (only mesh assets + validator).
- **RATER evidence:** the artifact's gate runs on the contact sheets + trace table + validator runs — all reproducible headless. 3 passes / 2 reviews cap contradiction from the earlier sessions remains an open policy item; do not claim a passed gate under a stretched reading.
- **Risks:** (1) silhouette IoU thresholds may need re-calibration after the first real renders — lock numbers only after the first honest run; treat a false pass as a bug, not a gift. (2) Triangle headroom ≈ 11k after this artifact — the van (artifact 5) is the biggest single future consumer; if the van forces > cap, the van's dent scale or mesh density is the triage point, not the validator. (3) Parallel Blender processes must each write distinct outputs (ADR-0005) — the merge step is the only shared write; keep it file-level atomic. (4) Palette drift risk is only closed if `palette.py` is the *only* place hexes enter bpy — any script that inlines a color regresses the lock; audit_look catches it at build, import gate catches it later. (5) BobbleController/GooglyEyes land as new Runtime components — must stay zero-alloc (no per-frame Vector3/rotation construction); a perf regression here is a blocker per standing orders.