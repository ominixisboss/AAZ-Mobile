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

## Skills — `SkillId` / `SkillCatalog`

All 21 skills with their printed names and XP costs. Learning a skill and promoting
it to Expert cost the same. Almost every skill is +1 at Basic and +2 at Expert, so
rank doubles as the modifier.

| Skill | XP | Effect |
| --- | --- | --- |
| Agility | 8 | +1 Defense and Agility rolls |
| Campfire Musician | 5 | With an instrument, d6 at each Safe House; 1 (Expert 1–2) grants a Clue |
| Charisma | 3 | +1 Charisma when persuading |
| Danger Sense | 4 | Reduces a foe's Surprise chance by 1 (Expert 2) |
| Deadeye | 8 | +1 ranged Attack, stacking with weapon modifiers |
| Fighter | 6 | +1 hand-to-hand Attack |
| Fixer | 3 | Spend Tools to clear Rusted; Expert keeps them on 1–2 |
| Intelligence | 5 | +1 Intelligence |
| Medic | 3 | +d3 (Expert d6) points to every Medkit found |
| Mutant Hunter | 4 | +1 Attack and Defense vs mutants — **learnable only from hunters met in play** |
| Purifier | 3 | +1 Attack vs Zombified |
| Quick Draw | 5 | Draw a melee weapon (Expert: a pistol) without losing a turn |
| Resilient | 5 | +1 Saves vs Radiation and Acid |
| Resistant to Hunger | 2 | Eat every 6 sectors (Expert 7) |
| Scavenger | 5 | Take the Junk/Supplies entry one above (Expert: above or below) |
| Slippery | 3 | +1 Escape |
| Stealth | 4 | +1 Stealth |
| Strength | 5 | +1 Strength and 3 more carry slots (Expert +2 and 5 more) |
| Tough | 8 | +1 current and maximum Life and Rad Resistance (Expert another +1 each) |
| Tracker | 6 | +1 Tracker, which you may decline to apply |
| Will | 6 | +1 Saves vs Psionics |

XP is earned at 1 per standard combat, 2 for especially powerful foes, plus awards
per Echo. Escaping or avoiding combat by Stealth earns nothing — the game pays you
only for fights you actually take.

## Exploration Table (2d6) — `Expedition.ExplorationOutcome`

| 2d6 | Result | Odds |
| --- | --- | --- |
| 2–3 | Rusty Marshes | 8.3% |
| 4 | Dark Hollows | 8.3% |
| 5 | Railway Remnants | 11.1% |
| 6 | Industrial Ruins | 13.9% |
| 7 | Broken Roads | 16.7% |
| 8 | Lost Villages | 13.9% |
| 9 | Military Installations | 11.1% |
| 10 | Corrupted Woods | 8.3% |
| 11 | Classified Facilities | 5.6% |
| 12+ | Zone Surge → Event 6 | 2.8% |

The bell curve makes Broken Roads twice as common as Classified Facilities, so the
rare terrain tables will be seen far less often — worth knowing when budgeting art
per location archetype.

## Scavenging (d6) — `Expedition.Scavenge`

d6, plus Tracker if applied, minus one per previous attempt in the sector.

| Total | Result |
| --- | --- |
| 1 | Nothing. Save vs L4 Radiation or lose 1 Rad Resistance |
| 2 | Nothing found |
| 3 | Roll on the Encounter Table |
| 4 | Roll on the Junk Table |
| 5 | Roll on the Supplies Table |
| 6 | Gain 1 Clue |

A sector cannot be scavenged if it holds a Safe House, is an Echo sector, still has
unresolved threats, or is one you fled from.

Measured over 100k trials per attempt, a sector dries up fast: useful loot falls
50% → 33% → 17% → 0% across the first four attempts, while the chance of pulling an
Encounter instead stays flat at ~17% until the roll can no longer reach 3.

## Injuries (d6) — `Injury`

Taken voluntarily to cancel the Life loss from a single attack. Permanent, duplicates
rerolled, capped at three — after which nothing stands between the operative and death.

| d6 | Injury | Effect |
| --- | --- | --- |
| 1 | Facial Scar | −1 Charisma |
| 2 | Impaired Arm | −1 melee Attack |
| 3 | Eye Damage | −1 ranged Attack |
| 4 | Limp | −1 all Escape |
| 5 | Cognitive Trauma | −1 Will and Saves vs Psionics |
| 6 | Crushed Leg | Food one sector earlier, −1 all Agility |

