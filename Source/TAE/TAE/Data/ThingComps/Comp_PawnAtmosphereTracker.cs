using System.Collections.Generic;
using TAE.Atmosphere.Rooms;
using Verse;

namespace TAE;

public class Comp_PawnAtmosphereTracker : ThingComp
{
    private static readonly Dictionary<Pawn, Comp_PawnAtmosphereTracker> OneOffs = new();
    private RoomComponent_Atmosphere currentAtmosphere;

    public Pawn Pawn => parent as Pawn;

    public bool IsOutside => currentAtmosphere.IsOutdoors;

    public RoomComponent_Atmosphere RoomComp => currentAtmosphere;

    public static Comp_PawnAtmosphereTracker CompFor(Pawn pawn)
    {
        if (OneOffs.TryGetValue(pawn, out var value))
        {
            return value;
        }
        return null;
    }

    public override void PostPostMake()
    {
        base.PostPostMake();
        if (OneOffs.TryAdd(Pawn, this))
        {
            //...
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
    }

    public bool IsInAtmosphere(AtmosphericDef def)
    {
        return currentAtmosphere.Volume.StoredValueOf(def) > 0;
    }

    //
    public void Notify_EnteredAtmosphere(RoomComponent_Atmosphere atmosphere)
    {
        currentAtmosphere = atmosphere;
    }

    //Implies leaving outside
    public void Notify_Clear()
    {
        currentAtmosphere = null;
    }
}