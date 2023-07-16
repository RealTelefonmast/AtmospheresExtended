using System.Collections.Generic;
using System.Linq;
using TeleCore;
using TeleCore.Generics.Container;
using TeleCore.Network.Data;
using Verse;

namespace TAE;

public static class AtmosResources
{   
    internal const int CELL_CAPACITY = 128;
    public const float MIN_EQ_VAL = 2;
    
    public static List<AtmosphericDef> AllAtmosphericDefs => DefDatabase<AtmosphericDef>.AllDefsListForReading;
    
    public static FlowVolumeConfig<AtmosphericDef> DefaultAtmosConfig(int roomSize)
    {
        return new FlowVolumeConfig<AtmosphericDef>
        {
            allowedValues = AllAtmosphericDefs,
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