using System.Collections.Generic;
using System.Linq;
using TeleCore.FlowCore;
using TeleCore.Generics.Container;
using Verse;

namespace TAE;

public static class AtmosResources
{
    public static List<AtmosphericDef> AllAtmosphericDefs => DefDatabase<AtmosphericDef>.AllDefsListForReading;

    public static ContainerConfig<AtmosphericDef> DefaultAtmosConfig(int roomSize)
    {
        return new ContainerConfig<AtmosphericDef>
        {
            containerClass = typeof(AtmosphericContainer),
            baseCapacity = roomSize * AtmosMath.CELL_CAPACITY,
            containerLabel = "mm yes air and stuff",
            storeEvenly = false,
            dropContents = false,
            leaveContainer = false,
            droppedContainerDef = null!,
            valueDefs = AllAtmosphericDefs.ToList(),
            explosionProps = null!
        };
    }
}