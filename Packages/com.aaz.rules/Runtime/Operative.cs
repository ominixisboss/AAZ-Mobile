using System;
using System.Collections.Generic;
using UnityEngine;

namespace AAZ.Rules
{
    public enum Vitality
    {
        Life,
        RadResistance,
    }

    /// <summary>
    /// A lone operative. Both tracks start at 8 and the Tough skill raises each maximum,
    /// so maxima are derived rather than stored - a skill learned mid-run then lifts the
    /// ceiling immediately, as the rules require.
    /// </summary>
    [Serializable]
    public sealed class Operative
    {
        /// <summary>Starting Life and Rad Resistance before upgrades or skills.</summary>
        public const int StartingVitality = 8;

        /// <summary>
        /// Items carryable with no Strength skill.
        /// <para>
        /// DEDUCED, not transcribed: the Carry Limit rules fall in a part of the book we do
        /// not have. Kolya and Halyna have no Strength and list Carry Limit 10; Vasin has
        /// Strength (+3) and lists 13. Confirm against the book before trusting it.
        /// </para>
        /// </summary>
        public const int BaseCarryLimit = 10;

        public string operativeName = "Operative";
        public int xp;

        [SerializeField] int m_BaseMaxLife = StartingVitality;
        [SerializeField] int m_BaseMaxRad = StartingVitality;

        public int life = StartingVitality;
        public int radResistance = StartingVitality;

        public int money;
        public int food;
        public int clues;

        public SkillSet skills = new SkillSet();
        public List<string> items = new List<string>();

        public int MaxLife => m_BaseMaxLife + skills.Bonus(SkillId.Tough);
        public int MaxRadResistance => m_BaseMaxRad + skills.Bonus(SkillId.Tough);

        public bool IsAlive => life > 0 && radResistance > 0;

        /// <summary>Strength grants +3 slots at Basic and +5 at Expert - not the usual +1/+2.</summary>
        public int CarryLimit
        {
            get
            {
                switch (skills.Rank(SkillId.Strength))
                {
                    case SkillRank.Basic: return BaseCarryLimit + 3;
                    case SkillRank.Expert: return BaseCarryLimit + 5;
                    default: return BaseCarryLimit;
                }
            }
        }

        /// <summary>A creation-step upgrade spent on +1 Life.</summary>
        public void UpgradeLife()
        {
            m_BaseMaxLife++;
            life++;
        }

        /// <summary>A creation-step upgrade spent on +1 Rad Resistance.</summary>
        public void UpgradeRadResistance()
        {
            m_BaseMaxRad++;
            radResistance++;
        }

        /// <summary>
        /// Learns a skill, or promotes it to Expert. Tough is special-cased: it raises the
        /// maxima, and the rules say current values rise with them at once.
        /// </summary>
        public void LearnSkill(SkillId skill)
        {
            SkillRank before = skills.Rank(skill);
            skills.Learn(skill);
            SkillRank after = skills.Rank(skill);

            if (skill != SkillId.Tough || after == before)
                return;

            int gained = (int)after - (int)before;
            life += gained;
            radResistance += gained;
        }

        public void Damage(int amount, Vitality track = Vitality.Life)
        {
            if (amount <= 0)
                return;

            if (track == Vitality.Life)
                life = Mathf.Max(0, life - amount);
            else
                radResistance = Mathf.Max(0, radResistance - amount);
        }

        public void Heal(int amount, Vitality track = Vitality.Life)
        {
            if (amount <= 0)
                return;

            if (track == Vitality.Life)
                life = Mathf.Min(MaxLife, life + amount);
            else
                radResistance = Mathf.Min(MaxRadResistance, radResistance + amount);
        }
    }
}
