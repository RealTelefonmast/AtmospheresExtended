﻿using System;
using System.Runtime.CompilerServices;
using TAC.Atmosphere.Rooms;
using TeleCore;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Verse;

namespace TAC;

//TODO: Make rule based sprading and dissipation: ie: if a gas cell is surrounded by gas of the same type, it spreads less than a cell with free neighbours => viscosity simulation
//TODO: NEW Fluid dynamics sim reference: https://cg.informatik.uni-freiburg.de/intern/seminar/gridFluids_fluid-EulerParticle.pdf
public unsafe class SpreadingGasGrid : MapInformation
{
    internal static SpreadingGasTypeDef[] GasDefsArr;
    internal static int GasDefsCount;
    internal const int AlphaCurvePoints = 8;
    internal const int AlphaCurvePointsData = AlphaCurvePoints + 1;
    
    //
    private SpreadingGasGridRenderer renderer;
    private DynamicAtmosphericDataMapInfo _mapInfo;
    
    //Map Data
    private int gridSize;

    //
    public readonly Color[] minColors;
    public readonly Color[] maxColors;
    public readonly uint[] maxDensities;
    
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
    private readonly byte[] randomSpreadDirs;
    //private readonly IntVec3[] randomSpreadCells;

    //
    public int Length => gasGridData.Length;
    
    //Value Tracking
    public int[] TotalSubGasCount => totalSubGasCount;
    public int[] TotalSubGasValue => totalSubGasValue;
    public uint TotalGasCount => totalGasCount;
    public long TotalGasValue => totalGasValue;
    
    private DynamicAtmosphericDataMapInfo MapInfo => _mapInfo ??= Map.GetMapInfo<DynamicAtmosphericDataMapInfo>();
    
    //
    public bool HasAnyGas => totalGasCount > 0;
    
    //
    public SpreadingGasGrid(Map map) : base(map)
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
        maxDensities = new uint[GasDefsCount];
        
        for (int i = 0; i < GasDefsCount; i++)
        {
            minColors[i] = GasDefsArr[i].colorMin;
            maxColors[i] = GasDefsArr[i].colorMax;
            maxDensities[i] = (uint)GasDefsArr[i].maxDensityPerCell;
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
        randomSpreadDirs = new byte[] {0, 1, 2, 3};
        randomSpreadDirs.Shuffle();
        //randomSpreadCells = GenAdj.CardinalDirections.ToArray();
        //randomSpreadCells.Shuffle();
        
        //
        workingCellCount = 128;//Mathf.CeilToInt(map.Area * 0.015625f);
        //spreadCellCount = Mathf.CeilToInt(map.Area * 0.03125f);
    }
    
    public override void ExposeDataExtra()
    {
    }

    #region Public Safe Accessors

    public float DensityPercentAt(int index, int defID)
    {
        return (float)DensityAt(index,defID) / ((SpreadingGasTypeDef)defID).maxDensityPerCell;
    }
    
    private ushort DensityAt(int index, int defID)
    {
        return gasGridData[index][defID].value;
    }
    
    public GasCellStack CellStackAt(int index)
    {
        return gasGridData[index];
    }
    
    public ushort OverflowAt(int index, int defID)
    {
        return gasGridData[index][defID].overflow;
    }
    
    public bool AnyGasAt(IntVec3 cell)
    {
        return gasGridData[cell.Index(map)].HasAnyGas;
    }
    
    public bool AnyGasAt(int index)
    {
        return gasGridData[index].HasAnyGas;
    }
    
    /// <summary>
    /// Public accessor to spawn gas.
    /// </summary>
    public void Notify_SpawnGasAt(IntVec3 cell, SpreadingGasTypeDef gasType, float value)
    {
        TryAddGasAt_Internal(cell, gasType, (ushort)value);
    }

    #endregion
    
