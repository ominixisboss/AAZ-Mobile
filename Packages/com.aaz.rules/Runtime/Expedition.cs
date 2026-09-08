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

    /// <summary>Which terrain table an Exploration roll sends you to.</summary>
    public enum ExplorationResult
    {
        RustyMarshes,
        DarkHollows,
        RailwayRemnants,
        IndustrialRuins,
        BrokenRoads,
        LostVillages,
        MilitaryInstallations,
        CorruptedWoods,
        ClassifiedFacilities,
        /// <summary>12+ - a Zone Surge is approaching. Proceed to Event 6.</summary>
        ZoneSurge,
    }

    public enum ScavengingResult
    {
        /// <summary>1 - Nothing. Save vs L4 Radiation or lose 1 Rad Resistance.</summary>
        NothingAndRadiation,
        /// <summary>2 - Nothing found.</summary>
        Nothing,
        /// <summary>3 - Roll on the Encounter Table.</summary>
        Encounter,
        /// <summary>4 - Roll on the Junk Table.</summary>
        Junk,
        /// <summary>5 - Roll on the Supplies Table.</summary>
        Supplies,
        /// <summary>6 - Gain 1 Clue.</summary>
        Clue,
    }

    /// <summary>
    /// Upkeep between encounters: hunger, re-entry checks, exploration, scavenging and the
    /// slow accumulation of Surge Warnings.
    /// </summary>
    [Serializable]
    public sealed class Expedition
    {
        /// <summary>Sectors per ration with no modifiers.</summary>
        public const int BaseFoodInterval = 5;

        /// <summary>Chance of a Random Encounter when re-entering an explored sector.</summary>
        public const int ExploredEncounterChance = 2;

        /// <summary>Chance of a Random Encounter in the sector you escape into.</summary>
        public const int EscapeEncounterChance = 1;

        /// <summary>Surge Warnings needed before a Zone Surge arrives.</summary>
        public const int SurgeThreshold = 12;

        /// <summary>Difficulty of the Radiation save on a failed scavenge.</summary>
        public const int ScavengeRadiationLevel = 4;

        [Tooltip("Sectors travelled, or turns waited, since the last ration.")]
        public int sectorsSinceMeal;

        [Tooltip("Next unused Sector Number. The rules start the map at 1.")]
        public int nextSectorNumber = 1;

        [Tooltip("Accumulated Surge Warnings. At 12 or more, a Zone Surge arrives.")]
        public int surgeWarnings;

        public int TakeSectorNumber() => nextSectorNumber++;

        // ---- movement -------------------------------------------------------

        /// <summary>Safe Houses never roll encounters, which is what makes them worth returning to.</summary>
        public static bool RollsRandomEncounter(Rng rng, SectorEntry entry)
            => entry == SectorEntry.Explored && Resolver.ChanceInSix(rng, ExploredEncounterChance);

        /// <summary>The lighter 1-in-6 check for the sector an Escape roll drops you into.</summary>
        public static bool RollsEncounterAfterEscape(Rng rng, SectorEntry entry)
            => entry != SectorEntry.SafeHouse && Resolver.ChanceInSix(rng, EscapeEncounterChance);

        // ---- hunger ---------------------------------------------------------

        /// <summary>
        /// Advances the hunger clock by one sector travelled or one turn waited, and reports
        /// whether a ration is now due.
        /// </summary>
        public bool AdvanceClock(Operative operative)
        {
            sectorsSinceMeal++;
            return sectorsSinceMeal >= operative.FoodInterval;
        }

        /// <summary>
        /// Settles a due ration. Eats if there is food; otherwise the operative starves and
        /// loses a point from whichever track they choose. Either way the clock resets.
        /// Returns true if a ration was actually eaten.
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

        // ---- surge ----------------------------------------------------------

        /// <summary>Adds Surge Warnings and reports whether a Zone Surge is now due.</summary>
        public bool AddSurgeWarnings(int amount)
        {
            surgeWarnings = Mathf.Max(0, surgeWarnings + amount);
            return SurgeDue;
        }

        public bool SurgeDue => surgeWarnings >= SurgeThreshold;

        /// <summary>Call once a Surge has been resolved.</summary>
        public void ResetSurgeWarnings() => surgeWarnings = 0;

        // ---- exploration ----------------------------------------------------

        public static ExplorationResult Explore(Rng rng) => ExplorationOutcome(rng.Roll(2, 6));

        /// <summary>Maps a 2d6 total onto the Exploration Table.</summary>
        public static ExplorationResult ExplorationOutcome(int total)
        {
            if (total >= 12) return ExplorationResult.ZoneSurge;

            switch (total)
            {
                case 2:
                case 3: return ExplorationResult.RustyMarshes;
                case 4: return ExplorationResult.DarkHollows;
                case 5: return ExplorationResult.RailwayRemnants;
                case 6: return ExplorationResult.IndustrialRuins;
                case 7: return ExplorationResult.BrokenRoads;
                case 8: return ExplorationResult.LostVillages;
                case 9: return ExplorationResult.MilitaryInstallations;
                case 10: return ExplorationResult.CorruptedWoods;
                case 11: return ExplorationResult.ClassifiedFacilities;
                default: return ExplorationResult.RustyMarshes;
            }
        }

        // ---- scavenging -----------------------------------------------------

        /// <summary>
        /// A sector cannot be scavenged if it holds a Safe House, is an Echo sector, still has
        /// unresolved threats, or is one you fled from.
        /// </summary>
        public static bool CanScavenge(bool isSafeHouse, bool isEchoSector,
                                       bool unresolvedThreats, bool fledFromHere)
            => !isSafeHouse && !isEchoSector && !unresolvedThreats && !fledFromHere;

        /// <summary>
        /// The Scavenging roll: d6, plus Tracker if applied, minus one per previous attempt
        /// in this sector. Picking a sector clean has diminishing returns by design.
        /// </summary>
        public static int ScavengeTotal(Rng rng, Operative operative, int previousAttempts,
                                        bool applyTracker = true)
        {
            int tracker = applyTracker ? operative.TrackerBonus : 0;
            return rng.Die(6) + tracker - Mathf.Max(0, previousAttempts);
        }

        /// <summary>
        /// Maps a scavenging total onto the table.
        /// <para>
        /// ASSUMPTION: totals are clamped to 1-6. The table has six rows and the modifiers
        /// can push a result outside them in both directions; the book does not say what
        /// happens then, and clamping is the conventional reading.
        /// </para>
        /// </summary>
        public static ScavengingResult ScavengingOutcome(int total)
        {
            switch (Mathf.Clamp(total, 1, 6))
            {
                case 1: return ScavengingResult.NothingAndRadiation;
                case 2: return ScavengingResult.Nothing;
                case 3: return ScavengingResult.Encounter;
                case 4: return ScavengingResult.Junk;
                case 5: return ScavengingResult.Supplies;
                default: return ScavengingResult.Clue;
            }
        }

        public static ScavengingResult Scavenge(Rng rng, Operative operative, int previousAttempts,
                                                bool applyTracker = true)
            => ScavengingOutcome(ScavengeTotal(rng, operative, previousAttempts, applyTracker));
    }
}
