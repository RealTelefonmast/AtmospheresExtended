using TeleCore;
using TeleCore.Data.Events;
using TeleCore.Static;
using Verse;

namespace TAE;

public class DynamicDataTracker : ThingTrackerComp
{
    private DynamicDataCacheMapInfo cacheMapInfo;

    private DynamicDataCacheMapInfo CacheInfo(Map map)
    {
        if (cacheMapInfo == null)
        {
            cacheMapInfo = map.GetMapInfo<DynamicDataCacheMapInfo>();
        }
        return cacheMapInfo;
    }

    //TODO: Update to use protected parent later
    public DynamicDataTracker(ThingTrackerMapInfo parent) : base(parent)
    {
    }
    
    public override void Notify_ThingRegistered(ThingStateChangedEventArgs args)
    {
        args.Thing.Map.GetMapInfo<DynamicDataCacheMapInfo>().Notify_ThingSpawned(args.Thing);
        args.Thing.Map.GetMapInfo<SpreadingGasGrid>().Notify_ThingSpawned(args.Thing);
    }

    public override void Notify_ThingDeregistered(ThingStateChangedEventArgs args)
    {
        args.Thing.Map.GetMapInfo<DynamicDataCacheMapInfo>().Notify_ThingDespawned(args.Thing);
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
                args.Thing.Map.GetMapInfo<DynamicDataCacheMapInfo>().Notify_UpdateThingState(args.Thing);
            }
                break;
        }
    }
}