    //CellStack / Value
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal GasCellStack CellStackAtUnsafe(int index)
    {
        return gasGridPtr[index];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal GasCellValue CellValueAtUnsafe(int index, int defID)
    {
        return gasGridPtr[index].stackPtr[defID];
    }
    
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    internal ushort DensityAtUnsafe(int index, int defID)
    {
        return gasGridPtr[index][defID].value;
    }
    
    internal void AddDensities(uint* densities, uint startIndex)
    {
        for (int id = 0; id < GasDefsCount; id++)
        {
            densities[startIndex * GasDefsCount + id] = gasGridPtr[startIndex][id].value; /// GasDefsArr[id].maxDensityPerCell;
        }
    }

    //
    internal bool AnyGasAtUnsafe(IntVec3 cell)
    {
        return gasGridPtr[cell.Index(map)].HasAnyGas;
    }
    
    internal bool AnyGasAtUnsafe(uint index)
    {
        return gasGridPtr[index].HasAnyGas;
    }

    private ushort TypeDensityAt(IntVec3 cell, SpreadingGasTypeDef gasType)
    {
        return gasGridPtr[cell.Index(map)][gasType].value;
    }

    private void SetDensity_Direct(int index, int defID, ushort value)
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

    #region Ticking

    public override void Tick()
    {
        //
    }

    public override void TeleTick()
    {
        if (!HasAnyGas) return;

        var area = map.Area;
        var cellsInRandomOrder = map.cellsInRandomOrder.GetAll();

        for (var i = 0; i < workingCellCount; ++i)
        {
            if (workingIndex >= area)
                workingIndex = 0;

            var cell = cellsInRandomOrder[workingIndex];

            for (var id = 0; id < GasDefsCount; ++id)
            {
                TrySpreadGas(cell, id);
                TryDissipate(cell.Index(map), cell, id);
            }

            workingIndex++;
        }
    }

    #endregion

    private void TrySpreadGas(IntVec3 pos, int defID)
    {
        var index = pos.Index(map);
        var def = (SpreadingGasTypeDef)defID;
        var cellValue = CellValueAtUnsafe(index, defID);

        if (cellValue.overflow > 0)
        {
            var extra = (ushort)Mathf.Clamp(cellValue.overflow, 0, def.maxDensityPerCell);
            cellValue.value += extra;
            cellValue.overflow -= extra;
        }

        //
        if (cellValue.value == 0) return;
        if (cellValue.value < def.minSpreadDensity) return;

        //randomSpreadCells.Shuffle();
        for (var i = 0; i < randomSpreadDirs.Length; i++)
        {
            var offset = IndexOffset(index, randomSpreadDirs[i]); // pos + randomSpreadCells[i];
            if (!CanSpreadTo(offset, def, out var passPct)) continue;

            var cellValueNghb = CellValueAtUnsafe(offset, defID);

        
            if (TryEqualizeWith(ref cellValue, ref cellValueNghb, def, passPct))
            {
                SetCellValueAt(index, cellValue);
                SetCellValueAt(offset, cellValueNghb);
            }
        }
    }
    
    private void TryDissipate(int index, IntVec3 cell, int defID)
    {
        //No gas at index, return
        if (index < 0 || index >= Length || defID >= GasDefsCount)
        {
            TLog.Warning($"Index for gasGrid cell is out of bound: {index} | {defID}");
            return;
        }
        
        var cellValue = gasGridPtr[index][defID];
        if (cellValue.TotalBitVal == 0) return;
        //if (cellValue.value == 0) return;
        var def = ((SpreadingGasTypeDef)defID);

        //Dissipate Into Room
        if (((SpreadingGasTypeDef) defID).roofBlocksDissipation && cell.Roofed(map))
        {
            if (def.dissipateTo != null)
            {
                var room = cell.GetRoomFast(map);
                var roomComp = room.GetRoomComp<RoomComponent_Atmosphere>();
                //TODO: Add dissipation
                // if (room is {ProperRoom: true} && roomComp.Notify_SpradingGasDissipating(def, def.dissipationAmount, out var actual))
                // {
                //     SetDensity_Direct(index, defID, (ushort)Math.Max(cellValue.value - actual.ActualAmount, 0));
                // }
            }
            return;
        }

        cellValue.value = (ushort)Math.Max(cellValue.value - def.dissipationAmount, 0);
        SetDensity_Direct(index, defID, cellValue.value);
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
        
        //TODO: Viscosity needs to be directly settable, not a hardcoded value like 0.35
        var diffShort = (ushort)(Mathf.Abs(diff * passPct) * 0.35f * def.ViscosityMultiplier);
        
        gasCellA -= diffShort;
        gasCellB += diffShort;
        return true;
    }
    
    private static void AdjustSaturation(ref GasCellValue cellValue, SpreadingGasTypeDef def, int value, out int actualValue)
    {
        actualValue = value;
        var val = cellValue.value + value;
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
    private bool CanSpreadToFast(IntVec3 cell, SpreadingGasTypeDef def)
    {
        if (gasGridPtr[cell.Index(map)][def].value >= def.maxDensityPerCell) return false;
        return MapInfo.AtmosphericPassGrid[cell] > 0;
    }
    
    private bool CanSpreadTo(int otherIndex, SpreadingGasTypeDef forDef, out float passPct)
    {
        passPct = 0f;
        if (OutOfBounds(otherIndex)) return false;
        if (gasGridPtr[otherIndex][forDef].value >= forDef.maxDensityPerCell) return false;
        passPct = MapInfo.AtmosphericPassGrid[otherIndex]; // DynamicDataCacheInfo forDef.TransferWorker.GetBaseTransferRate(other.GetFirstBuilding(map));
        return passPct > 0;
    }
    
    //
    public override void Update()
    {
        //renderer.Draw();
    }

    public override void UpdateOnGUI()
    {
        //AtmosphereUtility.DrawSpreadingGasAroundMouse();
        AtmosphericUtility.DrawPassPercentCellsGUI();
    }

    public override void TeleUpdate()
    {
        renderer.Draw();
        AtmosphericUtility.DrawPassPercentCells();
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

    internal void Debug_PushTypeRadial(IntVec3 root, SpreadingGasTypeDef def)
    {
        foreach (var subCell in GenRadial.RadialCellsAround(root, 6, true))
        {
            TryAddGasAt_Internal(subCell, def, (ushort)def.maxDensityPerCell, true);
        }
    }

    internal void Debug_PushRadialAdjacent(IntVec3 root, SpreadingGasTypeDef def)
    {
        AdjacentCellFiller.FillAdjacentCellsAround(root, map, 128, vec3 =>
        {
            TryAddGasAt_Internal(vec3, def, (ushort)def.maxDensityPerCell, true);
        }, vec3 => CanSpreadToFast(vec3, def), vec3 => CellValueAtUnsafe(vec3.Index(map), def).value > 0);
    }

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

    internal void Notify_ThingSpawned(Thing thing)
    {
        var ind = thing.Position.Index(map);
        switch (thing.def.Fillage)
        {
            case FillCategory.Full:
                SetCellStackAt(ind, new GasCellStack());
                return;
            case FillCategory.Partial:
            {
                for (var i = 0; i < GasDefsArr.Length; i++)
                {
                    var def = GasDefsArr[i];
                    var value = gasGridPtr[ind][def];
                    AdjustSaturation(ref value, def, (int)(-(float)(value.value + value.overflow) * thing.def.fillPercent), out _);
                    SetCellValueAt(ind, value);
                }
                break;
            }
        }
    }
}
