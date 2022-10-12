using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TAE;

[StructLayout(LayoutKind.Sequential)]
struct GasMeshProperties
{
    public int forwardIndex; //Only used to forward to a different MeshProp data struct
    public int index;       //CellIndex on the Map
    public float alpha;
    public Color minColor;
    public Color maxColor;
    public Matrix4x4 _matrix;
    
    public static int Size()
    {
        return Marshal.SizeOf<GasMeshProps>();
    }
}

public class SpreadingGasGridRenderer
{
}