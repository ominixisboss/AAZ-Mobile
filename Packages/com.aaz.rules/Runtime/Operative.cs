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
    /// How much of the carry limit an item costs. Worn equipment, artifacts in suit slots and
    /// the single item in your hands are Light, which is why a wielded rifle is free but a
    /// spare one is not.
    /// </summary>
    public enum ItemWeight
    {
        Light = 0,
        Normal = 1,
        Heavy = 2,
    }

    [Serializable]
    public struct Item
    {
        public string name;
        public ItemWeight weight;

        public Item(string name, ItemWeight weight = ItemWeight.Normal)
        {
            this.name = name;
            this.weight = weight;
        }

        public int Slots => (int)weight;
    }

    /// <summary>
    /// A lone operative. Maxima are derived rather than stored, so a skill or a Mark that
    /// shifts a ceiling takes effect the moment it is acquired, as the rules require.
    /// </summary>
    [Serializable]
    public sealed class Operative
    {
        /// <summary>Starting Life and Rad Resistance, before upgrades or skills.</summary>
        public const int StartingVitality = 8;

        /// <summary>Items carryable with no Strength skill and no backpack.</summary>
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
        public List<Item> items = new List<Item>();
        public List<Injury> injuries = new List<Injury>();
        public List<ZoneMark> marks = new List<ZoneMark>();

        [Tooltip("Extra slots from a backpack or a suit that raises capacity.")]
        public int carryBonus;

        [Tooltip("Defense modifier from the worn protective suit, if any.")]
        public int suitDefenseBonus;

        [Tooltip("Whether a protective suit is worn at all. Without one, Defense takes -1.")]
        public bool wearingSuit;

        // ---- vitality -------------------------------------------------------

        public int MaxLife => m_BaseMaxLife + skills.Bonus(SkillId.Tough);

        /// <summary>Rad Burn permanently lowers this ceiling by one.</summary>
        public int MaxRadResistance
            => m_BaseMaxRad + skills.Bonus(SkillId.Tough) - (HasMark(ZoneMark.RadBurn) ? 1 : 0);

        /// <summary>Either track reaching zero is death.</summary>
        public bool IsAlive => life > 0 && radResistance > 0;

        // ---- carrying -------------------------------------------------------

        /// <summary>Strength grants +3 slots at Basic and +5 at Expert, not the usual +1/+2.</summary>
        public int CarryLimit
        {
            get
            {
                int strength;
                switch (skills.Rank(SkillId.Strength))
                {
                    case SkillRank.Basic: strength = 3; break;
                    case SkillRank.Expert: strength = 5; break;
                    default: strength = 0; break;
                }
                return BaseCarryLimit + strength + carryBonus;
            }
        }

        /// <summary>Slots in use. Every point of Food costs one; Light items cost nothing.</summary>
        public int CarriedSlots
        {
            get
            {
                int total = Mathf.Max(0, food);
                for (int i = 0; i < items.Count; i++)
                    total += items[i].Slots;
                return total;
            }
        }

        public bool IsOverloaded => CarriedSlots > CarryLimit;

        // ---- afflictions ----------------------------------------------------

        public bool HasInjury(Injury injury) => injuries.Contains(injury);
        public bool HasMark(ZoneMark mark) => marks.Contains(mark);

        public bool CanTakeInjury => injuries.Count < Afflictions.MaxPerTrack;
        public bool CanTakeMark => marks.Count < Afflictions.MaxPerTrack;

        // ---- derived modifiers ---------------------------------------------

        public int MeleeAttackBonus
            => skills.Bonus(SkillId.Fighter) - (HasInjury(Injury.ImpairedArm) ? 1 : 0);

        public int RangedAttackBonus
            => skills.Bonus(SkillId.Deadeye) - (HasInjury(Injury.EyeDamage) ? 1 : 0);

        public int EscapeBonus
            => skills.Bonus(SkillId.Slippery) - (HasInjury(Injury.Limp) ? 1 : 0);

        public int CharismaBonus
            => skills.Bonus(SkillId.Charisma) - (HasInjury(Injury.FacialScar) ? 1 : 0);

        public int AgilityBonus
            => skills.Bonus(SkillId.Agility) - (HasInjury(Injury.CrushedLeg) ? 1 : 0);

        /// <summary>Agility applies to Defense too, as does the suit - or its absence.</summary>
        public int DefenseBonus
            => skills.Bonus(SkillId.Agility) + suitDefenseBonus + (wearingSuit ? 0 : -1);

        public int WillBonus
            => skills.Bonus(SkillId.Will) - (HasInjury(Injury.CognitiveTrauma) ? 1 : 0);

        /// <summary>Resilient covers Radiation and Acid; Stone Skin adds to Radiation only.</summary>
        public int RadiationSaveBonus
            => skills.Bonus(SkillId.Resilient) + (HasMark(ZoneMark.StoneSkin) ? 1 : 0);

        public int AcidSaveBonus => skills.Bonus(SkillId.Resilient);

        public int StealthBonus => skills.Bonus(SkillId.Stealth);
        public int TrackerBonus => skills.Bonus(SkillId.Tracker);
        public int IntelligenceBonus => skills.Bonus(SkillId.Intelligence);
        public int StrengthBonus => skills.Bonus(SkillId.Strength);

        /// <summary>Death Wish forbids fleeing until the operative is nearly dead.</summary>
        public bool CanAttemptEscape
            => !HasMark(ZoneMark.DeathWish) || life <= 2 || radResistance <= 2;

        /// <summary>Stone Skin costs a point of every Rad Resistance recovery.</summary>
        public int RadRecoveryPenalty => HasMark(ZoneMark.StoneSkin) ? 1 : 0;

        /// <summary>
        /// Sectors per ration. Resistant to Hunger stretches it; Crushed Leg and Hunger Surge
        /// each shorten it by one.
        /// <para>
        /// ASSUMPTION: Crushed Leg and Hunger Surge both read "Food is consumed one sector
        /// earlier" and are treated as stacking. The book does not say whether they should.
        /// </para>
        /// </summary>
        public int FoodInterval
        {
            get
            {
                int interval = Expedition.BaseFoodInterval
                             + skills.Bonus(SkillId.ResistantToHunger)
                             - (HasInjury(Injury.CrushedLeg) ? 1 : 0)
                             - (HasMark(ZoneMark.HungerSurge) ? 1 : 0);
                return Mathf.Max(1, interval);
            }
        }

        // ---- creation and advancement ---------------------------------------

        public void UpgradeLife()
        {
            m_BaseMaxLife++;
            life++;
        }

        public void UpgradeRadResistance()
        {
            m_BaseMaxRad++;
            radResistance++;
        }

        /// <summary>
        /// Learns a skill or promotes it to Expert. Tough is special-cased: it lifts both
        /// maxima, and the rules say current values rise with them immediately.
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

        /// <summary>
        /// Spends XP to learn or promote a skill. Learning and promoting cost the same.
        /// Mutant Hunter is refused: it can only be taught by hunters met in play.
        /// </summary>
        public bool TrySpendXpOn(SkillId skill, bool ignoreTaughtInPlayRestriction = false)
        {
            SkillCatalog.Info info = SkillCatalog.Get(skill);

            if (info.TaughtInPlayOnly && !ignoreTaughtInPlayRestriction)
                return false;

            if (!skills.CanAdvance(skill) || xp < info.Cost)
                return false;

            xp -= info.Cost;
            LearnSkill(skill);
            return true;
        }

        // ---- damage and recovery --------------------------------------------

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
            {
                life = Mathf.Min(MaxLife, life + amount);
                return;
            }

            radResistance = Mathf.Min(MaxRadResistance,
                                      radResistance + Mathf.Max(0, amount - RadRecoveryPenalty));
        }

        /// <summary>
        /// Full recovery on completing an Echo and returning to a Safe House, or on ending a
        /// session there.
        /// </summary>
        public void RestAtSafeHouse()
        {
            life = MaxLife;
            radResistance = MaxRadResistance;
        }
    }
}
