using System;

namespace AAZ.Core
{
    /// <summary>
    /// Deterministic xorshift32 generator.
    /// <para>
    /// A solo tabletop run is only trustworthy if it replays: the same seed and the same
    /// choices must produce the same Zone, every time, on every device. <see cref="Random"/>
    /// does not guarantee that across platforms or Unity versions, so the algorithm is
    /// spelled out here and the whole generator state is one serialisable uint.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class Rng
    {
        // Any non-zero seed works; xorshift is a fixed point at zero, so substitute one.
        const uint k_DefaultSeed = 0x9E3779B9;

        uint m_State;

        public Rng(uint seed) => m_State = seed == 0u ? k_DefaultSeed : seed;

        public Rng() : this(unchecked((uint)Environment.TickCount)) { }

        /// <summary>Full generator state. Persist this to resume a run mid-session.</summary>
        public uint State
        {
            get => m_State;
            set => m_State = value == 0u ? k_DefaultSeed : value;
        }

        public uint NextUInt()
        {
            uint x = m_State;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            m_State = x;
            return x;
        }

        /// <summary>Uniform value in [0, n). Rejection-sampled, so there is no modulo bias.</summary>
        public uint Below(uint n)
        {
            if (n == 0u)
                throw new ArgumentOutOfRangeException(nameof(n), "Range must be positive.");

            uint threshold = (uint)((0x100000000UL - n) % n);
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold)
                    return r % n;
            }
        }

        /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));

            return minInclusive + (int)Below((uint)(maxExclusive - minInclusive));
        }

        /// <summary>One die of the given size, result in [1, sides].</summary>
        public int Die(int sides) => (int)Below((uint)sides) + 1;

        /// <summary>Sum of <paramref name="count"/> dice, e.g. Roll(2, 6) for 2d6.</summary>
        public int Roll(int count, int sides)
        {
            int total = 0;
            for (int i = 0; i < count; i++)
                total += Die(sides);
            return total;
        }

        /// <summary>
        /// The d66 that Ganesha Games tables use: first die is the tens digit, second the
        /// units, giving 36 flat outcomes 11-66. Note this is uniform, unlike 2d6.
        /// </summary>
        public int D66() => Die(6) * 10 + Die(6);

        public bool Chance(int inSix) => Die(6) <= inSix;

        public T Pick<T>(System.Collections.Generic.IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Cannot pick from an empty list.", nameof(items));

            return items[(int)Below((uint)items.Count)];
        }
    }
}
