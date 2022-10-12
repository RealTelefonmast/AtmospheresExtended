using System;
using System.Linq;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE;

public class SpreadingGasLayer : IExposable
{
    private readonly Map map;
    private readonly SpreadingGasTypeDef gasType;
    
    //Total [16Bits| ActualDensity][16Bits| Overflow]
    private readonly uint[] totalGasGrid;
    private int totalGasCount;
    private int totalGasValue;

    private int dissipationIndex = 0;
    private int spreadIndex = 0;

    //Helper Data
    private static readonly FloatRange AlphaRange = new FloatRange(0.2f, 0.8f);
    private readonly IntVec3[] randomSpreadCells;
    
    public SpreadingGasLayer(SpreadingGasTypeDef gasType, Map map)
    {
        this.map = map;
        this.gasType = gasType;
        totalGasGrid = new uint[map.cellIndices.NumGridCells];
        
        //
        randomSpreadCells = GenAdj.CardinalDirections.ToArray();
    }

    public bool HasAnyGas => totalGasCount > 0;
    public int TotalGasCount => totalGasCount;
    public int TotalValue => totalGasValue;
    public uint[] Grid => totalGasGrid;
    
    public SpreadingGasTypeDef GasType => gasType;

    public void ExposeData()
    {
        TeleExposeUtility.ExposeUInt(map, (c) => totalGasGrid[map.cellIndices.CellToIndex(c)], delegate(IntVec3 c, uint val)
        {
            totalGasGrid[map.cellIndices.CellToIndex(c)] = val;
        }, "totalGasGrid");
        Scribe_Values.Look(ref spreadIndex, nameof(spreadIndex), 0, false);
        Scribe_Values.Look(ref dissipationIndex, nameof(dissipationIndex), 0, false);
    }

    public Color ColorAt(IntVec3 cell)
    {
        int index = CellIndicesUtility.CellToIndex(cell, map.Size.x);
        float density = DensityAt(index);
        var lerpVal = density / gasType.maxDensityPerCell;
        Color result = Color.Lerp(gasType.colorMin, gasType.colorMax, lerpVal);
        result.a = AlphaRange.LerpThroughRange(lerpVal);
        return result;
    }

    public override string ToString()
    {
        
        return base.ToString();
    }

    public (ushort density, ushort overflow) CellDataAt(int index)
    {
        return (DensityAt(index), OverflowAt(index));
    }

    public void SetCellData(int index, (ushort, ushort) data)
    {
        //TLog.Message($"Setting cellData at {index}: {data}");
        SetDensity(index, data.Item1);
        SetOverflow(index, data.Item2);
    }
    
    public ushort DensityAt(int index)
    {
        return (ushort) (totalGasGrid[index] >> 16);
    }
    
    public ushort OverflowAt(int index)
    {
        return (ushort) (totalGasGrid[index] & 0xFFFF);
    }

    public float PercentAt(int index)
    {
        return (float)DensityAt(index) / gasType.maxDensityPerCell;
    }
    
    //Override upper 16 bits
    //XXXX XXXX XXXX XXXX 0000 0000 0000 0000
    public void SetDensity(int index, ushort value)
    {
        //TLog.Message($"Setting density at {index}: {value}");
        var previous = DensityAt(index);
        totalGasGrid[index] = ((uint) value << 16) | (totalGasGrid[index] & 0xFFFF);
        
        //
        if (value > previous)
        {
            totalGasValue += value - previous;
        }
        else if (value < previous)
        {
            totalGasValue -= previous - value;
        }
        
        if (value > 0 && previous <= 0)
        {
            totalGasCount++;
        }
        
        else if (value <= 0 && previous > 0)
        {
            totalGasCount--;
        }
    }

    private string ColorBytes(uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        var string1 = $"{Convert.ToString(bytes[0], 2)}-{Convert.ToString(bytes[1], 2)}".ColorizeFix(Color.cyan);
        var string2 = $"{Convert.ToString(bytes[2], 2)}-{Convert.ToString(bytes[3], 2)}".ColorizeFix(Color.magenta);

        return $"{string1};{string2}";
    }
    
    //Override lower 16 bits
    // 0000 0000 0000 0000 XXXX XXXX XXXX XXXX
    public void SetOverflow(int index, ushort value)
    {
        totalGasGrid[index] &= 0xFFFF0000;
        totalGasGrid[index] |= value;
    }
    
    //
    public bool AnyGasAt(IntVec3 cell)
    {
        return AnyGasAt(CellIndicesUtility.CellToIndex(cell, map.Size.x));
    }
    
