using AAZ.Core;

namespace AAZ.Rules
{
    public readonly struct AttackResult
    {
        public readonly int Natural;
        /// <summary>Sum of the attack die and every exploding re-roll.</summary>
        public readonly int DiceTotal;
        /// <summary>Dice plus skill and weapon modifiers.</summary>
        public readonly int Total;
        public readonly int Damage;
        public readonly bool Jammed;

        public AttackResult(int natural, int diceTotal, int total, int damage, bool jammed)
        {
            Natural = natural;
            DiceTotal = diceTotal;
            Total = total;
            Damage = damage;
            Jammed = jammed;
        }

        public bool Exploded => DiceTotal > Natural;
    }

    /// <summary>
    /// Combat resolution. Damage is the attack total divided by the foe's Level, rounding
    /// down, so a high-Level foe shrugs off ordinary hits entirely and only an exploding
    /// roll gets through - which is where the game's lethality comes from.
    /// </summary>
    public static class Combat
    {
        /// <summary>
        /// Resolves one attack. Rolling a natural 6 explodes: roll again and add, repeating
        /// for as long as sixes keep coming. A natural 1 with a Rusted firearm misses and
        /// jams the weapon.
        /// </summary>
        /// <param name="rustedFirearm">True only for a firearm marked Rusted - melee weapons
        /// and clean guns never jam.</param>
        public static AttackResult Attack(Rng rng, int foeLevel, int modifier = 0, bool rustedFirearm = false)
        {
            if (foeLevel < 1)
                foeLevel = 1;

            int natural = rng.Die(6);

            if (natural == 1 && rustedFirearm)
                return new AttackResult(natural, natural, natural + modifier, 0, jammed: true);

            int diceTotal = natural;
            int last = natural;
            while (last == 6)
            {
                last = rng.Die(6);
                diceTotal += last;
            }

            int total = diceTotal + modifier;
            int damage = total > 0 ? total / foeLevel : 0;

            return new AttackResult(natural, diceTotal, total, damage, jammed: false);
        }

        /// <summary>
        /// A burst from a weapon with Fire Rate above 1. The first shot is at full bonus and
        /// each extra shot takes a further -1, so spraying trades accuracy for volume.
        /// Damage is worked out separately per shot.
        /// </summary>
        public static AttackResult[] Burst(Rng rng, int foeLevel, int modifier, int shots, bool rustedFirearm = false)
        {
            if (shots < 1)
                shots = 1;

            var results = new AttackResult[shots];
            for (int i = 0; i < shots; i++)
                results[i] = Attack(rng, foeLevel, modifier - i, rustedFirearm);

            return results;
        }

        /// <summary>
        /// One Defense roll against one attacking foe: meet or beat its Level to avoid the
        /// hit. Without a protective suit every Defense roll takes -1, which is why going
        /// unarmoured is a slow death rather than a quick one.
        /// </summary>
        public static CheckResult Defend(Rng rng, int foeLevel, Operative operative = null)
            => Resolver.Check(rng, foeLevel, operative?.DefenseBonus ?? -1);

        /// <summary>
        /// One Escape roll covers the whole group; against mixed Levels use the highest.
        /// Failure means taking damage from every foe and trying again next turn.
        /// <para>
        /// Check <see cref="Operative.CanAttemptEscape"/> first: the Death Wish mark forbids
        /// fleeing until the operative is nearly dead.
        /// </para>
        /// </summary>
        public static CheckResult Escape(Rng rng, int highestFoeLevel, Operative operative = null)
            => Resolver.Check(rng, highestFoeLevel, operative?.EscapeBonus ?? 0);

        /// <summary>
        /// Attempted before combat begins. Success slips past the foe entirely, at the cost
        /// of not being able to scavenge the sector. Failure is worse than not trying: the
        /// foe acts first and fights at +1 Level.
        /// </summary>
        public static CheckResult Sneak(Rng rng, int foeLevel, Operative operative = null)
            => Resolver.Check(rng, foeLevel, operative?.StealthBonus ?? 0);

        /// <summary>Effective foe Level after a failed Stealth attempt.</summary>
        public static int LevelAfterFailedStealth(int foeLevel) => foeLevel + 1;

        /// <summary>
        /// Rolls the foe's listed Surprise chance. Danger Sense reduces it by its rank, and a
        /// chance reduced to zero or below cannot trigger at all.
        /// </summary>
        public static bool EnemySurprises(Rng rng, int surpriseInSix, Operative operative = null)
        {
            int dangerSense = operative?.skills.Bonus(SkillId.DangerSense) ?? 0;
            return Resolver.ChanceInSix(rng, surpriseInSix - dangerSense);
        }

        /// <summary>Attack modifier for the weapon in hand, before skill bonuses.</summary>
        public const int UnarmedPenalty = -1;
    }
}
