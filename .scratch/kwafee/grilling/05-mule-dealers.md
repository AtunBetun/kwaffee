# Artifact 5 — The Mule & dealers

- Beads: `kwaffee-026`. Slug: `05-mule-dealers`.
- Grilled scope: BEAN RUN trigger/driver, route length + pacing, driving feel (launch/boost + pigeon), haggle mechanic, dealer memory/grudge boundary vs Artifact 6, Ma Paddy chase scope, non-obvious dealer choice.
- Lens: friendslop. Cheapest version still genuinely fun for 3-4 friends. Comedy over fidelity. Shrink over expand. One way to do a thing.

---

## Design tree (decisions resolved)

### 1. Who gets drafted for the run?
Options: floor vote UI / free volunteer / Vinny drafts lowest-tips / rotating schedule.
**DECIDED:** When beans hit 0, Big Vinny drafts the player with the fewest tips that shift ("You ain't worked a lick, kehd. Mule time."). Any other player can horn the van (V key, already bound) to STEAL the run from them. No vote UI, no schedule. Rationale: zero new UI, gives the low-score player redemption agency instead of punishment, and the horn-steal is a free griefing moment reusing the STEAL ethos — pressure with comedy, cheap.

### 2. When does the run trigger? How often?
Options: random beans-dry moment / fixed 60%-through-shift marker / always.
**DECIDED:** Trigger is machine-driven: beans hit 0 once per shift, scripted so it lands ~60% through the 3-4 min shift (bean meter drains on a seeded schedule, not random RNG past the seed). One run per shift, period. Rationale: "run pressures without owning the shift" — one predictable interlude is the pacing envelope; randomness would catch the shift at the wrong moment.

### 3. Route length and structure.
Options: open free-roam city / long chase / short linear street with fork.
**DECIDED:** One street segment in the SAME scene, no scene swap, no load. Shared stretch (~20s active driving: cones, potholes, pigeon), then a 3-way fork for dealer choice, then dealer beat (~10s incl. haggle). Total interlude ≤45s of real time, ~30s of it active driving. Return is a cut: beans auto-load, horn, Vinny one-liner, back in shop. Driver's seat = camera swap; shop stays rendered and live so the other 3 players keep serving (that IS the pressure). Rationale: no asset-load hitch at 60fps, other players keep working during the run, and the interlude can't swallow the shift's clock.

### 4. Driving feel model.
Options: sim steering / on-rails auto-forward minigame / arcade steer + charge-boost.
**DECIDED:** Arcade, reuse existing muscle memory. WASD/arrow steer left-right, auto-forward speed, HOLD to charge a BOOST, release to lurch — boost charges on the exact FLING charge curve so release timing already lives in the player's hands. Van is a rounded bean: steers like a wet bar of soap, oversteer fishtails, potholes buck it with squash-stretch, landing squashes it (phys language shared with bodies). Cones are smashable (pop, bounce, +laugh, never a hard stop). Rationale: reuse of the charge curve means zero new control vocabulary and the "lose control = funny" jank reads as comedy, not work. Nothing about the drive requires precision — the joke is the physics.

### 5. The pigeon.
Options: random on-route hazards / avoidable static obstacle / scripted terminator at one point.
**DECIDED:** ONE scripted pigeon, perching at the same point on the shared stretch every run. Googly eyes, felt-fluff wings, zero fear. Boost-leap over it or it "ends you": van tumbles, beans scatter, ~3s delay, restart just past it, Vinny roast ("The pigeon has your numbah, kehd."). Deterministic per seed, replayable, never removed — it becomes the run's recurring boss gag. Rationale: a single guaranteed gag beats a zoo of hazards; repeatable = the group's inside joke builds across shifts.

### 6. Haggle mechanic — reuse FLING?
Options: dedicated haggle minigame / dialogue choice / fling the coin bag with existing FLING physics.
**DECIDED:** YES, reuse FLING physics verbatim. The haggle is one throw: pick up the coin bag, charge-fling it onto the dealer's scale. Scale has a target zone; bag weight + landing position decide the paid/underpaid result. Zero new physics code — the coin bag is a heavier throwable in the same rigidbody pool. Rationale: cheapest possible; the haggle already inherits all the floaty-funny flight comedy from Artifacts 1-2, and one throw keeps the beat at ~10s.

### 7. Dealer memory / grudge — boundary vs Artifact 6.
Options: build full NPC memory store here / defer entirely / minimal shared contract.
**DECIDED:** Artifact 5 owns the minimal persisted per-cafe `DealerMemory` JSON (shape: `dealerId → { perPlayerShortCount, perPlayerGrudgeLevel, tradeCount }`), the price function `price = dealerBase(quality) * (1 + 0.25*grudgeLevel)`, and the chase trigger flag. Artifact 6 owns dealers as living NPCs: utility pickers, state machines, dialogue personality, gossip, reaction lines. Boundary: Artifact 5 ships the store + the math; it must NOT build NPC personality or dialogue. Store is the same single per-cafe JSON the spec's NPC memory uses, so Artifact 6 consumes it without rework. Rationale: clean data contract, no duplicate persistence, no Artifact-6 work smuggled into 5.

