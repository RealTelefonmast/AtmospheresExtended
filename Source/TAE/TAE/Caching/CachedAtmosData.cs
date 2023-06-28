using TeleCore;
using TeleCore.Primitive;

namespace TAE.Caching;

public struct CachedAtmosData
{
    public int roomID;
    public DefValueStack<AtmosphericDef> stack;

    public CachedAtmosData()
    {
        roomID = -1;
        stack = new DefValueStack<AtmosphericDef>();
    }
        
    public CachedAtmosData(RoomComponent_Atmospheric roomComp)
    {
        roomID = roomComp.Room.ID;
        stack = roomComp.Container.ValueStack;
        if (roomComp.IsOutdoors)
        {
            stack += roomComp.OutsideContainer.ValueStack;
        }
    }
        
    public override string ToString()
    {
        return $"[{roomID}][{stack.Empty}]\n{stack}";
    }
}