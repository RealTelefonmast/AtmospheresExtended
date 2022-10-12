using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using RimWorld;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE;

[StructLayout(LayoutKind.Explicit, Size = 48)]
public struct GasCellValue
{
    [FieldOffset(0)] public ushort defID = 0;

    [FieldOffset(16)] public readonly uint totalBitVal = 0;
    [FieldOffset(16)] public ushort value = 0;
    [FieldOffset(32)] public ushort overflow = 0;

    public GasCellValue(ushort defID, ushort value)
    {
        this.defID = defID;
        this.value = value;
    }

    public GasCellValue(ushort defID, ushort value, ushort overflow)
    {
        this.defID = defID;
        this.value = value;
        this.overflow = overflow;
    }
    
    public static GasCellValue operator +(GasCellValue self, GasCellValue value)
    {
        self.value += value.value;
        self.overflow += value.overflow;
        return self;
    }
    
    public static GasCellValue operator -(GasCellValue self, GasCellValue value)
    {
        self.value -= value.value;
        self.overflow -= value.overflow;
        return self;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct GasCellStack
{
    public GasCellValue[] stack;

    public GasCellValue this[SpreadingGasTypeDef def] => stack[def.IDReference]; 
    public GasCellValue this[int idx]
    {
        get => stack[idx];
        set => stack[idx] = value;
    }

    public GasCellStack()
    {
        var allDefs = DefDatabase<SpreadingGasTypeDef>.AllDefsListForReading;
        stack = new GasCellValue[allDefs.Count];
        for (var i = 0; i < allDefs.Count; i++)
        {
            var def = allDefs[i];
            stack[i] = new GasCellValue();
        }
    }
    
    //Numerically
    public static GasCellStack operator +(GasCellStack stack, (SpreadingGasTypeDef def, ushort val) value)
    {
        stack[value.def.IDReference] += new GasCellValue((ushort)value.def.IDReference, value.val);
        return stack;
    }
    
    public static GasCellStack operator -(GasCellStack stack, (SpreadingGasTypeDef def, ushort val) value)
    {
        stack[value.def.IDReference] -= new GasCellValue((ushort)value.def.IDReference, value.val);
        return stack;
    }

    //CellValue
    public static GasCellStack operator +(GasCellStack stack, GasCellValue value)
    {
        stack[value.defID] += value;
        return stack;
    }
    
    public static GasCellStack operator -(GasCellStack stack, GasCellValue value)
    {
        stack[value.defID] -= value;
        return stack;
    }
    
    //Stacks
    public static GasCellStack operator +(GasCellStack stack, GasCellStack value)
    {
        for (int i = 0; i < stack.stack.Length; i++)
        {
            stack[i] += value[i];
        }
        return stack;
    }
    
    public static GasCellStack operator -(GasCellStack stack, GasCellStack value)
    {
        for (int i = 0; i < stack.stack.Length; i++)
        {
            stack[i] -= value[i];
        }
        return stack;
    }
}

public class SpreadingGasGrid : MapInformation
{
    [Unsaved]
    private static SpreadingGasGrid _selfRef;
    
    //
    private SpreadingGasLayer[] layerStack;
    private SpreadingGasRenderer[] renderStack;
    
    //
    private GasCellStack[] gasGrid;

    public SpreadingGasGrid(Map map) : base(map)
    {
        _selfRef = this;
        var allDefs = DefDatabase<SpreadingGasTypeDef>.AllDefsListForReading;
        layerStack = new SpreadingGasLayer[allDefs.Count];
        renderStack = new SpreadingGasRenderer[allDefs.Count];
        
        for (int i = 0; i < allDefs.Count; i++)
        {
            var def = allDefs[i];
            layerStack[def.IDReference] = new SpreadingGasLayer(def, map);
            renderStack[i] = new SpreadingGasRenderer(layerStack[def.IDReference], map);
        }
    }
    
    public SpreadingGasLayer[] Layers => layerStack;

    public override void ExposeData()
    {
        base.ExposeData();
    }

    public override void Tick()
    {
        for (var l = 0; l < layerStack.Length; l++)
        {
            var gasLayer = layerStack[l];
            gasLayer.LayerTick();
        }
    }

    public override void Update()
    {
        for (var l = 0; l < renderStack.Length; l++)
        {
            renderStack[l].Draw();
        }
    }

    public IEnumerable<(SpreadingGasTypeDef gasType, ushort density, ushort overflow)> AllGassesAt(IntVec3 cell)
    {
        var index = CellIndicesUtility.CellToIndex(cell, map.Size.x);;
        foreach (var gasLayer in layerStack)
        {
            if (gasLayer.HasAnyGas)
            {
                yield return (gasLayer.GasType, gasLayer.DensityAt(index), gasLayer.OverflowAt(index));
            }
        }
    }

    public ushort GasDensityAt(IntVec3 cell, SpreadingGasTypeDef gasType)
    {
        return layerStack[gasType.IDReference].DensityAt(cell.Index(map));
    }
    
    public Color ColorAt(IntVec3 cell)
    {
        Color color = Color.white;
        for (var l = 0; l < layerStack.Length; l++)
        {
            color = layerStack[l].ColorAt(cell);
        }
        return color;
    }

    //
    public static void TryAddGasAt(IntVec3 cell, SpreadingGasTypeDef gasType, ushort amount)
    {
        _selfRef.TryAddGasAt_Internal(cell, gasType, amount);
    }

    private void TryAddGasAt_Internal(IntVec3 cell, SpreadingGasTypeDef gasType, ushort amount)
    {
        layerStack[gasType.IDReference].TryAddGasAt(cell, amount);   
    }

    public bool AnyGasAt(IntVec3 cell)
    {
        return layerStack.Any(s => s.AnyGasAt(cell));
    }

    internal void Debug_FillAll()
    {
        foreach (var gasLayer in layerStack)
        {
            for (int i = 0; i < map.Area; i++)
            {
                gasLayer.TryAddGasAt(map.cellIndices.IndexToCell(i), gasLayer.GasType.maxDensityPerCell);
            }
        }
    }

    internal void Debug_AddAllAt(IntVec3 cell)
    {
        foreach (var gasLayer in layerStack)
        {
            gasLayer.TryAddGasAt(cell, gasLayer.GasType.maxDensityPerCell);
        }
    }

    public void Debug_PushTypeRadial(IntVec3 cell, SpreadingGasTypeDef def)
    {
        foreach (var subCell in GenRadial.RadialCellsAround(cell, 6, true))
        {
            layerStack[def.IDReference].TryAddGasAt(subCell, def.maxDensityPerCell, true);
        }
    }
}
