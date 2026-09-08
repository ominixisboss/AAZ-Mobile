using System;
using System.Collections.Generic;
using UnityEngine;

namespace AAZ.Core
{
    /// <summary>One location in the Zone. Content stays as string keys so the rules tables,
    /// not code, decide what actually lives here.</summary>
    [Serializable]
    public sealed class HexCell
    {
        public HexCoord coord;

        [Tooltip("Terrain key, rolled from a terrain table.")]
        public string terrainId;

        [Tooltip("What was rolled here, resolved on first entry.")]
        public string contentKey;

        [Tooltip("Seen from a distance - drawn on the map, contents unknown.")]
        public bool scouted;

        [Tooltip("Entered at least once - contents resolved and remembered.")]
        public bool explored;

        public HexCell() { }

        public HexCell(HexCoord coord) => this.coord = coord;
    }

    /// <summary>
    /// A hex map with fog of war, backed by a dictionary for lookup and a list for saving.
    /// <para>
    /// Content is resolved lazily, on entry, rather than at generation time. That is how the
    /// tabletop game works - you roll when you get there - and it means a run's map is
    /// defined entirely by its seed plus the order the player walked it.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class HexMap : ISerializationCallbackReceiver
    {
        [SerializeField] List<HexCell> m_Cells = new List<HexCell>();

        [NonSerialized] Dictionary<HexCoord, HexCell> m_Lookup = new Dictionary<HexCoord, HexCell>();

        public IReadOnlyList<HexCell> Cells => m_Cells;

        public int Count => m_Cells.Count;

        /// <summary>Fills a hex-shaped region of the given radius with empty cells.</summary>
        public static HexMap CreateDisc(int radius)
        {
            var map = new HexMap();
            foreach (HexCoord coord in HexCoord.Spiral(HexCoord.Zero, radius))
                map.Add(new HexCell(coord));
            return map;
        }

        public bool TryGet(HexCoord coord, out HexCell cell) => m_Lookup.TryGetValue(coord, out cell);

        public HexCell Get(HexCoord coord) => m_Lookup.TryGetValue(coord, out HexCell cell) ? cell : null;

        public bool Contains(HexCoord coord) => m_Lookup.ContainsKey(coord);

        public HexCell Add(HexCell cell)
        {
            if (cell == null)
                throw new ArgumentNullException(nameof(cell));

            if (m_Lookup.TryGetValue(cell.coord, out HexCell existing))
                return existing;

            m_Cells.Add(cell);
            m_Lookup[cell.coord] = cell;
            return cell;
        }

        /// <summary>Marks everything within <paramref name="radius"/> as seen but not entered.</summary>
        public void Scout(HexCoord center, int radius)
        {
            foreach (HexCoord coord in HexCoord.Spiral(center, radius))
            {
                if (m_Lookup.TryGetValue(coord, out HexCell cell))
                    cell.scouted = true;
            }
        }

        /// <summary>Neighbouring cells that exist on the map.</summary>
        public IEnumerable<HexCell> NeighboursOf(HexCoord coord)
        {
            foreach (HexCoord step in coord.Neighbours())
            {
                if (m_Lookup.TryGetValue(step, out HexCell cell))
                    yield return cell;
            }
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            m_Lookup = new Dictionary<HexCoord, HexCell>(m_Cells.Count);
            foreach (HexCell cell in m_Cells)
            {
                if (cell != null)
                    m_Lookup[cell.coord] = cell;
            }
        }
    }
}
