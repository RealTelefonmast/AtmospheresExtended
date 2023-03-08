using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE.Caching
{
    /// <summary>
    /// Handles all atmospheric values on a map and within rooms
    /// </summary>
    public class AtmosphericCache : IExposable
    {
        private Map map;
        private HashSet<int> processedRooms;
        private CachedAtmosData[] atmosGrid;
        internal readonly AtmosphericScriber scriber;

        public AtmosphericMapInfo AtmosphericMapInfo => map.GetMapInfo<AtmosphericMapInfo>();

        public AtmosphericCache(Map map)
        {
            this.map = map;
            processedRooms = new();
            atmosGrid = new CachedAtmosData[map.cellIndices.NumGridCells];
            scriber = new AtmosphericScriber(map);
        }

        public void ExposeData()
        {
            scriber.ScribeData();
        }

        //
        public void SetData(IntVec3 pos, CachedAtmosData atmosData)
        {
            atmosGrid[map.cellIndices.CellToIndex(pos)] = atmosData;
        }

        public void ResetInfo(IntVec3 pos)
        {
            atmosGrid[map.cellIndices.CellToIndex(pos)].Reset();
        }

        public void TryCacheRegionAtmosphericInfo(IntVec3 pos, Region reg)
        {
            Room room = reg.Room;
            if (room == null) return;
            SetData(pos, new CachedAtmosData(room.ID, room.CellCount, AtmosphericMapInfo.ComponentAt(room)));
        }

        public bool TryGetAtmosphericValuesForRoom(Room r, out DefValueStack<AtmosphericDef> result)
        {
            CellIndices cellIndices = this.map.cellIndices;
            result = new();
            foreach (var c in r.Cells)
            {
                CachedAtmosData cachedInfo = this.atmosGrid[cellIndices.CellToIndex(c)];

                //If already processed or not a room, ignore
                if (cachedInfo.numCells <= 0 || processedRooms.Contains(cachedInfo.roomID) || cachedInfo.stack.Empty) continue;
                processedRooms.Add(cachedInfo.roomID);
                foreach (var value in cachedInfo.stack.Values)
                {
                    float addedValue = value.Value;
                    if (cachedInfo.numCells > r.CellCount)
                    {
                        addedValue = value.Value * (r.CellCount / (float)cachedInfo.numCells);
                    }
                    
                    //
                    result += (value.Def, Mathf.Round(addedValue));
                }
            }
            processedRooms.Clear();
            return !result.Empty;
        }
    }
}
