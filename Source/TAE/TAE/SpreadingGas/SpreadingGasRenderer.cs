using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using TeleCore;
using TeleCore.Loading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace TAE;

[StructLayout(LayoutKind.Sequential)]
struct GasMeshProps
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

public class SpreadingGasRenderer //: IDisposable
{
    //References
    private SpreadingGasLayer layer;
    private readonly Map map;
    private int bufferSize;

    private uint[] shaderArguments = new uint[5];
    
    //Buffers
    private Material _material;
    private float angle;
    private static Bounds bounds = new(Vector3.zero, Vector3.one * 10000f);
    
    private NativeArray<GasMeshProps> meshProperties;
    private Matrix4x4[] internalMatrices;

    private ComputeBuffer bufferMeshData;
    private ComputeBuffer bufferArguments;
    
    internal bool bufferIsDirty = true;
    private bool isInitialised = false;

    //PropertyIDs
    private const string PropertyAngle = "_Angle";
    private const string PropertyMaxAlpha = "_MaxAlpha";
    private const string PropertySrcMode = "SrcMode";
    private const string PropertyDstMode = "DstMode";
    
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
                _material.enableInstancing = true;
            }
            return _material;
        }
    }

    public SpreadingGasRenderer(SpreadingGasLayer layer, Map map)
    {
        this.layer = layer;
        this.map = map;
        this.bufferSize = map.cellIndices.NumGridCells;
        meshProperties = new NativeArray<GasMeshProps>(bufferSize, Allocator.Persistent);
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
    
    public void Draw()
    {
        if (!layer.HasAnyGas) return;
        if (!isInitialised)
        {
            InitRenderData();
            isInitialised = true;
        }
        
        UpdateGPUData();
        Graphics.DrawMeshInstancedIndirect(MeshPool.plane10, 0, Material, bounds, bufferArguments);
    }

    private void InitRenderData()
    {
        //Init Data
        Material.SetFloat(PropertyMaxAlpha, 1.0f);
        
        bufferMeshData = new ComputeBuffer(bufferSize, GasMeshProps.Size());
        bufferArguments = new ComputeBuffer(1, shaderArguments.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        Material.SetBuffer("_MeshProperties", bufferMeshData);
    }

    public void UpdateArguments()
    {
        shaderArguments[0] = MeshPool.plane10.GetIndexCount(0);
        shaderArguments[1] = (uint)(layer.TotalGasCount);
        shaderArguments[2] = MeshPool.plane10.GetIndexStart(0);
        shaderArguments[3] = MeshPool.plane10.GetBaseVertex(0);
        bufferArguments.SetData(shaderArguments);
    }
    
    private void UpdateMeshProps()
    {
        //Fill
        int j = 0;
        for (var i = 0; i < layer.Grid.Length; i++)
        {
            //var value = layer.Grid[i];
            if (layer.AnyGasAt(i))
            {
                //Forward Mapping
                var forwarded = meshProperties[j];
                forwarded.forwardIndex = i;
                meshProperties[j] = forwarded;

                //Set MeshData
                var meshProps = meshProperties[i];
                meshProps._matrix = internalMatrices[i];
                
                meshProps.index = i;
                meshProps.alpha = layer.PercentAt(i);
                meshProps.minColor = layer.GasType.colorMin;
                meshProps.maxColor = layer.GasType.colorMax;
                meshProperties[i] = meshProps;
                j++;
                
                //bufferMeshData.SetData(meshProperties, i, i, 1);
            }
        }
        
        //
        bufferMeshData.SetData(meshProperties); //, 0, 0, meshProperties.Length
    }
}
