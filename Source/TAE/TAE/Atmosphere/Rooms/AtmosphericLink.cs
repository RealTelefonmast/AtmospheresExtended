using Verse;

namespace TAE.Atmosphere.Rooms;

public class AtmosphericLink
{
    private Thing _connector;
    private RoomComponent_Atmosphere _a;
    private RoomComponent_Atmosphere _b;
    private AtmosInterface _interface;
    
    public bool ConnectsToOutside => _a.IsOutdoors || _b.IsOutdoors;
    public bool ConnectsToSelf => _a.IsOutdoors && _b.IsOutdoors || _a == _b;

    public AtmosphericLink(Thing thing, RoomComponent_Atmosphere a, RoomComponent_Atmosphere b)
    {
        _connector = thing;
        _a = a;
        _b = b;
    }
}
