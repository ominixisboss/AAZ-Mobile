# Rules digest — implemented core

**Provenance.** Transcribed from the *Alone Against the Zone* rulebook (Revised Edition
v1.1, Alexey Aparin / Andrea Sfiligoi, Ganesha Games), text supplied by the project
owner for a personal, unreleased play-aid. This file records only what the engine
implements and the ambiguities found while implementing it — it is not a substitute
for the book, and the tables that carry the game's actual content are not reproduced.

## The loop

Move to an adjacent sector → Explore (if new) → resolve what is there → optionally
Scavenge → move on. Three Echoes completed unlocks the Final Mission: the Heart of
the Zone.

## Character creation — `Operative`

Start with 0 XP, **8 Life**, **8 Rad Resistance**, and 2 free Skills at Basic.
Equipment: d6 Food, one roll each on Weapons / Junk / Supplies, and d6 × 10 rubles.
Then 3 upgrades, freely mixed: +1 Life, +1 Rad Resistance, an extra Skill, or an
extra Junk / Supplies / Weapons roll. Finally, roll a first Echo.

## Resolution — `Resolver.Check`

Roll d6, add modifiers, meet or beat the Difficulty Level. **A natural 6 always
succeeds and a natural 1 always fails**, keyed off the raw die rather than the total.

Dice codes used: d3 (d6 halved up), d6, 2d6, d66 (tens die then units, 11–66, flat).

## Combat — `Combat`

| Rule | Implementation |
| --- | --- |
| Attack | d6 + bonuses, **divided by the foe's Level, rounding down** = Damage |
| Exploding Dice | natural 6 → roll again and add, repeating on further sixes |
| Weapon Jam | natural 1 with a **Rusted** firearm → automatic miss, weapon jams; 1 Action to clear |
| Fire Rate | extra shots at cumulative −1 (−1 second shot, −2 third), damage computed per shot |
| Unarmed | −1 to Attack |
| Defense | d6 per attacking foe, meet or beat its Level; **−1 with no protective suit** |
| Escape | one roll for the whole group, against the **highest** Level present |
| Stealth | success bypasses combat but forfeits Scavenging; failure gives the foe first strike **and +1 Level** |
| Surprise | the foe's listed X-in-6, reduced by Alertness rank, floored at zero |

### What the damage rule actually does

Divide-by-Level with exploding dice is where the game's lethality lives. Measured
over 60,000 attacks per cell:

**Mean damage per attack**

| Foe Level | +0 | +1 | +2 | +3 |
| --- | --- | --- | --- | --- |
| 1 | 4.20 | 5.20 | 6.22 | 7.19 |
| 2 | 1.80 | 2.40 | 2.81 | 3.39 |
| 3 | 1.00 | 1.40 | 1.80 | 2.01 |
| 4 | 0.68 | 0.91 | 1.11 | 1.49 |
| 5 | 0.44 | 0.64 | 0.84 | 1.04 |
| 6 | 0.20 | 0.40 | 0.59 | 0.80 |

**Chance an attack deals nothing at all**

| Foe Level | +0 | +1 | +2 | +3 |
| --- | --- | --- | --- | --- |
| 2 | 16.7% | — | — | — |
| 3 | 33.2% | 16.5% | — | — |
| 4 | 49.7% | 33.4% | 16.6% | — |
| 5 | 66.6% | 49.7% | 33.3% | 16.8% |
| 6 | 83.5% | 66.8% | 49.8% | 33.5% |

Every +1 buys back exactly one sixth of the whiff chance, which is why a single skill
rank is worth more here than in most systems, and why a Level 6 foe is a wall rather
than a tough fight.

## Movement and upkeep — `Expedition`

- Flat-top hex grid, entered **from the south**; sectors numbered from 1 as visited.
- Entering an **unexplored** sector: number it, roll Exploration.
- Re-entering an **explored** sector: no Exploration, but a **2-in-6** Random Encounter.
- **Safe Houses** never roll encounters, and allow rest, storage and waiting out a Surge.
  **Refuges** only let you wait out a Surge.
- Escaping into a sector: **1-in-6** Random Encounter unless it is a Safe House.
- **Food**: 1 per 5 sectors travelled or turns waited (6 with Survivalist, 7 at Expert).
  With no food, starve: lose 1 Life *or* 1 Rad Resistance, player's choice.
- **Scavenging**: d6, plus Tracker rank if you choose to apply it, minus 1 per previous
  attempt in that sector.

## Skills — `SkillId`

All 21 skill effects are transcribed exactly. **Their names are not.** The printed
table renders each name as artwork, which did not survive into the supplied text.

Nine are confirmed, either by a pre-made operative's listed skills or by rules text
citing them: **Fixer, Intelligence, Medic, Resilient, Scavenger, Stealth, Strength,
Tough, Tracker, Will**. The remaining twelve carry inferred labels — marked
`INFERRED NAME` in `Skills.cs` — and should be corrected against the book. The
effects behind them are not in doubt.

## Assumptions that need checking against the book

1. **Base Carry Limit = 10.** The Carry Limit section is in the part of the book not
   supplied. Deduced: Kolya and Halyna have no Strength and list 10; Vasin has
   Strength (+3) and lists 13. The pre-made operatives are consistent with this, and
   also with 8 base vitality plus the documented upgrade rules — but it is a deduction.
2. **The natural-1 / natural-6 rule applies to Defense and Escape rolls.** It is stated
   under *Skill and Save Rolls*, whose list ends "and others depending on the
   situation", but the Defense and Escape sections do not restate it. The engine
   applies it uniformly via `Resolver.Check`.
3. **Fire Rate penalties are cumulative** (−1, −2, −3). The text's parenthetical says
   exactly this, so this one is safe.

## Not yet supplied

The transcription cuts off mid-sentence in the Scavenging rules
("Subtract 1 for each previo…"). Everything after it is missing, which is most of the
game's content:

- Rest of Scavenging; Surge Warnings; Life & Rad Resistance; Recovery Between Missions
- Death, Preventing Death, Injuries, Marks of the Zone
- Equipment: Carry Limit, Stash, Money, Weapons, Ammo, Protective Suits
- XP and Advancement
- All six Echoes of the Zone; Side Missions; Crossing the Perimeter
- **All nine Exploration tables** — only entries 4, 6, 8, 10 and 12 of the master
  Exploration Table were visible; 2, 3, 5, 7, 9 and 11 are unknown
- Locations & Characters; all Encounter tables; all Item tables; Events
- The summary tables and the operative sheet

The engine is content-agnostic and does not need these to compile — they are data,
authored as `RandomTable` assets. But nothing playable exists without them.
