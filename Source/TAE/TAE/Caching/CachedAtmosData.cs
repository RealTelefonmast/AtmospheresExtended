using TAE.Atmosphere.Rooms;
using TeleCore;
using TeleCore.Primitive;

namespace TAE.Caching;

public struct CachedAtmosData
{
    public int roomID;
    public DefValueStack<AtmosphericDef,double> stack;

    public CachedAtmosData()
    {
        roomID = -1;
        stack = new DefValueStack<AtmosphericDef, double>();
    }
        
    public CachedAtmosData(RoomComponent_Atmosphere roomComp)
    {
        roomID = roomComp.Room.ID;
        stack = roomComp.Volume.Stack;
        if (roomComp.IsOutdoors)
        {
            stack += roomComp.AtmosphericInfo.MapVolume.Stack;
        }
    }
        
    public override string ToString()
    {
        return $"[{roomID}][{stack.Empty}]\n{stack}";
    }
}