using System;
using System.Collections.Generic;
using UnityEngine;

namespace AAZ.Core
{
    /// <summary>Dice a printed table can be indexed by.</summary>
    public enum DieKind
    {
        D6,
        TwoD6,
        ThreeD6,
        D66,
        D8,
        D10,
        D12,
        D20,
        D100,
    }

    public static class DieKindExtensions
    {
        /// <summary>Every result the die can produce, ascending. Used for coverage checks.</summary>
        public static IEnumerable<int> Faces(this DieKind kind)
        {
            switch (kind)
            {
                case DieKind.D6: return Sequence(1, 6);
                case DieKind.TwoD6: return Sequence(2, 12);
                case DieKind.ThreeD6: return Sequence(3, 18);
                case DieKind.D8: return Sequence(1, 8);
                case DieKind.D10: return Sequence(1, 10);
                case DieKind.D12: return Sequence(1, 12);
                case DieKind.D20: return Sequence(1, 20);
                case DieKind.D100: return Sequence(1, 100);
                case DieKind.D66: return D66Faces();
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        public static int Roll(this DieKind kind, Rng rng)
        {
            switch (kind)
            {
                case DieKind.D6: return rng.Die(6);
                case DieKind.TwoD6: return rng.Roll(2, 6);
                case DieKind.ThreeD6: return rng.Roll(3, 6);
                case DieKind.D66: return rng.D66();
                case DieKind.D8: return rng.Die(8);
                case DieKind.D10: return rng.Die(10);
                case DieKind.D12: return rng.Die(12);
                case DieKind.D20: return rng.Die(20);
                case DieKind.D100: return rng.Die(100);
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        static IEnumerable<int> Sequence(int from, int to)
        {
            for (int i = from; i <= to; i++)
                yield return i;
        }

        // d66 skips 17-20, 27-30 and so on: the digits are two separate d6.
        static IEnumerable<int> D66Faces()
        {
            for (int tens = 1; tens <= 6; tens++)
                for (int units = 1; units <= 6; units++)
                    yield return tens * 10 + units;
        }
    }

    [Serializable]
    public sealed class TableEntry
    {
        [Tooltip("Lowest die result that selects this row, inclusive.")]
        public int min = 1;

        [Tooltip("Highest die result that selects this row, inclusive.")]
        public int max = 1;

        [Tooltip("Stable identifier the game logic branches on.")]
        public string key;

        [TextArea(1, 4)]
        [Tooltip("The text as printed on the table, shown to the player.")]
        public string text;

        [Tooltip("Optional asset this row resolves to - an encounter, a diorama scene, an item.")]
        public UnityEngine.Object payload;

        public bool Contains(int roll) => roll >= min && roll <= max;
    }

    /// <summary>
    /// A printed random table, authored once and rolled on at runtime.
    /// <para>
    /// Tables are the whole substance of a solo tabletop game, so they are assets rather than
    /// code: the rules can be transcribed, diffed and corrected without recompiling, and the
    /// validator below catches the transcription errors that are otherwise invisible until a
    /// roll silently returns nothing.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "AAZ/Random Table", fileName = "New Table")]
    public sealed class RandomTable : ScriptableObject
    {
        [Tooltip("Which die this table is indexed by. Note 2d6 is a bell curve and d66 is flat.")]
        public DieKind die = DieKind.TwoD6;

        [TextArea(1, 3)]
        public string description;

        public List<TableEntry> entries = new List<TableEntry>();

        /// <summary>Rolls and returns the matching row, or null if the table has a gap there.</summary>
        public TableEntry Roll(Rng rng, out int rolled)
        {
            rolled = die.Roll(rng);
            return Lookup(rolled);
        }

        public TableEntry Roll(Rng rng) => Roll(rng, out _);

        public TableEntry Lookup(int roll)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].Contains(roll))
                    return entries[i];
            }
            return null;
        }

        /// <summary>
        /// Reports transcription errors: results no row covers, results two rows both claim,
        /// and rows that reach outside what the die can actually roll.
        /// </summary>
        public List<string> Validate()
        {
            var problems = new List<string>();

            foreach (TableEntry entry in entries)
            {
                if (entry == null)
                {
                    problems.Add("An entry is null.");
                    continue;
                }

                if (entry.min > entry.max)
                    problems.Add($"Row '{entry.key}' has min {entry.min} above max {entry.max}.");
            }

            var uncovered = new List<int>();
            var doubled = new List<int>();

            foreach (int face in die.Faces())
            {
                int hits = 0;
                foreach (TableEntry entry in entries)
                {
                    if (entry != null && entry.Contains(face))
                        hits++;
                }

                if (hits == 0) uncovered.Add(face);
                else if (hits > 1) doubled.Add(face);
            }

            if (uncovered.Count > 0)
                problems.Add($"No row covers: {string.Join(", ", uncovered)}.");

            if (doubled.Count > 0)
                problems.Add($"More than one row covers: {string.Join(", ", doubled)}.");

            var faces = new HashSet<int>(die.Faces());
            foreach (TableEntry entry in entries)
            {
                if (entry == null || entry.min > entry.max)
                    continue;

                for (int v = entry.min; v <= entry.max; v++)
                {
                    if (faces.Contains(v))
                        continue;

                    problems.Add($"Row '{entry.key}' covers {v}, which {die} cannot roll.");
                    break;
                }
            }

            return problems;
        }
    }
}
