using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RimWorld;
using TeleCore;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Verse;

namespace TAE;

public unsafe class SpreadingGasGrid : MapInformation
{
    internal static SpreadingGasTypeDef[] GasDefsArr;
    internal static int GasDefsCount;
    
    //
    private SpreadingGasGridRenderer renderer;
    private DynamicDataCacheInfo cacheInfo;
    
    //Map Data
    private int gridSize;

    //
    public readonly Color[] minColors;
    public readonly Color[] maxColors;
    
    //
    private NativeArray<GasCellStack> gasGridData;
    private readonly GasCellStack* gasGridPtr;
    
    //
    private readonly int[] totalSubGasCount;
    private readonly int[] totalSubGasValue;
    private uint totalGasCount;
    private long totalGasValue;
    
    //Spreading And Dissipation
    private int workingIndex = 0;
    private readonly int workingCellCount;
    private readonly int[] randomSpreadDirs;
    //private readonly IntVec3[] randomSpreadCells;

    //
    public NativeArray<GasCellStack> GasGrid => gasGridData;
    public int Length => gasGridData.Length;
    
    //Value Tracking
    public int[] TotalSubGasCount => totalSubGasCount;
    public int[] TotalSubGasValue => totalSubGasValue;
    public uint TotalGasCount => totalGasCount;
    public long TotalGasValue => totalGasValue;
    
    private DynamicDataCacheInfo CacheInfo => cacheInfo ??= Map.GetMapInfo<DynamicDataCacheInfo>();
    
    //
    public bool HasAnyGas => totalGasCount > 0;
    
    //
    public unsafe SpreadingGasGrid(Map map) : base(map)
    {
        gridSize = map.cellIndices.NumGridCells;
        
        //
        if (GasDefsArr == null)
        {
            GasDefsArr = DefDatabase<SpreadingGasTypeDef>.AllDefsListForReading.ToArray();
            GasDefsCount = GasDefsArr.Length;
        }

        minColors = new Color[GasDefsCount];
        maxColors = new Color[GasDefsCount];
        
        for (int i = 0; i < GasDefsCount; i++)
        {
            minColors[i] = GasDefsArr[i].colorMin;
            maxColors[i] = GasDefsArr[i].colorMax;
        }
        
        //
        renderer = new SpreadingGasGridRenderer(this, map);

        gasGridData = new NativeArray<GasCellStack>(gridSize, Allocator.Persistent); // new GasCellStack[gridSize];
        gasGridPtr = (GasCellStack*)gasGridData.GetUnsafePtr();
        
        for (var c = 0; c < gridSize; c++)
        {
            gasGridPtr[c] = new GasCellStack();
        }
        
        totalSubGasCount = new int[GasDefsCount];
        totalSubGasValue = new int[GasDefsCount];
        
        //
        randomSpreadDirs = new[] {0, 1, 2, 3};
        randomSpreadDirs.Shuffle();
        //randomSpreadCells = GenAdj.CardinalDirections.ToArray();
        //randomSpreadCells.Shuffle();
        
        //
        workingCellCount = 128;//Mathf.CeilToInt(map.Area * 0.015625f);
        //spreadCellCount = Mathf.CeilToInt(map.Area * 0.03125f);
    }
    
    public override void ExposeData()
    {
        base.ExposeData();
    }
    
