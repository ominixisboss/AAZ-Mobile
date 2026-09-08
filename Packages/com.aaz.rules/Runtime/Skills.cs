using System;
using System.Collections.Generic;
using UnityEngine;

namespace AAZ.Rules
{
    /// <summary>
    /// The 21 skills on the Skills Table.
    /// <para>
    /// NAMING CAVEAT: the printed table renders each skill's name as artwork, and those names
    /// did not survive into the text we transcribed from. Nine names are confirmed by the
    /// pre-made operatives (Kolya, Halyna, Vasin) or by rules text that cites them - those are
    /// marked CONFIRMED. The rest are labels inferred from the effect and must be checked
    /// against the book. The *effects* below are transcribed verbatim and are not in doubt.
    /// </para>
    /// </summary>
    public enum SkillId
    {
        None = 0,

        /// <summary>+1 to Defense and Agility rolls. INFERRED NAME.</summary>
        Agility,
        /// <summary>With an instrument, d6 at each Safe House; 1 (Expert 1-2) grants a Clue. INFERRED NAME.</summary>
        Musician,
        /// <summary>+1 to Charisma rolls when persuading. INFERRED NAME.</summary>
        Charisma,
        /// <summary>Reduces enemy Surprise chance by 1 (Expert 2). INFERRED NAME.</summary>
        Alertness,
        /// <summary>+1 to Attack rolls with ranged weapons. INFERRED NAME.</summary>
        Marksman,
        /// <summary>+1 to Attack rolls with hand-to-hand weapons. INFERRED NAME.</summary>
        Brawler,
        /// <summary>Spend Tools to clear Rusted; Expert may keep the Tools on 1-2. CONFIRMED (Vasin).</summary>
        Fixer,
        /// <summary>+1 to Intelligence rolls. CONFIRMED (Halyna).</summary>
        Intelligence,
        /// <summary>Adds d3 (Expert d6) points to every Medkit found. CONFIRMED (Halyna).</summary>
        Medic,
        /// <summary>+1 to Attack and Defense against mutants. Learnable only from hunters met in play. INFERRED NAME.</summary>
        Hunter,
        /// <summary>+1 to Attack rolls against Zombified foes. INFERRED NAME.</summary>
        ZombieSlayer,
        /// <summary>Draw a melee weapon (Expert: a pistol) without losing a turn. INFERRED NAME.</summary>
        QuickDraw,
        /// <summary>+1 to Saves against Radiation and Acid. CONFIRMED (Kolya).</summary>
        Resilient,
        /// <summary>Eat every 6 sectors (Expert 7) instead of 5. INFERRED NAME.</summary>
        Survivalist,
        /// <summary>On Junk/Supplies rolls, take the entry one above (Expert: above or below). CONFIRMED (Kolya).</summary>
        Scavenger,
        /// <summary>+1 to Stealth rolls. CONFIRMED (Kolya).</summary>
        Stealth,
        /// <summary>+1 to Strength rolls and carry 3 more items (Expert +2 and 5 more). CONFIRMED (Vasin).</summary>
        Strength,
        /// <summary>+1 to current and maximum Life and Rad Resistance (Expert another +1 each). CONFIRMED (Vasin).</summary>
        Tough,
        /// <summary>+1 to Tracker rolls, which you may decline to apply. CONFIRMED (cited by the Scavenging rules).</summary>
        Tracker,
        /// <summary>+1 to Saves vs Psionics. CONFIRMED (Halyna).</summary>
        Will,
        /// <summary>+1 to Escape rolls. INFERRED NAME.</summary>
        Runner,
    }

    public enum SkillRank
    {
        None = 0,
        Basic = 1,
        Expert = 2,
    }

    /// <summary>
    /// An operative's skills. Almost every skill grants +1 at Basic and +2 at Expert, so the
    /// rank doubles as the modifier and most lookups are just <see cref="Bonus"/>. The handful
    /// that break that pattern (Strength's carry bonus, Survivalist's food interval) are
    /// handled where they apply rather than being forced into this shape.
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

        /// <summary>Learns a skill at Basic, or promotes an existing one to Expert.</summary>
        public void Learn(SkillId skill)
        {
            SkillRank current = Rank(skill);
            Set(skill, current == SkillRank.None ? SkillRank.Basic : SkillRank.Expert);
        }
    }
}