### 8. Ma Paddy chase scope.
Options: open-world pursuit sim / she never actually chases (dialogue only) / scripted comedic burst.
**DECIDED:** Scripted burst, not simulation. On underpay, Ma Paddy bursts out on a mobility scooter and chases for a fixed 8s comedic beat: van is faster (she never catches; chased = rear-view shot, shaking fist, scooter wobble, gaining on the pothole dips, then she drops off-frame swearing). Repeated underpaying raises grudge: chase gets one more absurd beat per tier (cap 3) — her scarf catches a cone, she bumper-cars a hydrant, etc. Grudge (price rise) persists via the memory store regardless. Rationale: "comedically persistent" is a performance, not a pursuit sim; fixed beat is cheap, repeatable with escalating gags, and it's a guaranteed replay-candidate highlight.

### 9. Dealer identities & what makes the choice non-obvious.
Options: all identical outcomes / one obvious best / 3-axis trade + per-player memory.
**DECIDED:** Three axes that never align on one winner:
- **Big Sal's Specialty Beans** — top quality, honest scale, highest price (real tip cost). Good-beans bonus: fewer customer complaints + small tip bump for the rest of the shift.
- **Rosie's Clean Beans** — cheapest, dependable, boring: no surprises, no bonus, no comedy. The never-wrong-wrong choice.
- **Ma Paddy's Pantry** — pre-war beans: seeded comedy outcome — vintage delight (big tip swing + fevered customers) OR chaos (beetle in a cup, customer flees, filth spike). Risky. Her displayed price is per-player: the grudge multiplier is hidden until you see her "special for YOU" price tag — the reveal IS the comedy and the memory signal.
Non-obviousness comes from: price vs quality vs variance vs an invisible per-player modifier. Rosie is safe-boring, Sal is pay-to-win-good, Ma Paddy is gamble-with-history. No dominant strategy; the "obvious" pick changes per player per shift. Rationale: choosing becomes a social argument at the table, not a spreadsheet read.

### 10. What the beans do for the rest of the shift (stakes).
Options: cosmetic only / flavor text / mechanical impact.
**DECIDED:** Mechanical, immediate. Sal's quality: +tip small, −complaints. Rosie's: baseline. Ma Paddy's: tip swing (big ±) and a chance of an instant chaos event (beetle, stink, "vintage" crowd-gasp). All outcomes land in the SAME shift so the choice carries consequences within reach of memory. Rationale: a run with zero consequence is a coaster; same-shift stakes make the dealer pick a real decision and feed the tabloid/roast pipeline.

### 11. Shady-beans comedy scoring (measurable gate).
**DECIDED:** The /sim harness records every run against an explicit laugh-event list: pigeon tumble fired, boost-leap over pigeon, cone-cascade (≥3 cones), Ma Paddy chase triggered, Vinny roast fired, tip-swing surprise (Ma Paddy outcome queued), beetle/chaos event, "special for YOU" price reveal happened. A run counts "funny" if ≥1 laugh event fired. **Gate: ≥70% of seeded runs funny.** Rationale: an enumerable event list is the only honest way to measure "funny ≥70%" without a human in the loop; the human laugh re-test belongs to Artifact 8.

### 12. Chase as a highlight (measurable).
**DECIDED:** Chase and pigeon-tumble events push into the CATASTROPHE REPLAY candidate list (artifact 8 consumes it). /sim asserts: underpay → chase triggers 100% of the time, and chase events are always replay-candidates. "Highlight" is evidenced by replay-candidacy + occurrence rate, verified by a scripted underpay sim run. Rationale: wires the delivery axis now so Artifact 8 cannot silently drop van moments.

---

## Scope cuts (explicitly OUT)

- No free-roam city, no GPS/minimap, no traffic AI, no multiple routes. One street, one fork.
- No shop-visible-in-drive interaction: while driving you are a van, not a barista. Other players keep serving.
- No haggle dialogue trees, no counter-offer rounds. One fling or nothing.
- No dealer buddy/friendship system, no dealer-romance, no dealer upgrade shops. Grudge only.
- No open pursuit sim; chase is a fixed scripted beat.
- No dealer minigames beyond the scale fling.
- Van is not a persistent vehicle between shifts; it's a run prop with a growing dent. No garage, no van upgrades (Pigeon-Powered Van Turbo stays a tabloid upgrade gag, game-state only if ever).

---

## Refined acceptance criteria (Artifact 5 gate, in artifact's own language)

