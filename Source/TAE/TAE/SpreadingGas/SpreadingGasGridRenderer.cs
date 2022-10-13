using System.Runtime.InteropServices;
using TeleCore;
using TeleCore.Loading;
using Unity.Collections;
using UnityEngine;
using Verse;

namespace TAE;

[StructLayout(LayoutKind.Sequential)]
struct GasMeshProperties
{
    public int forwardIndex; //Only used to forward to a different MeshProp data struct
    public int index;        //CellIndex on the Map
    public float[] indexedAlphas;
    public Matrix4x4 _matrix;
    
    public static int Size()
    {
        return Marshal.SizeOf<GasMeshProperties>();
    }
}

public class SpreadingGasGridRenderer
{
    private SpreadingGasGrid grid;
    private readonly Map map;
    private int bufferSize;

    private uint[] shaderArguments = new uint[5];
    
    //
    private Material _material;
    private float angle;
    private static Bounds bounds = new(Vector3.zero, Vector3.one * 10000f);
    
    //Buffer Data
    private NativeArray<GasMeshProperties> meshProperties;
    private Matrix4x4[] internalMatrices;

    private ComputeBuffer bufferMeshData;
    private ComputeBuffer bufferArguments;
    
    private bool isInitialised = false;
    
    //PropertyIDs
    private const string PropertyAngle = "_Angle";
    private const string PropertyMaxAlpha = "_MaxAlpha";
    private const string PropertyTypeCount = "_TypeCount";
    private const string PropertySrcMode = "SrcMode";
    private const string PropertyDstMode = "DstMode";
    
    private static readonly int TypeCount = Shader.PropertyToID(PropertyTypeCount);
    private static readonly int MinColors = Shader.PropertyToID("_MinColors");
    private static readonly int MaxColors = Shader.PropertyToID("_MaxColors");

    public SpreadingGasGridRenderer(SpreadingGasGrid grid, Map map)
    {
        this.grid = grid;
        this.map = map;
        this.bufferSize = map.cellIndices.NumGridCells;
        meshProperties = new NativeArray<GasMeshProperties>(bufferSize, Allocator.Persistent);
        internalMatrices = new Matrix4x4[bufferSize];
        
        SetupInternalMatrixBuffer();
        
        //
        void UnloadData()
        {
            meshProperties.Dispose();
            bufferMeshData?.Dispose();
            bufferArguments?.Dispose();
        }

        UnloadUtility.RegisterUnloadAction(UnloadData);
        ApplicationQuitUtility.RegisterQuitEvent(UnloadData);
    }
    
    public Material Material
    {
        get
        {
            if (_material == null)
            {
                if (AtmosContent.InstancedGas == null)
                {
                    TLog.Error($"Cannot find {nameof(AtmosContent.InstancedGas)}!");
                }

                _material = MaterialPool.MatFrom("Things/Gas/GasCloudThickA", AtmosContent.InstancedGas);
                _material.SetInt(TypeCount, grid.gasDefs.Length);
                _material.SetColorArray(MinColors, grid.minColors);
                _material.SetColorArray(MaxColors, grid.maxColors);
                _material.enableInstancing = true;
            }
            return _material;
        }
    }
    
    [TweakValue("Atmospheric", 0, 1000)] 
    private static float _RotSpeed = 100;

    public void UpdateGPUData()
    {
        //Set Mesh Data
        UpdateArguments();
        UpdateMeshProps();

        //Set Speed
        Material.SetFloat("_RotSpeed", _RotSpeed);
    }

    //
    private void SetupInternalMatrixBuffer()
    {
        Vector3 size = new(4.0f, 0f, 4.0f);
        for (int i = 0; i < bufferSize; i++)
        {
            Rand.PushState(i);
            Vector3 pos = map.cellIndices.IndexToCell(i).ToVector3ShiftedWithAltitude(AltitudeLayer.Gas);
            pos.x += Rand.Range(-0.25f, 0.25f);
            pos.z += Rand.Range(-0.24f, 0.24f);
            Quaternion rotation = Quaternion.AngleAxis(((float) Rand.Range(0, 360)), Vector3.up);
            internalMatrices[i] = Matrix4x4.TRS(pos, rotation, size);
            Rand.PopState();
        }
    }
    
    private void InitRenderData()
    {
        //Init Data
        Material.SetFloat(PropertyMaxAlpha, 1.0f);
        
        bufferMeshData = new ComputeBuffer(bufferSize, GasMeshProperties.Size());
        bufferArguments = new ComputeBuffer(1, shaderArguments.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        Material.SetBuffer("_MeshProperties", bufferMeshData);
    }

    public void UpdateArguments()
    {
        shaderArguments[0] = MeshPool.plane10.GetIndexCount(0);
        shaderArguments[1] = (uint)(grid.TotalGasCount);
        shaderArguments[2] = MeshPool.plane10.GetIndexStart(0);
        shaderArguments[3] = MeshPool.plane10.GetBaseVertex(0);
        bufferArguments.SetData(shaderArguments);
    }
    
    private void UpdateMeshProps()
    {
        //Fill
        int j = 0;
        for (var i = 0; i < grid.Grid.Length; i++)
        {
            //var value = layer.Grid[i];
            if (grid.AnyGasAt(i))
            {
                //Forward Mapping
                var forwarded = meshProperties[j];
                forwarded.forwardIndex = i;
                meshProperties[j] = forwarded;

                //Set MeshData
                var meshProps = meshProperties[i];
                meshProps._matrix = internalMatrices[i];
                
                meshProps.index = i;
                meshProps.indexedAlphas = grid.DensityPercentagesAt(i);
                meshProperties[i] = meshProps;
                j++;
                
                //bufferMeshData.SetData(meshProperties, i, i, 1);
            }
        }
        
        //
        bufferMeshData.SetData(meshProperties); //, 0, 0, meshProperties.Length
    }
    
    //
    public void Draw()
    {
        if (!grid.HasAnyGas) return;
        if (!isInitialised)
        {
            InitRenderData();
            isInitialised = true;
        }
        
        UpdateGPUData();
        Graphics.DrawMeshInstancedIndirect(MeshPool.plane10, 0, Material, bounds, bufferArguments);
    }
}