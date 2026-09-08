# AAZ-Mobile — shape of the app

## Recommendation: hex-crawl map with diorama scenes

You had no preference between the three shapes, so this is the call and the
reasoning behind it. Build an **HD-2D hex map you move a token across, with
composed diorama scenes for the moments that matter** — arriving somewhere,
an encounter, an anomaly, a find.

Not free-roam Octopath-style, and not a node crawler.

### Why

**It is what the tabletop already is.** Reviewers describe a solo survival hex
crawl. A hex map with lazy resolution maps onto that one-to-one, so faithfulness
costs almost nothing. Free-roam would mean inventing a whole spatial layer the
rules do not have, and then reconciling it with the rules that do exist.

**It dodges the expensive part.** The costly stage of any HD-2D project is building
3D environments — see `CONVERSION-PLAN.md`. Free-roam needs continuous, walkable,
seamless sets. A hex crawl needs a handful of reusable composed dioramas, one per
location archetype, re-lit and re-dressed. That is an order of magnitude less art
and level work for a look that is arguably *better*, because every shot is framed
rather than wandered through.

**HD-2D is at its best framed.** Tilt-shift, bloom and a long lens are the language
of a composed shot. A diorama you look at rewards them; a world you run around in
fights them — the focus band has to keep moving and the effect gets weaker to stay
readable.

**It fits the mobile budget.** One diorama on screen at a time means a tight shadow
distance, few lights, and a fixed camera whose tilt-shift band can be tuned per
scene rather than compromised for all cases.

### The cost of choosing this

Less moment-to-moment agency than free-roam. If what you actually want is the
feeling of *walking* the Zone, this is the wrong shape and the extra art cost of
free-roam is the price of getting it. Say so and I will re-plan — this is a
reversible decision right now and an expensive one later.

## Layers

```
  Presentation   HD-2D map view · diorama scenes · UI      (Packages/com.aaz.hd2d)
  ───────────────────────────────────────────────────────
  Rules          resolution · combat · upkeep · operative  (Packages/com.aaz.rules)
  ───────────────────────────────────────────────────────
  Content        RandomTable assets transcribed from book  (not written - needs the tables)
  ───────────────────────────────────────────────────────
  Core           dice · tables · hex · map · save          (Packages/com.aaz.core)
```

Three layers are built. Content is deliberately empty: the tables are the game's
substance, they are data rather than code, and the part of the book carrying them
has not been supplied.

## What is built

### `Packages/com.aaz.core` — rules-agnostic primitives

| Type | Purpose |
| --- | --- |
| `Rng` | Deterministic xorshift32. A seed replays identically on every device; `System.Random` does not guarantee that. Unbiased `Below(n)` by rejection sampling. |
| `DieKind` / `RandomTable` | Printed tables as editable assets, indexed by d6 / 2d6 / d66 / d100. `Validate()` reports gaps, overlaps and rows outside what the die can roll. |
| `HexCoord` | Axial coordinates, cube-space maths, rings, spirals, pointy-top XZ world placement and the inverse. |
| `HexMap` | Fog of war (scouted vs. explored), dictionary lookup with list-backed serialisation. |
| `ZoneRun` | One expedition, JSON-serialisable, generator state persisted alongside the map. |

Two decisions worth flagging:

- **Content resolves lazily, on entry.** That is how the tabletop plays — you roll
  when you arrive — and it means a run is fully described by its seed plus the
  order you walked it. Cheap saves, reproducible bug reports.
- **`ZoneRun` persists generator state, not just the seed.** Mobile sessions get
  killed constantly. Restoring only the seed would re-roll luck the player already
  spent; restoring the state continues the same die sequence.

### `Packages/com.aaz.hd2d` — rendering

Lit billboard sprites, tilt-shift depth of field, diorama camera rig. See its
README.

## Verification status

| | |
| --- | --- |
| Dice distributions, d66 faces, determinism | **Validated** — reference implementation run in Python before porting |
| Hex distance, rings, spirals, world round-trip, cube rounding | **Validated** — 2601 centre round-trips and 250k jittered points, zero failures |
| Flat-top orientation and north–south axis | **Validated** — bearings 30/90/150/210/270/330, re-checked after the orientation fix |
| Exploding dice expectation | **Validated** — 4.2035 measured against the analytic 4.2 |
| Damage curve (divide-by-Level) | **Measured** — see the tables in `RULES.md` |
| C# compilation | **Not verified** — no .NET or Unity toolchain in this environment |
| Shaders | **Not verified** — same reason |

The algorithms are checked; the C# is a direct port of checked algorithms but has
never been through a compiler. Expect to fix import-time errors, not logic errors.

## Next, in order

1. **The rest of the rulebook.** The supplied text cuts off in the Scavenging rules;
   see the closing section of `RULES.md` for exactly what is missing.
2. Transcribe the tables into `RandomTable` assets. Mechanical work, and the
   validator catches transcription mistakes.
3. Correct the twelve inferred skill names against the book.
4. Wire the expedition loop on top of `Expedition` and the exploration tables.
5. First diorama end-to-end for one location archetype, to price the art.
6. Tune the volume profiles per region.
