using System.Linq;
using TeleCore;
using TeleCore.Primitive;
using Verse;

namespace TAE.Caching;

internal class AtmosphericScriber
{
    private Map map;

    private DefValueStack<AtmosphericDef, double>[] temporaryGrid;
    private DefValueStack<AtmosphericDef, double>[] atmosphericGrid;

    private AtmosphericMapInfo AtmosphericMapInfo => map.GetMapInfo<AtmosphericMapInfo>();

    internal AtmosphericScriber(Map map)
    {
        this.map = map;
    }

    public void ApplyLoadedDataToRegions()
    {
        if (atmosphericGrid == null) return;
            
        CellIndices cellIndices = map.cellIndices;
        var values = atmosphericGrid[map.cellIndices.NumGridCells];
        if (values.IsValid)
        {
            AtmosphericMapInfo.MapVolume.Volume.LoadFromStack(values);
        }

        foreach (var comp in AtmosphericMapInfo.AllAtmosphericRooms)
        {
            var index = cellIndices.CellToIndex(comp.Parent.Room.Cells.First());
            var valueStack = atmosphericGrid[index];
            if (valueStack.IsValid)
            {
                comp.Volume.LoadFromStack(valueStack);
            }
        }
            
        atmosphericGrid = null;
    }

    internal void ScribeData()
    {
        //TLog.Debug($"Exposing Atmospheric | {Scribe.mode}".Colorize(Color.cyan));
        int arraySize = map.cellIndices.NumGridCells + 1;
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            temporaryGrid = new DefValueStack<AtmosphericDef,double>[arraySize];
            var outsideAtmosphere = AtmosphericMapInfo.MapVolume.Volume.Stack;
            temporaryGrid[arraySize - 1] = outsideAtmosphere;
                
            foreach (var roomComp in AtmosphericMapInfo.AllAtmosphericRooms)
            {
                if (roomComp.IsOutdoors) continue;
                var roomAtmosphereStack = roomComp.Volume.Stack;
                foreach (IntVec3 c2 in roomComp.Room.Cells)
                {
                    temporaryGrid[map.cellIndices.CellToIndex(c2)] = roomAtmosphereStack;
                }
            }
        }


        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            atmosphericGrid = new DefValueStack<AtmosphericDef,double>[arraySize];
        }

        //Turn temp grid into byte arrays
        var savableTypes = DefDatabase<AtmosphericDef>.AllDefsListForReading;
        foreach (var type in savableTypes)
        {
            byte[] dataBytes = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                //GenSerialization.SerializeFloat(arraySize, (int idx) => temporaryGrid[idx].values?.FirstOrFallback(f => f.Def == type).Value ?? 0);
                dataBytes = DataSerializeUtility.SerializeUshort(arraySize, (int idx) => (ushort)(temporaryGrid[idx].Values?.FirstOrFallback(f => f.Def == type).Value ?? 0));
                DataExposeUtility.ByteArray(ref dataBytes, $"{type.defName}.atmospheric");
            }

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                DataExposeUtility.ByteArray(ref dataBytes, $"{type.defName}.atmospheric");
                DataSerializeUtility.LoadUshort(dataBytes, arraySize, delegate(int idx, ushort idxValue)
                {
                    var atmosStack =  new DefValue<AtmosphericDef,double>(type, idxValue);
                    if (atmosStack.Value > 0)
                    {
                        atmosphericGrid[idx] += atmosStack;
                    }
                });
                    
                /*
                GenSerialization.LoadFloat(dataBytes, arraySize, delegate (int idx, float idxValue)
                {
                    atmosphericGrid[idx] += new DefValueStack<AtmosphericDef>(type, idxValue);
                });
                */
            }
        }
    }
}