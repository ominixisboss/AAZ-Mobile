using System;
using UnityEngine;

namespace AAZ.Core
{
    /// <summary>
    /// The whole state of one expedition, in a form that survives an app kill.
    /// <para>
    /// Mobile sessions get interrupted constantly, so a run has to be resumable at any
    /// moment. Persisting the generator state alongside the map is what makes that safe: a
    /// reloaded run continues the same die sequence instead of re-rolling luck.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ZoneRun
    {
        public uint seed;
        public uint rngState;
        public int turn;
        public HexCoord position;
        public HexMap map;

        [NonSerialized] Rng m_Rng;

        /// <summary>Generator for this run. Its state is written back on every save.</summary>
        public Rng Rng => m_Rng ??= new Rng(rngState != 0u ? rngState : seed);

        public static ZoneRun Create(uint seed, int mapRadius)
        {
            var run = new ZoneRun
            {
                seed = seed,
                rngState = seed,
                turn = 0,
                position = HexCoord.Zero,
                map = HexMap.CreateDisc(mapRadius),
            };

            run.map.Scout(HexCoord.Zero, 1);

            if (run.map.TryGet(HexCoord.Zero, out HexCell start))
                start.explored = true;

            return run;
        }

        public string ToJson() => JsonUtility.ToJson(Sync(), prettyPrint: true);

        public static ZoneRun FromJson(string json)
        {
            var run = JsonUtility.FromJson<ZoneRun>(json);
            run?.map?.OnAfterDeserialize();
            return run;
        }

        /// <summary>Copies live generator state back into the serialisable field.</summary>
        public ZoneRun Sync()
        {
            if (m_Rng != null)
                rngState = m_Rng.State;
            return this;
        }
    }
}
