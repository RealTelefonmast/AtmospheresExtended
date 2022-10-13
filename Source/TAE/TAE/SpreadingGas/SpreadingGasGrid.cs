using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using RimWorld;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE;

public class SpreadingGasGrid : MapInformation
{
    [Unsaved]
    private static SpreadingGasGrid _selfRef;
    
    //
    private SpreadingGasGridRenderer renderer;
    
    //
    private int gasTypeCount;
    private GasCellStack[] gasGrid;
    private int[] totalSubGasCount;
    private int[] totalSubGasValue;

    private int totalGasCount;
    private long totalGasValue;
    
    private int dissipationIndex = 0;
    private int spreadIndex = 0;
    
    private readonly IntVec3[] randomSpreadCells;

    public bool HasAnyGas => totalGasCount > 0;
    
    public SpreadingGasGrid(Map map) : base(map)
    {
        _selfRef = this;
        var allDefs = DefDatabase<SpreadingGasTypeDef>.AllDefsListForReading;
        gasTypeCount = allDefs.Count;
        
        renderer = new SpreadingGasGridRenderer();
        
        gasGrid = new GasCellStack[map.cellIndices.NumGridCells];
        totalSubGasCount = new int[gasTypeCount];
        totalSubGasValue = new int[gasTypeCount];
        
        //
        randomSpreadCells = GenAdj.CardinalDirections.ToArray();
        
        //
        /*
        layerStack = new SpreadingGasLayer[allDefs.Count];
        renderStack = new SpreadingGasRenderer[allDefs.Count];
        
        for (int i = 0; i < allDefs.Count; i++)
        {
            var def = allDefs[i];
            layerStack[def.IDReference] = new SpreadingGasLayer(def, map);
            renderStack[i] = new SpreadingGasRenderer(layerStack[def.IDReference], map);
        }
        */
    }
    
    public override void ExposeData()
    {
        base.ExposeData();
    }
    
    //CellStack / Value
    public GasCellStack CellStackAt(int index)
    {
        return gasGrid[index];
    }
    
    public GasCellValue CellValueAt(int index, int defID)
    {
        return gasGrid[index][defID];
    }
    
    public void SetCellValueAt(int index, GasCellValue value)
    {
        gasGrid[index][value.defID] = value;
    }

    //Access Helpers
    public ushort DensityAt(int index, int defID)
    {
        return gasGrid[index][defID].value;
    }
    
    public ushort OverflowAt(int index, int defID)
    {
        return gasGrid[index][defID].overflow;
    }

    public float DensityPercentAt(int index, int defID)
    {
        return (float)DensityAt(index,defID) / ((SpreadingGasTypeDef)defID).maxDensityPerCell;
    }
    
    //
    public bool AnyGasAt(IntVec3 cell)
    {
        return gasGrid[cell.Index(map)].HasAnyGas;
    }

    public ushort TypeDensityAt(IntVec3 cell, SpreadingGasTypeDef gasType)
    {
        return gasGrid[cell.Index(map)][gasType].value;
    }
    
    public Color ColorAt(IntVec3 cell)
    {
        return gasGrid[cell.Index(map)].Color;
    }

    public void SetDensity_Direct(int index, int defID, ushort value)
    {
        var previous = DensityAt(index, defID);
        var cellValue = gasGrid[index][defID];
        
        cellValue.value = value;

        gasGrid[index][defID] = cellValue;
        
        //
        if (value > previous)
        {
            totalSubGasCount[defID] += value - previous;
        }
        else if (value < previous)
        {
            totalSubGasCount[defID] -= previous - value;
        }
        
        if (value > 0 && previous <= 0)
        {
            totalSubGasCount[defID]++;
        }
        
        else if (value <= 0 && previous > 0)
        {
            totalSubGasCount[defID]--;
        }
    }
    
    public void SetOverflow_Direct(int index, int defID, ushort value)
    {
        var cellValue = gasGrid[index][defID];
        cellValue.overflow = value;
        gasGrid[index][defID] = cellValue;
    }
    
    public override void Tick()
    {
        /*
        for (var l = 0; l < layerStack.Length; l++)
        {
            var gasLayer = layerStack[l];
            gasLayer.LayerTick();
        }
        */
        
        //
        if (!HasAnyGas) return;
        int area = map.Area;
        int num = Mathf.CeilToInt(area * 0.015625f);
        var cellsInRandomOrder = map.cellsInRandomOrder.GetAll();

        for (int i = 0; i < num; i++)
        {
            if (dissipationIndex >= area) 
                dissipationIndex = 0;
            
            var cell = cellsInRandomOrder[dissipationIndex];
            
            //if(gasType.roofBlocksDissipation && cell.Roofed(map)) continue;
            
            for (int id = 0; id <= gasTypeCount; id++)
            {
                Dissipate(CellIndicesUtility.CellToIndex(cell, map.Size.x), id);
            }

            dissipationIndex++;
        }

        num = Mathf.CeilToInt(area * 0.03125f);
        for (int j = 0; j < num; j++)
        {
            if (spreadIndex >= area)
                spreadIndex = 0;

            for (int id = 0; id <= gasTypeCount; id++)
            {
                TrySpreadGas(cellsInRandomOrder[spreadIndex], id);
            }

            spreadIndex++;
        }
    }