    //CellStack / Value
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GasCellStack CellStackAt(int index)
    {
        return gasGridPtr[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GasCellValue CellValueAt(int index, int defID)
    {
        return gasGridPtr[index].stackPtr[defID];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetCellValueAt(int index, GasCellValue value)
    {
        DetectValueChange(value.defID, gasGridPtr[index][value.defID].value, value.value);
       
        var previousTotal = gasGridPtr[index].totalValue;
       
        //Set Value
        var val = gasGridPtr[index];
        val[value.defID] = value;
        gasGridPtr[index] = val;
        
        DetectCountChange(previousTotal, gasGridPtr[index].totalValue);
    }

    private void SetCellStackAt(int index, GasCellStack value)
    {
        for (var i = 0; i < GasDefsCount; i++)
        {
            SetCellValueAt(index, value[i]);
        }
    }

    //Access Helpers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort DensityAt(int index, int defID)
    {
        return gasGridPtr[index][defID].value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort OverflowAt(int index, int defID)
    {
        return gasGridPtr[index][defID].overflow;
    }

    internal void AddPercentages(float* alphas, int startIndex)
    {
        for (int id = 0; id < GasDefsCount; id++)
        {
            alphas[((startIndex*GasDefsCount)) + id] = (float) gasGridPtr[startIndex][id].value / GasDefsArr[id].maxDensityPerCell;
        }
    }

    public float DensityPercentAt(int index, int defID)
    {
        return (float)DensityAt(index,defID) / ((SpreadingGasTypeDef)defID).maxDensityPerCell;
    }
    
    //
    public bool AnyGasAt(IntVec3 cell)
    {
        return gasGridPtr[cell.Index(map)].HasAnyGas;
    }

    public bool AnyGasAt(int index)
    {
        return gasGridPtr[index].HasAnyGas;
    }
    
    public ushort TypeDensityAt(IntVec3 cell, SpreadingGasTypeDef gasType)
    {
        return gasGridPtr[cell.Index(map)][gasType].value;
    }

    public void SetDensity_Direct(int index, int defID, ushort value)
    {
        var cellValue = gasGridPtr[index][defID];
        cellValue.value = value;
        SetCellValueAt(index, cellValue);
    }

    private void DetectValueChange(int defID, ushort previous, ushort value)
    {
        var calcVal = value - previous;
        totalSubGasValue[defID] += calcVal;
        totalGasValue += calcVal;

        switch (value)
        {
            case > 0 when previous <= 0:
                totalSubGasCount[defID]++;
                break;
            case <= 0 when previous > 0:
                totalSubGasCount[defID]--;
                break;
        }
    }

    private void DetectCountChange(long previousTotal, long newTotal)
    {
        switch (newTotal)
        {
            case > 0 when previousTotal <= 0:
                totalGasCount++;
                break;
            case <= 0 when previousTotal > 0:
                totalGasCount--;
                break;
        }
    }
    
    public void SetOverflow_Direct(int index, int defID, ushort value)
    {
        var cellValue = gasGridPtr[index][defID];
        cellValue.overflow = value;
        SetCellValueAt(index, cellValue);
    }
    
    public override void Tick()
    {
        //
        if (!HasAnyGas) return;

        int area = map.Area;
        var cellsInRandomOrder = map.cellsInRandomOrder.GetAll();
        
        for (int i = 0; i < workingCellCount; ++i)
        {
            if (workingIndex >= area) 
                workingIndex = 0;
            
            var cell = cellsInRandomOrder[workingIndex];

            for (int id = 0; id < GasDefsCount; ++id)
            {
                Dissipate(cell.Index(map), cell , id);
                TrySpreadGas(cell, id);
            }
            workingIndex++;
        }
    }

    void Dissipate(int index, IntVec3 cell, int defID)
    {
        //
        if(((SpreadingGasTypeDef)defID).roofBlocksDissipation && cell.Roofed(map)) return;
        
        //No gas at index, return
        if (index < 0 || index >= Length || defID >= GasDefsCount)
        {
            TLog.Warning($"Index for gasGrid cell is out of bound: {index} | {defID}");
            return;
        } 
        if (gasGridPtr[index][defID].totalBitVal == 0) return;
        var def = ((SpreadingGasTypeDef)defID);
        //if (totalGasGrid[index] == 0) return;

        ushort densityValue = DensityAt(index, defID);
        if (densityValue == 0) return;
        
        //TODO:
        //if (densityValue > def.minDissipationDensity) return;
        
        densityValue = (ushort)Math.Max(densityValue - def.dissipationAmount, 0);
        
        SetDensity_Direct(index, defID, densityValue);
    }
    
    void TrySpreadGas(IntVec3 pos, int defID)
    {
        var index = pos.Index(map);
        var def = ((SpreadingGasTypeDef)defID);
        var cellValue = CellValueAt(index, defID);
        
        //
        if (cellValue.value == 0) return;
        if (cellValue.value < def.minSpreadDensity) return;
        
        //randomSpreadCells.Shuffle();
        for (int i = 0; i < randomSpreadDirs.Length; i++)
        {
            var offset = IndexOffset(index, randomSpreadDirs[i]); // pos + randomSpreadCells[i];
            if (!CanSpreadTo(offset, def, out float passPct)) continue;

            int newIndex = offset;
            var cellValueNghb = CellValueAt(newIndex, defID);
            
            //
            if (TryEqualizeWith(ref cellValue, ref cellValueNghb, def, passPct))
            {
                SetCellValueAt(index, cellValue);
                SetCellValueAt(newIndex, cellValueNghb);
            }
        }
    }

    private bool OutOfBounds(int index)
    {
        return index < 0 || index >= gridSize;
    }

    private int IndexOffset(int index, int direction)
    {
        switch (direction)
        {
            case Rot4.NorthInt:
            {
                index += map.cellIndices.mapSizeX;
                break;
            }
            case Rot4.EastInt:
            {
                index += 1;
                break;
            }   
            case Rot4.SouthInt:
            {
                index -= map.cellIndices.mapSizeX;
                break;
            }    
            case Rot4.WestInt:
            {
                index -= 1;
                break;
            }
        }
        //
        return index;
    }

    private static bool TryEqualizeWith(ref GasCellValue gasCellA, ref GasCellValue gasCellB, SpreadingGasTypeDef def, float passPct)
    {
        //Get the diff pressure between cells, and divide by 4 spreading directions
        float diff = (gasCellA.value - gasCellB.value);
        if (diff <= 0) return false;
        
        var diffShort = (ushort)(Mathf.Abs(diff * passPct) * 0.125f * def.ViscosityMultiplier);
        
        gasCellA -= diffShort;
        gasCellB += diffShort;
        return true;
    }
    
    private static void AdjustSaturation(ref GasCellValue cellValue, SpreadingGasTypeDef def, int value, out int actualValue)
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
    private bool CanSpreadTo(int otherIndex, SpreadingGasTypeDef forDef, out float passPct)
    {
        passPct = 0f;
        if (OutOfBounds(otherIndex)) return false;
        passPct = CacheInfo.AtmosphericPassGrid[otherIndex]; // DynamicDataCacheInfo forDef.TransferWorker.GetBaseTransferRate(other.GetFirstBuilding(map));
        return passPct > 0;
    }
    
    //
    public override void Update()
    {
        //renderer.Draw();
    }

    public override void UpdateOnGUI()
    {
        AtmosphereUtility.DrawSpreadingGasAroundMouse();
    }

    public override void CustomUpdate()
    {
        renderer.Draw();
    }

    //Debug Options
    internal void Debug_FillAll()
    {
        for (int i = 0; i < map.Area; i++)
        {
            SetCellStackAt(i, GasCellStack.Max);
        }
    }

    internal void Debug_AddAllAt(IntVec3 cell)
    {
        SetCellStackAt(cell.Index(map), GasCellStack.Max);
    }

    internal void Debug_PushTypeRadial(IntVec3 cell, SpreadingGasTypeDef def)
    {
        foreach (var subCell in GenRadial.RadialCellsAround(cell, 6, true))
        {
            TryAddGasAt_Internal(subCell, def, (ushort)def.maxDensityPerCell, true);
        }
    }
    
    //
    /*
    public static void TryAddGasAt(IntVec3 cell, SpreadingGasTypeDef gasType, ushort amount)
    {
        _selfRef.TryAddGasAt_Internal(cell, gasType, amount);
    }
    */
    
    private void TryAddGasAt_Internal(IntVec3 cell, SpreadingGasTypeDef gasType, ushort amount, bool noOverflow = false)
    {
        if (!CanSpreadTo(cell.Index(map), gasType, out _)) return;
        
        int index = CellIndicesUtility.CellToIndex(cell, Map.Size.x);
        var cellValue = gasGridPtr[index][gasType];
        AdjustSaturation(ref cellValue, gasType, amount, out _);

        if (noOverflow)
            cellValue.overflow = 0;
        
        SetCellValueAt(index, cellValue);
    }
}
