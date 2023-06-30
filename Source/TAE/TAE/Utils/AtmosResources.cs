using System.Collections.Generic;
using System.Linq;
using TeleCore.Generics.Container;
using Verse;

namespace TAE;

public static class AtmosResources
{   
    internal const int CELL_CAPACITY = 128;
    public const float MIN_EQ_VAL = 2;
    
    public static List<AtmosphericDef> AllAtmosphericDefs => DefDatabase<AtmosphericDef>.AllDefsListForReading;
}