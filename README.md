# AAZ-Mobile

A personal, unreleased digital play-aid for the solo tabletop RPG
**Alone Against the Zone** (Alexey Aparin / Andrea Sfiligoi, Ganesha Games, 2026),
rendered in an HD-2D style. Not for distribution.

## Layout

| Path | What it is | State |
| --- | --- | --- |
| `Packages/com.aaz.hd2d` | HD-2D rendering: lit billboard sprites, tilt-shift depth of field, diorama camera rig | Built |
| `Packages/com.aaz.core` | Rules-agnostic primitives: deterministic dice, random tables, hex map, run state | Built |
| — | Game layer: character, resolution, expedition loop | Blocked on the rules |
| — | Content: tables transcribed from the rulebook | Blocked on the rules |

Read [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) first — it explains the chosen
shape (hex-crawl map plus diorama scenes) and why. [`docs/GAME-BRIEF.md`](docs/GAME-BRIEF.md)
records what is actually known about the game, and is explicit that it is drawn
from public listings rather than the rulebook.
[`docs/CONVERSION-PLAN.md`](docs/CONVERSION-PLAN.md) holds the HD-2D staging and
the mobile performance budget.

## Still blocked: the design document

The doc has been shared twice as a Google Drive link, and Drive is refused by this
environment's network policy. So is pixeldrain, Dropbox, and every other
file-sharing host tested:

```
drive.google.com:443              connect_rejected
drive.usercontent.google.com:443  connect_rejected
docs.google.com:443               connect_rejected
dropbox.com:443                   connect_rejected
pixeldrain.com:443                connect_rejected
```

Reachable: `github.com`, `api.github.com`, `raw.githubusercontent.com`,
`objects.githubusercontent.com`, `codeload.github.com`.

**GitHub is the only working channel.** Either commit the doc to this repository
(a PDF is fine), or paste its text directly into the session.

## Verification status

Dice and hex algorithms were validated against a Python reference implementation
before porting. The C# and the shaders have never been compiled — there is no
.NET or Unity toolchain in this environment. See the table in
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md#verification-status).
