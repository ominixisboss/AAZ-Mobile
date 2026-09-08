using AAZ.Core;

namespace AAZ.Rules
{
    /// <summary>Outcome of a d6 check, keeping the natural die alongside the modified total.</summary>
    public readonly struct CheckResult
    {
        /// <summary>The raw die, before modifiers. The 1-always-fails / 6-always-succeeds
        /// rules key off this, not off the total.</summary>
        public readonly int Natural;
        public readonly int Total;
        public readonly int Difficulty;
        public readonly bool Success;

        public CheckResult(int natural, int total, int difficulty, bool success)
        {
            Natural = natural;
            Total = total;
            Difficulty = difficulty;
            Success = success;
        }

        public bool CriticalSuccess => Natural == 6;
        public bool CriticalFailure => Natural == 1;

        public override string ToString()
            => $"d6={Natural}{(Total != Natural ? $" ({Total:+0;-0;0} total)" : "")} vs L{Difficulty} -> {(Success ? "success" : "failure")}";
    }

    /// <summary>
    /// The universal d6 check: roll, add modifiers, meet or beat the Difficulty Level.
    /// Used for Skill rolls, Saves, Stealth, Charisma, Strength and the rest.
    /// </summary>
    public static class Resolver
    {
        /// <summary>
        /// Rolls against a Difficulty Level. A natural 6 always succeeds and a natural 1
        /// always fails, whatever the modifiers - so a hard enough task is never automatic
        /// and an easy enough one is never safe.
        /// </summary>
        public static CheckResult Check(Rng rng, int difficulty, int modifier = 0)
        {
            int natural = rng.Die(6);
            int total = natural + modifier;

            bool success;
            if (natural == 6)
                success = true;
            else if (natural == 1)
                success = false;
            else
                success = total >= difficulty;

            return new CheckResult(natural, total, difficulty, success);
        }

        /// <summary>An X-in-6 chance, as the rules write it ("a 3-in-6 chance").</summary>
        public static bool ChanceInSix(Rng rng, int chance)
        {
            if (chance <= 0)
                return false;
            if (chance >= 6)
                return true;

            return rng.Die(6) <= chance;
        }
    }
}
