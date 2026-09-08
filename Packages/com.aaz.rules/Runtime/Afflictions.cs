using System.Collections.Generic;
using AAZ.Core;

namespace AAZ.Rules
{
    /// <summary>
    /// Injuries Table (d6). Taken voluntarily to cancel the Life loss from a single attack -
    /// the Zone's bargain: survive now, be worse at everything forever. Permanent, capped at
    /// three, after which nothing stands between the operative and death.
    /// </summary>
    public enum Injury
    {
        None = 0,
        /// <summary>1 - Permanent -1 to Charisma rolls.</summary>
        FacialScar = 1,
        /// <summary>2 - Permanent -1 to melee Attack rolls.</summary>
        ImpairedArm = 2,
        /// <summary>3 - Permanent -1 to ranged Attack rolls.</summary>
        EyeDamage = 3,
        /// <summary>4 - Permanent -1 to all Escape rolls.</summary>
        Limp = 4,
        /// <summary>5 - Permanent -1 to Will rolls and Saves vs Psionics.</summary>
        CognitiveTrauma = 5,
        /// <summary>6 - Food consumed one sector earlier, and -1 to all Agility rolls.</summary>
        CrushedLeg = 6,
    }

    /// <summary>
    /// Marks of the Zone Table (d6). The radiation counterpart to Injuries: taken to cancel
    /// a Rad Resistance loss during an event or encounter. Also permanent and capped at three.
    /// </summary>
    public enum ZoneMark
    {
        None = 0,
        /// <summary>1 - Only one rest and recovery per Safe House visit.</summary>
        ParanoiaDrift = 1,
        /// <summary>2 - Food consumed one sector earlier than usual.</summary>
        HungerSurge = 2,
        /// <summary>3 - An action needing a specific non-weapon item: d6, on a 1 it is gone.</summary>
        Disorientation = 3,
        /// <summary>4 - +1 to Saves vs Radiation, but recover 1 less Rad Resistance.</summary>
        StoneSkin = 4,
        /// <summary>5 - May not Escape unless Life or Rad Resistance is 2 or less.</summary>
        DeathWish = 5,
        /// <summary>6 - Maximum Rad Resistance permanently reduced by 1.</summary>
        RadBurn = 6,
    }

    public static class Afflictions
    {
        /// <summary>An operative may carry at most three Injuries and three Marks.</summary>
        public const int MaxPerTrack = 3;

        /// <summary>
        /// Rolls a d6 for a new Injury, rerolling duplicates. Returns None if the operative
        /// already carries three, in which case the damage must simply be taken.
        /// </summary>
        public static Injury RollInjury(Rng rng, ICollection<Injury> existing)
        {
            if (existing.Count >= MaxPerTrack)
                return Injury.None;

            // Six faces and at most two already taken, so a distinct result always exists.
            Injury result;
            do
            {
                result = (Injury)rng.Die(6);
            }
            while (existing.Contains(result));

            return result;
        }

        /// <summary>Rolls a d6 for a new Mark of the Zone, rerolling duplicates.</summary>
        public static ZoneMark RollMark(Rng rng, ICollection<ZoneMark> existing)
        {
            if (existing.Count >= MaxPerTrack)
                return ZoneMark.None;

            ZoneMark result;
            do
            {
                result = (ZoneMark)rng.Die(6);
            }
            while (existing.Contains(result));

            return result;
        }

        public static string Describe(Injury injury)
        {
            switch (injury)
            {
                case Injury.FacialScar: return "Facial Scar";
                case Injury.ImpairedArm: return "Impaired Arm";
                case Injury.EyeDamage: return "Eye Damage";
                case Injury.Limp: return "Limp";
                case Injury.CognitiveTrauma: return "Cognitive Trauma";
                case Injury.CrushedLeg: return "Crushed Leg";
                default: return "None";
            }
        }

        public static string Describe(ZoneMark mark)
        {
            switch (mark)
            {
                case ZoneMark.ParanoiaDrift: return "Paranoia Drift";
                case ZoneMark.HungerSurge: return "Hunger Surge";
                case ZoneMark.Disorientation: return "Disorientation";
                case ZoneMark.StoneSkin: return "Stone Skin";
                case ZoneMark.DeathWish: return "Death Wish";
                case ZoneMark.RadBurn: return "Rad Burn";
                default: return "None";
            }
        }
    }
}
