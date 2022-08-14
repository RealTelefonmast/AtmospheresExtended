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
    internal class AtmosphericScriber
    {
        private Map map;

        private DefValueStack<AtmosphericDef>[] temporaryGrid;
        private DefValueStack<AtmosphericDef>[] atmosphericGrid;

        private AtmosphericMapInfo AtmosphericMapInfo => map.GetMapInfo<AtmosphericMapInfo>();


        internal AtmosphericScriber(Map map)
        {
            this.map = map;
        }

        public void ApplyLoadedDataToRegions()
        {
            if (atmosphericGrid == null) return;

            CellIndices cellIndices = map.cellIndices;
            AtmosphericMapInfo.MapContainer.Data_LoadFromStack(atmosphericGrid[map.cellIndices.NumGridCells]);
            TLog.Message($"Applying Outside Atmospheric: {atmosphericGrid[map.cellIndices.NumGridCells]}");

            foreach (var comp in AtmosphericMapInfo.AllAtmosphericRooms)
            {
                var valueStack = atmosphericGrid[cellIndices.CellToIndex(comp.Parent.Room.Cells.First())];
                comp.RoomContainer.Data_LoadFromStack(valueStack);
                TLog.Message($"Applying on Tracker {comp.Room.ID}: {valueStack}");
            }
            //
            /*
            foreach (Region region in map.regionGrid.AllRegions_NoRebuild_InvalidAllowed)
            {
                if (region.Room != null)
                {
                    var valueStack = atmosphericGrid[cellIndices.CellToIndex(region.Cells.First())];
                    Log.Message($"Applying on Region  {region.Room.ID}: {valueStack}");
                    AtmosphericMapInfo.PollutionFor(region.Room).ActualContainer.Container.LoadFromStack(valueStack);
                }
            }
            */
            //

            atmosphericGrid = null;
        }

        internal void ScribeData()
        {
            Log.Message($"[TAE] Exposing Atmospheric | {Scribe.mode}".Colorize(Color.cyan));
            int arraySize = map.cellIndices.NumGridCells + 1;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                temporaryGrid = new DefValueStack<AtmosphericDef>[arraySize];
                var outsideAtmosphere = AtmosphericMapInfo.MapContainer.ValueStack;
                temporaryGrid[arraySize - 1] = outsideAtmosphere;

                foreach (var roomComp in AtmosphericMapInfo.AllAtmosphericRooms)
                {
                    if (roomComp.IsOutdoors) continue;
                    var roomAtmosphereStack = roomComp.RoomContainer.ValueStack;
                    foreach (IntVec3 c2 in roomComp.Room.Cells)
                    {
                        temporaryGrid[map.cellIndices.CellToIndex(c2)] = roomAtmosphereStack;
                    }
                }
            }

            //Turn temp grid into byte arrays
            var savableTypes = DefDatabase<AtmosphericDef>.AllDefsListForReading;
            foreach (var type in savableTypes)
            {
                byte[] dataBytes = null;
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    dataBytes = GenSerialization.SerializeFloat(arraySize, (int idx) => temporaryGrid[idx].values?.FirstOrFallback(f => f.Def == type).Value ?? 0);
                    DataExposeUtility.ByteArray(ref dataBytes, $"{type.defName}.atmospheric");
                }

                if (Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    DataExposeUtility.ByteArray(ref dataBytes, $"{type.defName}.atmospheric");
                    atmosphericGrid = new DefValueStack<AtmosphericDef>[arraySize];
                    GenSerialization.LoadFloat(dataBytes, arraySize, delegate (int idx, float idxValue)
                    {
                        if (idxValue > 0)
                        {
                            Log.Message($"Loading {idx}[{map.cellIndices.IndexToCell(idx)}]: {idxValue} ({type})");
                        }
                        atmosphericGrid[idx] += new DefValueStack<AtmosphericDef>(type, idxValue);
                    });
                }
            }
        }
    }
}