    public bool AnyGasAt(int idx)
    {
        return totalGasGrid[idx] > 0;
    }
    
    public void LayerTick()
    {
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
            
            Dissipate(CellIndicesUtility.CellToIndex(cell, map.Size.x));
            dissipationIndex++;
        }

        num = Mathf.CeilToInt(area * 0.03125f);
        for (int j = 0; j < num; j++)
        {
            if (spreadIndex >= area)
                spreadIndex = 0;
            
            TrySpreadGas(cellsInRandomOrder[spreadIndex]);
            spreadIndex++;
        }
    }
    
    private void Dissipate(int index)
    {
        //No gas at index, return
        if (totalGasGrid[index] == 0) return;
        
        ushort densityValue = DensityAt(index);
        if (densityValue == 0) return;
        if (densityValue < gasType.minDissipationDensity) return;
        
        densityValue = (ushort)Math.Max(densityValue - gasType.dissipationAmount, 0);;
        SetDensity(index, densityValue);
    }

    private void TrySpreadGas(IntVec3 pos)
    {
        int index = CellIndicesUtility.CellToIndex(pos, map.Size.x);
        var densityValue = DensityAt(index);
        var overflowValue = OverflowAt(index);
        
        //
        if (densityValue == 0) return;
        if (densityValue < gasType.minSpreadDensity) return;
        
        var gasCellA = (densityValue, overflowValue);
        randomSpreadCells.Shuffle();
        for (int i = 0; i < randomSpreadCells.Length; i++)
        {
            var cell = pos + randomSpreadCells[i];
            if (!CanSpreadTo(cell, out float passPct)) continue;

            int newIndex = CellIndicesUtility.CellToIndex(cell, map.Size.x);
            var densityValueNghb = DensityAt(newIndex);
            var overflowValueNghb = OverflowAt(newIndex);
            
            var gasCellB = (densityValueNghb, overflowValueNghb);
            if (TryEqualizeWith(ref gasCellA, ref gasCellB, passPct))
            {
                SetCellData(index, gasCellA);
                SetCellData(newIndex, gasCellB);
            }
        }
    }

    public bool TryEqualizeWith(ref (ushort density, ushort overflow) gasCellA, ref (ushort density, ushort overflow) gasCellB, float passPct)
    {
        var diff = (gasCellA.density - gasCellB.density) / 4;
        diff = (int)(diff / (gasType.spreadViscosity * diff));
        if (diff <= 0) return false;
        
       // var value = (ushort)Math.Min((diff * 0.5f * passPct), );
        ushort value = (ushort)(Mathf.Abs(diff * passPct) / 2);
        AdjustSaturation(ref gasCellA, -value, out int changedValue);
        AdjustSaturation(ref gasCellB, -changedValue, out _);
        return true;
    }

    public void AdjustSaturation(ref (ushort,ushort) gasCellValue, int value, out int actualValue)
    {
        actualValue = value;
        var val = gasCellValue.Item1 + value;
        if (gasCellValue.Item2 > 0 && val < gasType.maxDensityPerCell)
        {
            var extra = (ushort)Mathf.Clamp(gasType.maxDensityPerCell - val, 0, gasCellValue.Item2);
            val += extra;
            gasCellValue.Item2 -= extra;
        }
        gasCellValue.Item1 = (ushort)Mathf.Clamp(val, 0, gasType.maxDensityPerCell);
        if (val < 0)
        {
            actualValue = value + val;
            return;
        }

        if (val < gasType.maxDensityPerCell) return;
        var overFlow = val - gasType.maxDensityPerCell;
        actualValue = value - overFlow;
        gasCellValue.Item2 = (ushort)(gasCellValue.Item2 + overFlow);
    }

    public void TryAddGasAt(IntVec3 cell, int value, bool noOverflow = false)
    {
        if (!CanSpreadTo(cell, out _)) return;
        
        int index = CellIndicesUtility.CellToIndex(cell, this.map.Size.x);
        var cellData = CellDataAt(index);
        AdjustSaturation(ref cellData, value, out _);

        if (noOverflow)
            cellData.overflow = 0;
        
        SetCellData(index, cellData);
    }

    //Helper
    private bool CanSpreadTo(IntVec3 other, out float passPct)
    {
        passPct = 0f;
        if (!other.InBounds(map)) return false;
        passPct = gasType.TransferWorker.GetBaseTransferRate(other.GetFirstBuilding(map));
        return passPct > 0;
    }
}