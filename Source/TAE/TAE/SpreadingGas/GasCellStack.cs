using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;

namespace TAE;

[StructLayout(LayoutKind.Sequential)]
public struct GasCellStack
{
    public GasCellValue[] stack;
    private uint totalValue;
    
    public GasCellValue this[SpreadingGasTypeDef def] => stack[def.IDReference];

    public bool HasAnyGas => totalValue > 0;

    public static GasCellStack Max => new GasCellStack()
    {
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

    public static implicit operator GasCellValue[](GasCellStack stack) => stack.stack;
    
    public GasCellValue this[int idx]
    {
        get => stack[idx];
        set => stack[idx] = value;
    }

    public GasCellStack()
    {
        var allDefs = DefDatabase<SpreadingGasTypeDef>.AllDefsListForReading;
        stack = new GasCellValue[allDefs.Count];
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
        stack.ChangedValueOf(value.def, value.val);
        return stack;
    }

    public static GasCellStack operator -(GasCellStack stack, (SpreadingGasTypeDef def, ushort val) value)
    {
        stack[value.def.IDReference] -= new GasCellValue((ushort)value.def.IDReference, value.val);
        stack.ChangedValueOf(value.def, -value.val);
        return stack;
    }

    //CellValue
    public static GasCellStack operator +(GasCellStack stack, GasCellValue value)
    {
        stack[value.defID] += value;
        stack.ChangedValueOf(value.defID, value.value);
        return stack;
    }

    public static GasCellStack operator -(GasCellStack stack, GasCellValue value)
    {
        stack[value.defID] -= value;
        stack.ChangedValueOf(value.defID, -value.value);
        return stack;
    }

    //Stacks
    public static GasCellStack operator +(GasCellStack stack, GasCellStack value)
    {
        for (int i = 0; i < stack.stack.Length; i++)
        {
            stack[i] += value[i];
            stack.ChangedValueOf(stack[i].defID, value[i].value);
        }

        return stack;
    }

    public static GasCellStack operator -(GasCellStack stack, GasCellStack value)
    {
        for (int i = 0; i < stack.stack.Length; i++)
        {
            stack[i] -= value[i];
            stack.ChangedValueOf(stack[i].defID, -value[i].value);
        }
        
        return stack;
    }
    
    internal void ChangedValueOf(ushort defID, int diff)
    {
        totalValue = (uint)(totalValue + diff);
    }

    internal void ChangedValueOf(ushort defID, ushort previousValue, ushort newValue)
    {
        totalValue = (uint)(totalValue + (previousValue - newValue));
    }
}