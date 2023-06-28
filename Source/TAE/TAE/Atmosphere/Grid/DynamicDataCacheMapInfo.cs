﻿using TeleCore;
using TeleCore.Data.Events;
using TeleCore.Static;
using UnityEngine;
using Verse;

namespace TAE;

public class DynamicDataCacheMapInfo : MapInformation
{
    //Grid data
    public ComputeGrid<float> AtmosphericPassGrid { get; }
    public ComputeGrid<float> LightPassGrid { get; }
    public ComputeGrid<uint> EdificeGrid { get; }

    public ComputeBuffer AtmosphericBuffer => AtmosphericPassGrid.DataBuffer;
    public ComputeBuffer LightPassBuffer => LightPassGrid.DataBuffer;
    public ComputeBuffer EdificeBuffer => EdificeGrid.DataBuffer;

    public DynamicDataCacheMapInfo(Map map) : base(map)
    {
        AtmosphericPassGrid = new ComputeGrid<float>(map, _ => 1f);
        LightPassGrid = new ComputeGrid<float>(map, _ => 1f);
        EdificeGrid = new ComputeGrid<uint>(map);
    }

    //TODO: Let data get filled directly in map load - update buffer later
    public override void ThreadSafeInit()
    {
        AtmosphericPassGrid.ThreadSafeInit();
        LightPassGrid.ThreadSafeInit();
        EdificeGrid.ThreadSafeInit();

        AtmosphericPassGrid.UpdateBuffer();
        LightPassGrid.UpdateBuffer();
        EdificeGrid.UpdateBuffer();
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
            if (!isBuilding) continue;
            
            //
            AtmosphericPassGrid.SetValue_Array(pos, AtmosphereUtility.DefaultAtmosphericPassPercentAtCell(pos, map));
            if (thing.def.IsEdifice())
                EdificeGrid.SetValue_Array(pos, 1);
            if (thing.def.blockLight)
                LightPassGrid.SetValue_Array(pos, 0);
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
                if(AtmosphericPassGrid.IsReady)
                    AtmosphericPassGrid.ResetValue(pos, 1f);
                if (b.def.IsEdifice() && EdificeGrid.IsReady)
                    EdificeGrid.ResetValue(pos);
                if (b.def.blockLight && LightPassGrid.IsReady)
                    LightPassGrid.ResetValue(pos, 1f);
            }
        }
    }
}

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