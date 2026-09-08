# AAZ-Mobile

A personal, unreleased digital play-aid for the solo tabletop RPG
**Alone Against the Zone** (Alexey Aparin / Andrea Sfiligoi, Ganesha Games, 2026),
rendered in an HD-2D style. Not for distribution.

## Layout

| Path | What it is | State |
| --- | --- | --- |
| `Packages/com.aaz.hd2d` | HD-2D rendering: lit billboard sprites, tilt-shift depth of field, diorama camera rig | Built |
| `Packages/com.aaz.core` | Rules-agnostic primitives: deterministic dice, random tables, hex map, run state | Built |
| `Packages/com.aaz.rules` | Rules engine: operative, skills, afflictions, resolution, combat, expedition upkeep | Built |
| — | Content: terrain, encounter and item tables, Events | Blocked on the missing pages |

Read [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) first — it explains the chosen
shape (hex-crawl map plus diorama scenes) and why. [`docs/RULES.md`](docs/RULES.md)
records the implemented core mechanics, the measured combat curves, and the
assumptions that still need checking against the book.
[`docs/CONVERSION-PLAN.md`](docs/CONVERSION-PLAN.md) holds the HD-2D staging and
the mobile performance budget.

## Still needed: the rest of the rulebook

The complete core rules are now implemented. What is still missing is content: the
nine terrain tables, the Encounter and Item tables, the Events, and the Locations.
Most useful next are the **Weapons and Protective Suits summary tables**, which carry
modifiers the combat engine already accepts but cannot yet be given. See the closing
section of [`docs/RULES.md`](docs/RULES.md) for the full list.

Note that file-sharing hosts are unreachable from this environment (Drive,
pixeldrain and Dropbox are all refused by the network policy). Only GitHub is
reachable, so commit the pages to this repository or paste the text directly.

## Verification status

Dice and hex algorithms were validated against a Python reference implementation
before porting. The C# and the shaders have never been compiled — there is no
.NET or Unity toolchain in this environment. See the table in
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md#verification-status).
