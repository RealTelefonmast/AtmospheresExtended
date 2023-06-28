using System;
using JetBrains.Annotations;
using TeleCore;

namespace TAE.Atmosphere.Rooms;

public class AtmosphericPortalWorker : RoomPortalWorker
{
    private RoomComponent_Atmospheric _from;
    private RoomComponent_Atmospheric _to;
    
    public AtmosphericPortalWorker([NotNull] RoomPortal parent) : base(parent)
    {
        if(!parent.IsValid)
        {
            throw new ArgumentException("parent RoomPortal must be valid", nameof(parent));
        }

        _from = parent[0]?.GetRoomComp<RoomComponent_Atmospheric>();
        _to = parent[1]?.GetRoomComp<RoomComponent_Atmospheric>();

        if(_from == null || _to == null)
        {
            throw new InvalidOperationException("Could not get the RoomComponent_Atmospheric from the parent");
        }
    }
}