    void Dissipate(int index, int defID)
    {
        //No gas at index, return
        if (gasGrid[index][defID] == 0) return;
        var def = ((SpreadingGasTypeDef)defID);
        //if (totalGasGrid[index] == 0) return;

        ushort densityValue = DensityAt(index, defID);
        if (densityValue == 0) return;
        if (densityValue < def.minDissipationDensity) return;
        
        densityValue = (ushort)Math.Max(densityValue - def.dissipationAmount, 0);
        
        SetDensity(index, defID, densityValue);
    }
    
    void TrySpreadGas(IntVec3 pos, int defID)
    {
        var index = CellIndicesUtility.CellToIndex(pos, map.Size.x);
        var def = ((SpreadingGasTypeDef)defID);
        var cellValue = CellValueAt(index, defID);

        //
        if (cellValue.value == 0) return;
        if (cellValue.value < def.minSpreadDensity) return;
        
        randomSpreadCells.Shuffle();
        for (int i = 0; i < randomSpreadCells.Length; i++)
        {
            var cell = pos + randomSpreadCells[i];
            if (!CanSpreadTo(cell, def, out float passPct)) continue;

            int newIndex = CellIndicesUtility.CellToIndex(cell, map.Size.x);
            var cellValueNghb = CellValueAt(index, defID);
            
            if (TryEqualizeWith(ref cellValue, ref cellValueNghb, def, passPct))
            {
                SetCellValueAt(index, cellValue);
                SetCellValueAt(newIndex, cellValueNghb);
            }
        }
    }
    
    public bool TryEqualizeWith(ref GasCellValue gasCellA, ref GasCellValue gasCellB, SpreadingGasTypeDef def, float passPct)
    {
        var diff = (gasCellA.value - gasCellB.value) / 4;
        diff = (int)(diff / (def.spreadViscosity * diff));
        if (diff <= 0) return false;
        
        // var value = (ushort)Math.Min((diff * 0.5f * passPct), );
        ushort value = (ushort)(Mathf.Abs(diff * passPct) / 2);
        AdjustSaturation(ref gasCellA, def, -value, out int changedValue);
        AdjustSaturation(ref gasCellB, def, -changedValue, out _);
        return true;
    }
    
    public void AdjustSaturation(ref GasCellValue cellValue, SpreadingGasTypeDef def, int value, out int actualValue)
    {
        actualValue = value;
        var val = cellValue.value + value;
        if (cellValue.overflow > 0 && val < def.maxDensityPerCell)
        {
            var extra = (ushort) Mathf.Clamp(def.maxDensityPerCell - val, 0, cellValue.overflow);
            val += extra;
            cellValue.overflow -= extra;
        }
        cellValue.value = (ushort)Mathf.Clamp(val, 0, def.maxDensityPerCell);
        if (val < 0)
        {
            actualValue = value + val;
            return;
        }

        if (val < def.maxDensityPerCell) return;
        var overFlow = val - def.maxDensityPerCell;
        actualValue = value - overFlow;
        cellValue.overflow = (ushort)(cellValue.overflow + overFlow);
    }
    
    //
    private bool CanSpreadTo(IntVec3 other, SpreadingGasTypeDef forDef, out float passPct)
    {
        passPct = 0f;
        if (!other.InBounds(map)) return false;
        passPct = forDef.TransferWorker.GetBaseTransferRate(other.GetFirstBuilding(map));
        return passPct > 0;
    }
    
    //
    public override void Update()
    {
        renderer.Draw();
    }

    //Debug Options
    internal void Debug_FillAll()
    {
        for (int i = 0; i < map.Area; i++)
        {
            gasGrid[i] = GasCellStack.Max;
        }
    }

    internal void Debug_AddAllAt(IntVec3 cell)
    {
        gasGrid[cell.Index(map)] = GasCellStack.Max;
    }

    internal void Debug_PushTypeRadial(IntVec3 cell, SpreadingGasTypeDef def)
    {
        foreach (var subCell in GenRadial.RadialCellsAround(cell, 6, true))
        {
            TryAddGasAt_Internal(cell, def, (ushort)def.maxDensityPerCell, true);
        }
    }
    
    //
    public static void TryAddGasAt(IntVec3 cell, SpreadingGasTypeDef gasType, ushort amount)
    {
        _selfRef.TryAddGasAt_Internal(cell, gasType, amount);
    }
    
    private void TryAddGasAt_Internal(IntVec3 cell, SpreadingGasTypeDef gasType, ushort amount, bool noOverflow = false)
    {
        if (!CanSpreadTo(cell, gasType, out _)) return;
        
        int index = CellIndicesUtility.CellToIndex(cell, map.Size.x);
        var cellValue = gasGrid[gasType][index];
        AdjustSaturation(ref cellValue, gasType, amount, out _);

        if (noOverflow)
            cellValue.overflow = 0;
        
        SetCellValueAt(index, cellValue);
    }
}
