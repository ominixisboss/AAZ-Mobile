# Alone Against the Zone — what is actually known

> **Provenance warning.** Everything below comes from public product listings and
> reviews found via web search. It is marketing copy, not the rulebook. No
> attribute list, resolution mechanic, table or number in the real game has been
> seen. Nothing here is safe to implement as rules — it is only enough to make
> architectural decisions. The design doc still has to arrive before content work
> can start.

## The game

A solo survival tabletop RPG. You play a lone operative entering a shattered
exclusion zone: exploration, scarce resources, fast and unforgiving combat,
death everywhere.

| | |
| --- | --- |
| Written by | Alexey Aparin |
| Edited by | Andrea Sfiligoi |
| Publisher | Ganesha Games |
| Released | 2026 |
| System basis | Stand-alone, but built on the system from *Alone Against Fear* |
| Kit required | The book, paper and pencil, and a couple of ordinary d6 |
| Structure | Described by reviewers as a solo survival hex crawl |
| Setting | Explicitly inspired by the Strugatsky brothers' *Roadside Picnic*; pointedly **not** affiliated with S.T.A.L.K.E.R. or Metro 2033 |
| Content | Mutations in crumbling ruins, reality-bending anomalies, dangerous storms |

Ganesha Games is the *Four Against Darkness* house, which is a strong signal about
the shape of the rules even though the specifics are unseen: that family of games
is built on printed random tables, d6 and d66 lookups, pencil-and-paper
bookkeeping, and lazy content resolution — you roll for a location when you arrive
at it, not before.

## What this pins down

Enough to commit to three architectural decisions:

1. **Two d6 is the entire dice vocabulary.** So the die layer needs d6, 2d6 (a bell
   curve) and d66 (36 flat outcomes) and essentially nothing else.
2. **Tables are the substance of the game.** They belong in editable assets, not in
   code, so the rulebook can be transcribed and corrected without recompiling.
3. **It is a hex crawl with lazy resolution.** So the map is a hex grid with fog of
   war, and a cell's contents are rolled on entry and then remembered.

All three are implemented in `Packages/com.aaz.core` and none of them assume any
particular rule.

## What is still unknown, and blocks content work

- Character creation: what attributes exist, their ranges, how they are rolled.
- The core resolution mechanic: roll-under, roll-over, target numbers, modifiers.
- Combat: initiative, damage, death and recovery.
- Inventory: slot limits, encumbrance, weapon and consumable rules.
- Anomalies, artifacts, mutations and storms: how they are detected, survived, used.
- Zone structure: map size, region types, movement cost, how an expedition ends.
- Progression: whether an operative improves between runs.

## Sources

- [Alone against the Zone — Ganesha Games, DriveThruRPG](https://www.drivethrurpg.com/en/product/563135/alone-against-the-zone)
- [Alone Against the Zone — itch.io](https://ganesha-games.itch.io/alone-against-the-zone)
- [Alone against the Zone — BoardGameGeek](https://boardgamegeek.com/boardgame/468423/alone-against-the-zone)
- [Alone Against the Zone — Amazon listing](https://www.amazon.com/Alone-Against-Zone-survival-exclusion/dp/B0GVY31KB3)
- [Alone against the Zone — Solo RPG List](https://solorpglist.com/item.php?id=1100)
- [RPG PDF Spotlight — Voices From The Pulpit](https://angusabranson.com/2026/04/08/rpg-pdf-spotlight-alone-against-the-zone/)
- [Review and Overview — The Dungeon Dive](https://dungeondive.quest/t/alone-against-the-zone-review-and-overview/1695)
