using System.Collections.Generic;
using System.Linq;
using TeleCore;
using TeleCore.FlowCore;
using TeleCore.Generics.Container;
using TeleCore.Network.Data;
using Verse;

namespace TAC;

public static class AtmosResources
{   
    // 1000L of Air = 1m^3 of Air
    //We assume a rimworld tile has a volume of 2m^3
    internal const float CELL_FLOOR = 2.25f; //1.5*1.5
    internal const float CELL_HEIGHT = 2.5f;
    internal const int CELL_CAPACITY = 5625; // 2.25 * 2.5 * 1000
    public const float MIN_EQ_VAL = 2;
    
    public static List<AtmosphericValueDef> AllAtmosphericDefs => DefDatabase<AtmosphericValueDef>.AllDefsListForReading;
    
    public static FlowVolumeConfig<AtmosphericValueDef> DefaultAtmosConfig(int roomSize)
    {
        return new FlowVolumeConfig<AtmosphericValueDef>
        {
            values = new FlowVolumeConfig<AtmosphericValueDef>.Values()
            {
                allowedValues = AllAtmosphericDefs,
            },
            capacity = roomSize * CELL_CAPACITY,
            area = 0,
            elevation = 0,
            height = 0
            
            // containerLabel = "mm yes air and stuff",
            // storeEvenly = false,
            // dropContents = false,
            // leaveContainer = false,
            // droppedContainerDef = null!,
            // valueDefs = AllAtmosphericDefs.ToList(),
            // explosionProps = null!
        };
    }
    
}