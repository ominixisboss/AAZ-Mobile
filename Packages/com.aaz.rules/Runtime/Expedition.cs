using System;
using AAZ.Core;
using UnityEngine;

namespace AAZ.Rules
{
    /// <summary>What entering a sector demands of the operative.</summary>
    public enum SectorEntry
    {
        /// <summary>New ground: number it and roll on the Exploration Table.</summary>
        Unexplored,
        /// <summary>Already mapped: no Exploration roll, but a 2-in-6 Random Encounter.</summary>
        Explored,
        /// <summary>Safe House: never rolls a Random Encounter.</summary>
        SafeHouse,
    }

    /// <summary>
    /// Upkeep that runs between encounters: hunger, re-entry checks and scavenging odds.
    /// </summary>
    [Serializable]
    public sealed class Expedition
    {
        /// <summary>Sectors per ration with no Survivalist skill.</summary>
        public const int BaseFoodInterval = 5;

        /// <summary>Chance of a Random Encounter when re-entering an explored sector.</summary>
        public const int ExploredEncounterChance = 2;

        /// <summary>Chance of a Random Encounter in the sector you escape into.</summary>
        public const int EscapeEncounterChance = 1;

        [Tooltip("Sectors travelled, or turns waited, since the last ration.")]
        public int sectorsSinceMeal;

        [Tooltip("Next unused Sector Number. The rules start the map at 1.")]
        public int nextSectorNumber = 1;

        /// <summary>Survivalist stretches a ration to 6 sectors at Basic and 7 at Expert.</summary>
        public static int FoodInterval(Operative operative)
            => BaseFoodInterval + (operative?.skills.Bonus(SkillId.Survivalist) ?? 0);

        public int TakeSectorNumber() => nextSectorNumber++;

        /// <summary>
        /// Whether re-entering this kind of sector triggers a Random Encounter. Safe Houses
        /// never do, which is what makes them worth walking back to.
        /// </summary>
        public static bool RollsRandomEncounter(Rng rng, SectorEntry entry)
        {
            switch (entry)
            {
                case SectorEntry.Explored: return Resolver.ChanceInSix(rng, ExploredEncounterChance);
                case SectorEntry.SafeHouse: return false;
                default: return false;
            }
        }

        /// <summary>The lighter 1-in-6 check for the sector an Escape roll drops you into.</summary>
        public static bool RollsEncounterAfterEscape(Rng rng, SectorEntry entry)
            => entry != SectorEntry.SafeHouse && Resolver.ChanceInSix(rng, EscapeEncounterChance);

        /// <summary>
        /// Advances the hunger clock by one sector travelled or one turn waited, and reports
        /// whether a ration is now due.
        /// </summary>
        public bool AdvanceClock(Operative operative)
        {
            sectorsSinceMeal++;
            return sectorsSinceMeal >= FoodInterval(operative);
        }

        /// <summary>
        /// Settles a due ration. Eats if there is food; otherwise the operative starves and
        /// loses a point from whichever track they choose. Either way the clock resets.
        /// </summary>
        public bool Eat(Operative operative, Vitality starveCost = Vitality.Life)
        {
            sectorsSinceMeal = 0;

            if (operative.food > 0)
            {
                operative.food--;
                return true;
            }

            operative.Damage(1, starveCost);
            return false;
        }

        /// <summary>
        /// The Scavenging roll for a sector: d6, plus Tracker if you choose to apply it,
        /// minus 1 for every previous attempt here. Picking a sector clean has diminishing
        /// returns by design.
        /// </summary>
        public static int ScavengeRoll(Rng rng, Operative operative, int previousAttempts, bool applyTracker = true)
        {
            int roll = rng.Die(6);
            int tracker = applyTracker ? operative.skills.Bonus(SkillId.Tracker) : 0;
            return roll + tracker - Mathf.Max(0, previousAttempts);
        }
    }
}
