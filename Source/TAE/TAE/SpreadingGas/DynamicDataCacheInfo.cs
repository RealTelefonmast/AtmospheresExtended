using TeleCore;
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

    public void Notify_UpdateThingState(Thing thing)
    {
        foreach (var pos in thing.OccupiedRect())
        {
            if (thing is Building b)
            {
                //TODO: Map AtmosDef to int ID for compute shader def-based pass percent
                AtmosphericPassGrid.SetValue(pos, AtmosphericTransferWorker.AtmosphericPassPercent(b));
                if (b.def.IsEdifice())
                    EdificeGrid.SetValue(pos, 1);
                if (b.def.blockLight)
                    LightPassGrid.SetValue(pos, 0);
            }
        }
    }

    public void Notify_ThingSpawned(Thing thing)
    {
        Notify_UpdateThingState(thing);
        //UpdateGraphics();
    }

    public void Notify_ThingDespawned(Thing thing)
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