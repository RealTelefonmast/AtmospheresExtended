﻿using TeleCore;
using TeleCore.Data.Events;
using TeleCore.Static;
using Verse;

namespace TAE;

public class DynamicAtmosphericDataTracker : ThingTrackerComp
{
    private DynamicAtmosphericDataMapInfo _mapInfo;

    private DynamicAtmosphericDataMapInfo CacheInfo(Map map)
    {
        if (_mapInfo == null)
        {
            _mapInfo = map.GetMapInfo<DynamicAtmosphericDataMapInfo>();
        }
        return _mapInfo;
    }

    //TODO: Update to use protected parent later
    public DynamicAtmosphericDataTracker(ThingTrackerMapInfo parent) : base(parent)
    {
    }
    
    public override void Notify_ThingRegistered(ThingStateChangedEventArgs args)
    {
        args.Thing.Map.GetMapInfo<DynamicAtmosphericDataMapInfo>().Notify_ThingSpawned(args.Thing);
        args.Thing.Map.GetMapInfo<SpreadingGasGrid>().Notify_ThingSpawned(args.Thing);
    }

    public override void Notify_ThingDeregistered(ThingStateChangedEventArgs args)
    {
        args.Thing.Map.GetMapInfo<DynamicAtmosphericDataMapInfo>().Notify_ThingDespawned(args.Thing);
    }

    public override void Notify_ThingSentSignal(ThingStateChangedEventArgs args)
    {
        switch (args.CompSignal)
        {
            case KnownCompSignals.FlickedOn:
            case KnownCompSignals.FlickedOff:
            case KnownCompSignals.PowerTurnedOn:
            case KnownCompSignals.PowerTurnedOff:
            case KnownCompSignals.RanOutOfFuel:
            case KnownCompSignals.Refueled:
            case "DoorOpened":
            case "DoorClosed":
            {
                args.Thing.Map.GetMapInfo<DynamicAtmosphericDataMapInfo>().Notify_ThingSentSignal(args);
            }
                break;
        }
    }
}