## Marks of the Zone (d6) — `ZoneMark`

The radiation counterpart: cancels a Rad Resistance loss during an event or encounter.
Same permanence and same cap of three.

| d6 | Mark | Effect |
| --- | --- | --- |
| 1 | Paranoia Drift | Only one rest and recovery per Safe House visit |
| 2 | Hunger Surge | Food one sector earlier |
| 3 | Disorientation | An action needing a specific non-weapon item: d6, on a 1 it is gone |
| 4 | Stone Skin | +1 Saves vs Radiation, but recover 1 less Rad Resistance |
| 5 | Death Wish | May not Escape unless Life or Rad Resistance is 2 or less |
| 6 | Rad Burn | Maximum Rad Resistance permanently −1 |

## Carry, death and recovery

- **Carry Limit 10.** Heavy items cost 2 slots, Light items, money and ammo cost
  nothing, and every point of Food costs 1. Worn gear, artifacts in suit slots and the
  single item in your hands count as Light. Strength adds 3 slots (Expert 5).
- **Death** is either track reaching zero. Items in Safe Houses are lost; returning to
  the death sector recovers one item of choice; a Hidden Cache survives on a d6 of 4–6.
- **Recovery**: completing an Echo and returning to a Safe House — or ending a session
  there — restores both tracks to maximum.
- **Surge Warnings** accumulate from events; at 12+ a Zone Surge arrives (Event 6).

## Echoes

Roll d6 for a first Echo at creation. One Echo active at a time, plus one Side
Mission. Each completed Echo grants an Echo Fragment; **three Fragments unlock the
Final Mission** (Event 14), though the player may choose to gather more first.
Objectives for Echoes 1–5 are transcribed; Echo 6 (Sky Walk) is not.

Echo 5's Devil's Dance also settles the map orientation independently: its direction
die reads **1 N, 2 NE, 3 SE, 4 S, 5 SW, 6 NW**, which is exactly the flat-top
neighbour set. A pointy-top grid would give E/NE/NW/W/SW/SE.

## Assumptions that need checking against the book

1. **The natural-1 / natural-6 rule applies to Defense and Escape rolls.** It is stated
   under *Skill and Save Rolls*, whose list ends "and others depending on the
   situation", but the Defense and Escape sections do not restate it. The engine
   applies it uniformly via `Resolver.Check`.
2. **Scavenging totals are clamped to 1–6.** Modifiers can push a result outside the
   table in both directions and the book does not say what then happens.
3. **Crushed Leg and Hunger Surge stack.** Both read "Food is consumed one sector
   earlier"; the book does not say whether an operative carrying both eats two sectors
   sooner. The engine stacks them, flooring the interval at 1.

Resolved since the last pass: base Carry Limit is confirmed at 10, and all 21 skill
names and XP costs are now transcribed rather than inferred. Halyna's kit comes to
exactly 10/10 slots under the transcribed rules, which corroborates both.

## Not yet supplied

The second transcription cuts off inside Echo 6. What remains missing is content
rather than mechanics — the engine does not need it to compile, but nothing is
playable without it:

- **Echo 6: Sky Walk** objectives (cut off mid-sentence)
- Side Missions Table; Crossing the Perimeter
- **All nine terrain tables** — Rusty Marshes, Dark Hollows, Railway Remnants,
  Industrial Ruins, Broken Roads, Lost Villages, Military Installations,
  Corrupted Woods, Classified Facilities
- **Locations & Characters** — Grainfield Camp, First Light Camp, Hunter's Shack,
  Mobile Science Station, Line 9 Train, Stalker Camp, and their NPCs
- **Encounter tables** — Stalkers, Military Forces, Outlaws, Mutated Creatures,
  Lost Ones, Anomalies
- **Item tables** — Artifacts, Junk, Supplies, Weapons, Stalker's Stash,
  Military Crate, Scientific Storage Unit
- **Events 1–60** (pp. 120–138), including Event 6 (Zone Surge), Event 14 (Final
  Mission) and the Echo trigger events
- Weapons and Protective Suits summary tables (stats, Fire Rate, Rusted status)
- Mapping the Zone, Topographic Symbols, Fading Trails detail (p. 147)

Of these, the **Weapons and Protective Suits summary tables** are the highest value
next: they carry the attack and defense modifiers the combat engine already accepts
but currently has nothing to feed it.
