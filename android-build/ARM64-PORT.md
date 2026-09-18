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

## Progress

| Step | Slots | State |
| --- | ---: | --- |
| map data (`mapjson`, `asm/macros/map.inc`) | 8,493 | done |
| map script tables (`map_script` macros) | 832 | done |
| sound data (`m4a.inc`, `music_voice.inc`) | 3,774 | done |
| song headers (`tools/mid2agb`) | 2,333 | done |
| in-track song pointers (`music_player.c`) | 8,659 | done |
| field effect scripts (`field_effect.c`) | 170 | done |
| mystery event command table | 17 | done |
| script engine (`event_scripts`, `scrcmd.c`) | 17,438 | done |
| battle scripts (`battle_scripts_1/2`, `battle_script_commands.c`) | 1,562 | done |
| battle AI scripts (`battle_ai_script_commands.c`) | 1,218 | done |
| battle anim scripts (`battle_anim.c`) | 4,231 | done |
| contest AI scripts | 364 | blocked, see below |
| residue in `battle_ai_scripts` | 4 | remaining |
| **converted so far** | **47,948 / 48,316** | **99.2%** |

Each step is verified three ways: the arm64 object has zero remaining ABS32,
the arm32 object is byte-for-byte unchanged in relocation count, and the
resulting field offsets are compared against what the aarch64 C compiler
actually produces rather than hand-computed.

Watch for hardcoded structure sizes, not just pointer emissions. `voice_group`
indexed backwards by a literal `0xC`, which is `sizeof(struct ToneData)` on a
32-bit target; on arm64 the compiler reports 24. Measured stride after the fix
is 12 on arm32 and 24 on arm64, with the sample pointer at offset 4 and 8
respectively -- matching `offsetof` exactly.

## Deriving instruction layouts, rather than guessing them

The battle interpreters hide the pointer width in three separate places
besides the emission: `gBattlescriptCurrInstr += N` advances, `... + N` read
offsets into the instruction, and helpers such as `JumpIfMoveFailed(N, ...)`
that take an instruction length as an argument.

Classifying fields by parameter name does not work. `tryfaintmon` emits
`.int NULL` into a pointer field, and shares its opcode with
`tryfaintmon_spikes`, which names the same field `ptr` -- widening one and not
the other would have produced two different layouts for one opcode.

What does work is deriving the truth from the handlers: every
`ReadStreamPtr(gBattlescriptCurrInstr + N)` names a pointer offset for that
opcode. Cross-checking those against the layouts parsed out of
`battle_script.inc` confirmed 112 pointer fields, with the only two
non-matches being 1-byte instructions that read no operands at all. Every
advance, offset and helper length is then recomputed from that layout and
emitted in terms of `sizeof(void *)`, so both ABIs are right by construction.

The same cross-check validated the derivation before any of it was applied:
142 instruction lengths computed from the macros reproduced the hardcoded
advances exactly, with zero mismatches.

## Contest AI: deliberately not converted

Contest AI does not follow the convention the other interpreters use. Its
dispatcher consumes the opcode before calling the handler, so `gAIScriptPtr`
points at the first operand rather than the instruction -- and worse, handlers
delegate to helpers (`ContestAICmd_get_condition` and friends) that themselves
consume a varying number of operands before the handler reads anything.

That makes the instruction layout not statically derivable from the handler
the way it is everywhere else. The cross-check refused it: with the
instruction as the base, 88 of 100 pointer offsets failed to land on a 4-byte
field; shifting one past the opcode still left 45 failing.

Rather than apply a half-understood transformation to something that cannot
be tested here, contest AI is left alone. It is 364 slots, 0.75% of the
total. Converting it needs each helper's operand consumption modelled, or
simply hand-auditing 136 commands.

## Status

Implementation in progress; the 32-bit build stays green throughout.

The honest caveat: this has to be validated by running the game, and this
environment has no device or emulator (`dl.google.com` is blocked by the
network policy, so no emulator images). CI can prove it compiles, links and
packages; it cannot prove the data is laid out correctly. Wrong padding
produces a build that installs and then misbehaves in ways only play-testing
finds.
