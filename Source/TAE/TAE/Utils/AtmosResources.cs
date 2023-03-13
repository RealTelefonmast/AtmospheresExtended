﻿using System.Collections.Generic;
using System.Linq;
using TeleCore.FlowCore;
using Verse;

namespace TAE;

public static class AtmosResources
{
    public static List<AtmosphericDef> AllAtmosphericDefs => DefDatabase<AtmosphericDef>.AllDefsListForReading;

    public static ContainerConfig DefaultAtmosConfig(int roomSize)
    {
        return new ContainerConfig
        {
            containerClass = typeof(AtmosphericContainer),
            baseCapacity = roomSize * AtmosMath.CELL_CAPACITY,
            containerLabel = "mm yes air and stuff",
            storeEvenly = false,
            dropContents = false,
            leaveContainer = false,
            droppedContainerDef = null!,
            valueDefs = AllAtmosphericDefs.Cast<FlowValueDef>().ToList(),
            explosionProps = null!
        };
    }
}