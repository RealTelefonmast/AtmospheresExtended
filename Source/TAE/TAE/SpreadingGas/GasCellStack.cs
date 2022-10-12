using System.Runtime.InteropServices;
using Verse;

namespace TAE;

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