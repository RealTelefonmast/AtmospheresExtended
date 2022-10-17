using System;
using System.Runtime.InteropServices;

namespace TAE;

[StructLayout(LayoutKind.Explicit, Size = 48)]
public unsafe struct GasCellValue
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

    public static GasCellValue operator +(GasCellValue self, ushort value)
    {
        self.value += value;
        return self;
    }
    
    public static GasCellValue operator -(GasCellValue self, ushort value)
    {
        self.value -= value;
        return self;
    }
    
    //
    public static bool operator ==(GasCellValue self, int value)
    {
        return self.totalBitVal == value;
    }
    
    public static bool operator !=(GasCellValue self, int value)
    {
        return self.totalBitVal != value;
    }

    public override string ToString()
    {
        return $"[{(SpreadingGasTypeDef)defID}]: ({value}, {overflow})";
    }
}