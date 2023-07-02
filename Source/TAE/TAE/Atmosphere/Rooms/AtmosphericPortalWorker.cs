using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TeleCore;
using TeleCore.FlowCore;

namespace TAE.Atmosphere.Rooms;

public class AtmosphericPortalWorker : RoomPortalWorker
{
    private RoomComponent_Atmosphere _from;
    private RoomComponent_Atmosphere _to;
    private AtmosInterface _interface;
    
    private readonly Dictionary<AtmosphericDef, FlowResult<AtmosphericDef, double>> lastResultByDef = new();
    
    public bool ConnectsToOutside => _from.IsOutdoors || _to.IsOutdoors;
    public bool ConnectsToSelf => _from.IsOutdoors && _to.IsOutdoors || _from == _to;
    //TODO: public bool IsValid => parent is {Spawned: true}; // && connections[0] != null && connections[1] != null;
    
    public AtmosphericPortalWorker(RoomPortal parent) : base(parent)
    {
        if(!parent.IsValid)
        {
            throw new ArgumentException("parent RoomPortal must be valid", nameof(parent));
        }

        _from = parent[0]?.GetRoomComp<RoomComponent_Atmosphere>();
        _to = parent[1]?.GetRoomComp<RoomComponent_Atmosphere>();

        if(_from == null || _to == null)
        {
            throw new InvalidOperationException("Could not get the RoomComponent_Atmospheric from the parent");
        }
    }
    
    
}