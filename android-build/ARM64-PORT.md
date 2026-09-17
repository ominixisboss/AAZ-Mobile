# arm64 port: analysis and plan

The fork is `armeabi-v7a` only, which cannot install on a 64-bit-only device
(Pixel 7 and later, recent flagships, modern emulator images). This is the
analysis of what it would take to build for `arm64-v8a`, measured rather than
assumed.

## What is NOT a problem

The game's assembly is **ABI-neutral**. All 545 `.s` files -- `data/*.s`, the
110 `sound/songs/*.s` and the 420 generated `sound/songs/midi/*.s` -- assemble
for aarch64 with **zero failures**. They contain only data directives
(`.byte`, `.word`, `.short`, `.align`, `.global`), no ARM instructions.

So there is no "rewrite the assembly for a new architecture" problem. There is
exactly one blocker.

## The one blocker

The data contains **48,316 four-byte pointer slots**, which assemble to
`R_AARCH64_ABS32` relocations. An absolute 32-bit relocation against a symbol
cannot exist in a shared object, and every Android app is a PIE/shared library:

```
relocation R_AARCH64_ABS32 against `PetalburgCity_EventScript_WallysMom'
can not be used when making a shared object
```

Minimal repro confirms both halves of this: `.word symbol` fails to link into a
`-shared` object, and `.xword symbol` links fine, becoming a relative dynamic
relocation.

## Where the 48,316 slots are

They split almost evenly into two classes that need different fixes.

| Source | Slots | Class |
| --- | ---: | --- |
| `data/event_scripts.s` | 17,438 | bytecode |
| `data/battle_anim_scripts.s` | 4,231 | bytecode |
| `data/battle_scripts_1.s` | 1,508 | bytecode |
| `data/battle_ai_scripts.s` | 1,222 | bytecode |
| `data/contest_ai_scripts.s` | 364 | bytecode |
| **bytecode subtotal** | **24,763** | |
| `sound/songs/**` (530 files) | 10,992 | struct |
| `data/maps.s` | 4,439 | struct |
| `data/map_events.s` | 4,054 | struct |
| `data/sound_data.s` | 3,774 | struct |
| **struct subtotal** | **23,259** | |

## Plan

### Struct data (23,259 slots) -- widen to 64-bit

These slots are fields of C structs. On arm64 the compiler already lays those
fields out as 8 bytes, so the assembly must match: `.4byte sym` becomes
`.xword sym`, with `.balign 8` where the struct's alignment now demands it.
The C side needs no changes; the risk is purely in getting padding to match
the arm64 C ABI.

The 10,992 song slots are the cheapest win by far, because they are emitted by
`tools/mid2agb`, so they are fixed in one generator rather than 530 files.
The map slots likewise come from `tools/mapjson`.

### Bytecode (24,763 slots) -- keep 4 bytes, store offsets

Widening here would be much worse: these pointers sit inside bytecode streams
that C interpreters walk byte by byte, so every argument offset in every script
command would shift.

Instead keep them four bytes and store a **link-time-constant offset** rather
than an address:

```
.4byte SomeScript - __script_base
```

A difference between two symbols in the same output section is resolved at link
time to a constant, so it produces **no dynamic relocation at all** and is legal
in a shared object. Sizes and every existing offset stay exactly as they are.

The cost is on the C side: each interpreter must add `__script_base` where it
currently dereferences a raw script pointer -- `script.c`'s `ScriptReadWord`
and the battle script, battle anim and AI command readers.

## Status

Analysis complete and verified. Implementation not started.

The honest caveat: this has to be validated by running the game, and this
environment has no device or emulator (`dl.google.com` is blocked by the
network policy, so no emulator images). CI can prove it compiles, links and
packages; it cannot prove the data is laid out correctly. Wrong padding
produces a build that installs and then misbehaves in ways only play-testing
finds.
