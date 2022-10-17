using TeleCore;
using TeleCore.Static;
using UnityEngine;
using Verse;

namespace TAE;

public class DynamicDataCacheInfo : MapInformation
{
    //Grid data
    public ComputeGrid<float> AtmosphericPassGrid { get; }
    public ComputeGrid<float> LightPassGrid { get; }
    public ComputeGrid<uint> EdificeGrid { get; }

    public ComputeBuffer AtmosphericBuffer => AtmosphericPassGrid.DataBuffer;
    public ComputeBuffer LightPassBuffer => LightPassGrid.DataBuffer;
    public ComputeBuffer EdificeBuffer => EdificeGrid.DataBuffer;

    public DynamicDataCacheInfo(Map map) : base(map)
    {
        AtmosphericPassGrid = new ComputeGrid<float>(map, _ => 1f);
        LightPassGrid = new ComputeGrid<float>(map, _ => 1f);
        EdificeGrid = new ComputeGrid<uint>(map);
    }

    public override void ThreadSafeInit()
    {
        AtmosphericPassGrid.ThreadSafeInit();
        LightPassGrid.ThreadSafeInit();
        EdificeGrid.ThreadSafeInit();

        AtmosphericPassGrid.UpdateCPUData();
        LightPassGrid.UpdateCPUData();
        EdificeGrid.UpdateCPUData();
    }

    /*
    public void UpdateGraphics()
    {
        if (!AtmosphericPassGrid.IsReady) return;
        
        Color[] colors = new Color[map.cellIndices.NumGridCells];
        Color[] colors2 = new Color[map.cellIndices.NumGridCells];
        Color[] colors3 = new Color[map.cellIndices.NumGridCells];
        IntVec2 size = new IntVec2(map.Size.x, map.Size.z);
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color(0, atmosphericPassGrid[i], 0);
            colors2[i] = new Color(lightPassGrid[i], 0, 0);
            colors3[i] = new Color(0, 0, edificeGrid[i]);
        }

        TiberiumContent.GenerateTextureFrom(colors, size, "AtmosphericGrid");
        TiberiumContent.GenerateTextureFrom(colors2, size, "LightPassGrid");
        TiberiumContent.GenerateTextureFrom(colors3, size, "EdificeGrid");
        
    }
    */
    
    internal void Notify_UpdateThingState(Thing thing)
    {
        var isBuilding = thing is Building;
        foreach (var pos in thing.OccupiedRect())
        {
            if (isBuilding)
            {
                //
                AtmosphericPassGrid.SetValue(pos, AtmosphericTransferWorker.DefaultAtmosphericPassPercent(thing));
                if (thing.def.IsEdifice())
                    EdificeGrid.SetValue(pos, 1);
                if (thing.def.blockLight)
                    LightPassGrid.SetValue(pos, 0);
            }
        }
    }

    internal void Notify_ThingSpawned(Thing thing)
    {
        Notify_UpdateThingState(thing);
        //UpdateGraphics();
    }

    internal void Notify_ThingDespawned(Thing thing)
    {
        foreach (var pos in thing.OccupiedRect())
        {
            if (thing is Building b)
            {
                AtmosphericPassGrid.ResetValue(pos, 1f);
                if (b.def.IsEdifice())
                    EdificeGrid.ResetValue(pos);
                if (b.def.blockLight)
                    LightPassGrid.ResetValue(pos, 1f);
            }
        }
    }
}

public class DynamicDataTracker : ThingTrackerComp
{
    private DynamicDataCacheInfo cacheInfo;

    private DynamicDataCacheInfo CacheInfo(Map map)
    {
        if (cacheInfo == null)
        {
            cacheInfo = map.GetMapInfo<DynamicDataCacheInfo>();
        }
        return cacheInfo;
    }

    //TODO: Update to use protected parent later
    public DynamicDataTracker(ThingTrackerInfo parent) : base(parent)
    {
    }

    public override void Notify_ThingRegistered(Thing thing)
    {
        thing.Map.GetMapInfo<DynamicDataCacheInfo>().Notify_ThingSpawned(thing);
    }

    public override void Notify_ThingDeregistered(Thing thing)
    {
        thing.Map.GetMapInfo<DynamicDataCacheInfo>().Notify_ThingDespawned(thing);
    }

    public override void Notify_ThingStateChanged(Thing thing, string compSignal = null)
    {
        switch (compSignal)
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
                thing.Map.GetMapInfo<DynamicDataCacheInfo>().Notify_UpdateThingState(thing);
            }
            break;
        }
    }
}