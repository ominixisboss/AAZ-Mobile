using System;
using System.Collections.Generic;
using UnityEngine;

namespace AAZ.Rules
{
    /// <summary>The 21 skills on the Skills Table. Names and XP costs are transcribed.</summary>
    public enum SkillId
    {
        None = 0,

        /// <summary>+1 to Defense and Agility rolls.</summary>
        Agility,
        /// <summary>With an instrument, d6 at each Safe House; 1 (Expert 1-2) grants a Clue.</summary>
        CampfireMusician,
        /// <summary>+1 to Charisma rolls when persuading.</summary>
        Charisma,
        /// <summary>Reduces a foe's Surprise chance by 1 (Expert 2).</summary>
        DangerSense,
        /// <summary>+1 to Attack rolls with ranged weapons, stacking with weapon modifiers.</summary>
        Deadeye,
        /// <summary>+1 to Attack rolls with hand-to-hand weapons, on top of weapon bonuses.</summary>
        Fighter,
        /// <summary>Spend Tools to clear Rusted; Expert keeps the Tools on a 1-2.</summary>
        Fixer,
        /// <summary>+1 to Intelligence rolls.</summary>
        Intelligence,
        /// <summary>Adds d3 (Expert d6) points to every Medkit found.</summary>
        Medic,
        /// <summary>+1 to Attack and Defense against mutants. Learnable only from hunters met in play.</summary>
        MutantHunter,
        /// <summary>+1 to Attack rolls against Zombified foes.</summary>
        Purifier,
        /// <summary>Draw a melee weapon (Expert: a pistol) without losing a turn.</summary>
        QuickDraw,
        /// <summary>+1 to Saves against Radiation and Acid.</summary>
        Resilient,
        /// <summary>Eat every 6 sectors (Expert 7) instead of every 5.</summary>
        ResistantToHunger,
        /// <summary>On Junk/Supplies rolls, take the entry one above (Expert: above or below).</summary>
        Scavenger,
        /// <summary>+1 to Escape rolls.</summary>
        Slippery,
        /// <summary>+1 to Stealth rolls.</summary>
        Stealth,
        /// <summary>+1 to Strength rolls and carry 3 more items (Expert +2 and 5 more).</summary>
        Strength,
        /// <summary>+1 to current and maximum Life and Rad Resistance (Expert another +1 each).</summary>
        Tough,
        /// <summary>+1 to Tracker rolls, which you may decline to apply.</summary>
        Tracker,
        /// <summary>+1 to Saves vs Psionics.</summary>
        Will,
    }

    public enum SkillRank
    {
        None = 0,
        Basic = 1,
        Expert = 2,
    }

    /// <summary>Static data about each skill: its printed name and XP cost.</summary>
    public static class SkillCatalog
    {
        public readonly struct Info
        {
            public readonly string Name;
            public readonly int Cost;
            /// <summary>Mutant Hunter cannot be bought - it is taught by hunters met in play.</summary>
            public readonly bool TaughtInPlayOnly;

            public Info(string name, int cost, bool taughtInPlayOnly = false)
            {
                Name = name;
                Cost = cost;
                TaughtInPlayOnly = taughtInPlayOnly;
            }
        }

        static readonly Dictionary<SkillId, Info> s_Info = new Dictionary<SkillId, Info>
        {
            { SkillId.Agility,           new Info("Agility", 8) },
            { SkillId.CampfireMusician,  new Info("Campfire Musician", 5) },
            { SkillId.Charisma,          new Info("Charisma", 3) },
            { SkillId.DangerSense,       new Info("Danger Sense", 4) },
            { SkillId.Deadeye,           new Info("Deadeye", 8) },
            { SkillId.Fighter,           new Info("Fighter", 6) },
            { SkillId.Fixer,             new Info("Fixer", 3) },
            { SkillId.Intelligence,      new Info("Intelligence", 5) },
            { SkillId.Medic,             new Info("Medic", 3) },
            { SkillId.MutantHunter,      new Info("Mutant Hunter", 4, taughtInPlayOnly: true) },
            { SkillId.Purifier,          new Info("Purifier", 3) },
            { SkillId.QuickDraw,         new Info("Quick Draw", 5) },
            { SkillId.Resilient,         new Info("Resilient", 5) },
            { SkillId.ResistantToHunger, new Info("Resistant to Hunger", 2) },
            { SkillId.Scavenger,         new Info("Scavenger", 5) },
            { SkillId.Slippery,          new Info("Slippery", 3) },
            { SkillId.Stealth,           new Info("Stealth", 4) },
            { SkillId.Strength,          new Info("Strength", 5) },
            { SkillId.Tough,             new Info("Tough", 8) },
            { SkillId.Tracker,           new Info("Tracker", 6) },
            { SkillId.Will,              new Info("Will", 6) },
        };

        public static Info Get(SkillId skill)
            => s_Info.TryGetValue(skill, out Info info) ? info : new Info(skill.ToString(), 0);

        public static string Name(SkillId skill) => Get(skill).Name;

        /// <summary>Learning a skill and promoting it to Expert cost the same.</summary>
        public static int Cost(SkillId skill) => Get(skill).Cost;

        public static IEnumerable<SkillId> All => s_Info.Keys;
    }

    /// <summary>
    /// An operative's skills. Almost every skill grants +1 at Basic and +2 at Expert, so rank
    /// doubles as the modifier and most lookups are just <see cref="Bonus"/>. The few that
    /// break the pattern - Strength's carry slots, Resistant to Hunger's food interval - are
    /// handled where they apply rather than forced into this shape.
    /// </summary>
    [Serializable]
    public sealed class SkillSet
    {
        [Serializable]
        public struct Entry
        {
            public SkillId skill;
            public SkillRank rank;
        }

        [SerializeField] List<Entry> m_Entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => m_Entries;

        public SkillRank Rank(SkillId skill)
        {
            for (int i = 0; i < m_Entries.Count; i++)
            {
                if (m_Entries[i].skill == skill)
                    return m_Entries[i].rank;
            }
            return SkillRank.None;
        }

        public bool Has(SkillId skill) => Rank(skill) != SkillRank.None;

        /// <summary>The +1 / +2 modifier this skill contributes, or 0 if untrained.</summary>
        public int Bonus(SkillId skill) => (int)Rank(skill);

        public void Set(SkillId skill, SkillRank rank)
        {
            for (int i = 0; i < m_Entries.Count; i++)
            {
                if (m_Entries[i].skill != skill)
                    continue;

                if (rank == SkillRank.None)
                    m_Entries.RemoveAt(i);
                else
                    m_Entries[i] = new Entry { skill = skill, rank = rank };
                return;
            }

            if (rank != SkillRank.None)
                m_Entries.Add(new Entry { skill = skill, rank = rank });
        }

        /// <summary>Learns at Basic, or promotes an existing skill to Expert.</summary>
        public void Learn(SkillId skill)
        {
            SkillRank current = Rank(skill);
            Set(skill, current == SkillRank.None ? SkillRank.Basic : SkillRank.Expert);
        }

        public bool CanAdvance(SkillId skill) => Rank(skill) != SkillRank.Expert;
    }
}
