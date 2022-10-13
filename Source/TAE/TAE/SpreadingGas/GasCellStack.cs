using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;

namespace TAE;

[StructLayout(LayoutKind.Sequential)]
public struct GasCellStack
{
    public GasCellValue[] stack;
    private Color colorVal;
    private uint totalValue;
    
    public GasCellValue this[SpreadingGasTypeDef def] => stack[def.IDReference];

    public static implicit operator GasCellValue[](GasCellStack stack) => stack.stack;
    
    public Color Color => colorVal;
    public bool HasAnyGas => totalValue > 0;

    public static GasCellStack Max => new GasCellStack()
    {
        colorVal = Color.white,
        stack = FullStack,
    };

    private static GasCellValue[] FullStack
    {
        get
        {
            var list = DefDatabase<SpreadingGasTypeDef>.AllDefsListForReading;
            var stack = new GasCellValue[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                var gas = list[i];
                stack[i] = new GasCellValue(gas.IDReference, (ushort)gas.maxDensityPerCell);
            }
            return stack;
        }
    }
    
    public GasCellValue this[int idx]
    {
        get => stack[idx];
        set => stack[idx] = value;
    }

    public GasCellStack()
    {
        var allDefs = DefDatabase<SpreadingGasTypeDef>.AllDefsListForReading;
        stack = new GasCellValue[allDefs.Count];
        colorVal = Color.white;
        totalValue = 0;
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

    public void ChangedValueOf(ushort defID, ushort previousValue, ushort newValue)
    {
        totalValue = (uint)(totalValue + (previousValue - newValue));
        
        //
        var def = (SpreadingGasTypeDef)defID;
        var previousColor = Color.Lerp(def.colorMin, def.colorMax,(previousValue/(float)def.maxDensityPerCell));
        var newColor = Color.Lerp(def.colorMin, def.colorMax,(newValue/(float)def.maxDensityPerCell));
        var colorDiff = previousColor - newColor;
        colorVal += colorDiff;
    }

    public void UpdateColor()
    {
        colorVal = new Color(0, 0, 0, 0);
        for (int i = 0; i < stack.Length; i++)
        {
            var value = stack[i];
            var def = (SpreadingGasTypeDef)value.defID;
            colorVal += Color.Lerp(def.colorMin, def.colorMax, value.value/(float)def.maxDensityPerCell);
           
        }
        colorVal /= stack.Length;
    }
}