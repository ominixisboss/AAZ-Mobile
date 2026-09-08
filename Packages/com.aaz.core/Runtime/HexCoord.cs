using System;
using System.Collections.Generic;
using UnityEngine;

namespace AAZ.Core
{
    /// <summary>
    /// Axial hex coordinate (q, r). Axial storage keeps the struct to two ints while all the
    /// interesting maths - distance, rings, line-of-sight - stays exact in cube space, where
    /// the third component is always -q-r.
    /// </summary>
    [Serializable]
    public struct HexCoord : IEquatable<HexCoord>
    {
        public int q;
        public int r;

        public HexCoord(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        public static readonly HexCoord Zero = new HexCoord(0, 0);

        /// <summary>The six axial steps, in clockwise order starting east.</summary>
        public static readonly HexCoord[] Directions =
        {
            new HexCoord( 1,  0),
            new HexCoord( 1, -1),
            new HexCoord( 0, -1),
            new HexCoord(-1,  0),
            new HexCoord(-1,  1),
            new HexCoord( 0,  1),
        };

        /// <summary>Implied cube coordinate: cube constraint is x + y + z == 0.</summary>
        public int x => q;
        public int y => -q - r;
        public int z => r;

        public static HexCoord operator +(HexCoord a, HexCoord b) => new HexCoord(a.q + b.q, a.r + b.r);
        public static HexCoord operator -(HexCoord a, HexCoord b) => new HexCoord(a.q - b.q, a.r - b.r);
        public static HexCoord operator *(HexCoord a, int k) => new HexCoord(a.q * k, a.r * k);
        public static bool operator ==(HexCoord a, HexCoord b) => a.q == b.q && a.r == b.r;
        public static bool operator !=(HexCoord a, HexCoord b) => !(a == b);

        public bool Equals(HexCoord other) => this == other;
        public override bool Equals(object obj) => obj is HexCoord other && this == other;
        public override int GetHashCode() => unchecked((q * 31) ^ (r * 131));
        public override string ToString() => $"({q}, {r})";

        public static int Distance(HexCoord a, HexCoord b)
        {
            int dq = a.q - b.q;
            int dr = a.r - b.r;
            return (Mathf.Abs(dq) + Mathf.Abs(dq + dr) + Mathf.Abs(dr)) / 2;
        }

        public int DistanceTo(HexCoord other) => Distance(this, other);

        public HexCoord Neighbour(int direction) => this + Directions[((direction % 6) + 6) % 6];

        public IEnumerable<HexCoord> Neighbours()
        {
            for (int i = 0; i < 6; i++)
                yield return this + Directions[i];
        }

        /// <summary>Every hex exactly <paramref name="radius"/> steps away, walked in order.</summary>
        public static List<HexCoord> Ring(HexCoord center, int radius)
        {
            var results = new List<HexCoord>();

            if (radius <= 0)
            {
                results.Add(center);
                return results;
            }

            // Start on the ring, then walk each of the six edges in turn.
            HexCoord hex = center + Directions[4] * radius;
            for (int side = 0; side < 6; side++)
            {
                for (int step = 0; step < radius; step++)
                {
                    results.Add(hex);
                    hex += Directions[side];
                }
            }

            return results;
        }

        /// <summary>Every hex within <paramref name="radius"/>, centre first, spiralling out.</summary>
        public static List<HexCoord> Spiral(HexCoord center, int radius)
        {
            var results = new List<HexCoord> { center };
            for (int r = 1; r <= radius; r++)
                results.AddRange(Ring(center, r));
            return results;
        }

        /// <summary>
        /// Pointy-top layout onto the XZ ground plane, which is where an HD-2D map lives:
        /// the board lies flat and the camera looks down at it.
        /// </summary>
        public Vector3 ToWorld(float hexSize)
        {
            const float sqrt3 = 1.7320508f;
            float wx = hexSize * sqrt3 * (q + r * 0.5f);
            float wz = hexSize * 1.5f * r;
            return new Vector3(wx, 0f, wz);
        }

        /// <summary>Inverse of <see cref="ToWorld"/>, rounded to the nearest hex.</summary>
        public static HexCoord FromWorld(Vector3 world, float hexSize)
        {
            const float sqrt3 = 1.7320508f;
            float fq = (world.x / sqrt3 - world.z / 3f) / hexSize;
            float fr = (world.z * (2f / 3f)) / hexSize;
            return Round(fq, fr);
        }

        /// <summary>Rounds fractional axial coordinates by way of cube space.</summary>
        public static HexCoord Round(float fq, float fr)
        {
            float fx = fq;
            float fz = fr;
            float fy = -fx - fz;

            int rx = Mathf.RoundToInt(fx);
            int ry = Mathf.RoundToInt(fy);
            int rz = Mathf.RoundToInt(fz);

            float dx = Mathf.Abs(rx - fx);
            float dy = Mathf.Abs(ry - fy);
            float dz = Mathf.Abs(rz - fz);

            // Discard whichever component moved furthest, so the cube constraint holds.
            if (dx > dy && dx > dz)
                rx = -ry - rz;
            else if (dy <= dz)
                rz = -rx - ry;

            return new HexCoord(rx, rz);
        }
    }
}