1. Beans dry exactly once per shift, seeded ~60% through. One run per shift. `/sim` asserts one interlude per 3.5-min shift, plus no second trigger.
2. Interlude ≤45s wall time from draft to back-in-shop; ≥1 laugh event (list in §11) fires in ≥70% of seeded scripted runs. Block: drive-with-no-fun, interlude swallowing shift time, repeatable same-pick dominance.
3. Haggle is exactly one coin-bag fling using the artifact-1 FLING charge curve; scale target zone decides paid vs underpaid; underpaid is reachable (bag lands light/short) and paid is reachable (sweet spot). Block: no haggle, no underpay path, new physics path.
4. Ma Paddy grudge: underpay increments per-player short count; price multiplier `1 + 0.25*grudge` applies to THAT player's price on subsequent runs; chase burst triggers 100% on underpay; chase + pigeon-tumble both produce replay candidates. Block: memory doesn't persist per cafe, chase not triggered, price not per-player.
5. Dealer choice non-obvious: /sim 20-shift sweep shows each dealer picked <50% of shifts, and per-player prices diverge for players with different grudge history. Block: >50% single-dealer dominance, choice reducible to a dominant strategy.
6. During the run, shop continues producing serves; /sim tracks tips/serves during interlude and shows the shop does NOT stall (serve-capability maintained by remaining players). Block: run halts the whole shift.
7. No per-frame allocation during the drive scene; van, cones, coin bag are pooled; 60fps at 4 players maintained (drive adds ≤N objects render-batched, static street geometry merged). Performance regression = blocker, per standing rule.
8. All visible assets authored via the Blender bpy pipeline, manifest-validated (required-name list, ≤35,000-triangle cap, origins grounded y=0, per-submesh material indices). Block: any external/downloaded asset, any unvalidated mesh.

## Blender art needs (bpy pipeline, all manifest-validated)

- `TheMule` — the van: rounded glossy-candy bean, cookie wheels, parameterized DENT (scars.depth/count grow each shift) — required.
- `DealerSal` / `DealerRosie` / `DealerPaddy` — three bobblehead dealer bodies, felt aprons, distinct silhouettes (Sal: tall thin; Rosie: squat apron; Ma Paddy: wider, shawl) — required x3.
- `CoinBag` — candy-glossy payload sack (throwable, heavier cup-class) — required.
- `Scale` — dealer counter scale with target zone — required.
- `RoadCones` (pack of 3-4) and `Potholes` (road geometry with splash-squash trigger) — required.
- `ThePigeon` — googly-eyed felt pigeon, spring-bobble idle — required.
- `MobilityScooter` — Ma Paddy's chase ride — required.
- Route street/curb/storefront geometry as static merged bpy meshes (one combined `MuleStreet`, `MuleStorefronts` per dealer fork) — required.
- All share the clay+candy+felt 3-material palette; nothing reads as a generic city asset.

## Dependencies (from earlier artifacts)

- **Artifact 1:** FLING charge curve (unconditionally reused for coin-bag haggle AND the van boost), rigidbody pool, per-player `TipLedger` (drives the draft: lowest tips = drafted).
- **Artifact 2:** STEAL verb semantics (horn-steal of the run), griefing permissions.
- **Artifact 3:** Vinny roast trigger table (run narration lines: draft, pigeon, chase, underpay reveal).
- **Artifact 4:** machine/bean drain that owns the "beans dry" trigger timing at 60%; machine-bean coupling defines what a run is FOR.
- **Artifact 6 (consumer):** consumes the `DealerMemory` JSON contract for dialogue/gossip/personality — Artifact 5 does not ship those.
- **Artifact 8 (consumer):** replay-candidate list accepts chase + pigeon-tumble.
- **Standing:** Unity 6 + Blender 5.2.1 bpy pipeline, main-branch-only, per-file ownership, human Unity license/WebGL prereq unchanged (never retry batchmode licensing).

## Risks (stated loudly)

- **Draft reads as punishment** → frame is redemption + horn-steal griefing; if /sim shows drafted player's post-run tips never recover, revisit draft to "random among bottom half."
- **Drive drifts into work** → the boost power-fantasy is the fun; if ≥70% funny metric fails in sim, cut the route to ~15s before adding ANY mechanics. Shrink, never add.
- **Pigeon becomes rage, not gag** → it restarts you just past it with a roast; it may never kill a shift (beans on timer pause during tumble). Keep the ~3s cap.
- **Haggle feels identical to cup-fling** → acceptable; the scale target is the differentiator. If RATER flags staleness, add a wobble factor on the scale plate (cheap), not a new mechanic.
- **Grudge snowballs into unfun** → cap grudge at level 4 and let it decay 1 level per shift where the player buys from Ma Paddy at full price. Converges, stays personal.
- **Perf during drive** → street + storefronts merged into ONE static mesh each; cone/coin/pigeon pooled. Van rear-view mirrors are fake (no extra view render). Hitch = blocker.
- **Boundary bleed with Artifact 6** → Artifact 5 ships NO dialogue/personality beyond the single Vinny lines the run needs; any dealer line is Artifact 6 scope. Enforced by acceptance criterion.