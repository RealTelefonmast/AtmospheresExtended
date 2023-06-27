using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TAE;

[StaticConstructorOnStartup]
internal static class AtmosContent
{
    private static AssetBundle bundleInt;
    private static Dictionary<string, Shader> lookupShades;
    private static Dictionary<string, ComputeShader> lookupComputeShades;
    private static Dictionary<string, Material> lookupMats;

    //
    public static ComputeShader GasGridCompute = LoadComputeShader("GasGridCompute");
        
    //Shaders
    public static readonly Shader TextureBlend = LoadShader("TextureBlend");
    public static readonly Shader CustomOverlay = LoadShader("CustomOverlay");
    public static readonly Shader InstancedGas = LoadShader("InstancedGas");

    public static readonly Material CustomOverlayWorld = LoadMaterial("CustomOverlayWorld");
    //public static readonly Shader WavesShader = LoadShader("Waves");

    public static AssetBundle AtmosBundle
    {
        get
        {
            if (bundleInt == null)
            {
                bundleInt = AtmosphereMod.Mod.MainBundle;
            }
            return bundleInt;
        }
    }

    public static ComputeShader LoadComputeShader(string shaderName)
    {
        if (lookupComputeShades == null)
            lookupComputeShades = new Dictionary<string, ComputeShader>();

        if (AtmosBundle != null)
        {
            if (!lookupComputeShades.ContainsKey(shaderName))
                lookupComputeShades[shaderName] = AtmosBundle.LoadAsset<ComputeShader>(shaderName);
        }

        if (!lookupComputeShades.TryGetValue(shaderName, out var shader) || shader == null)
        {
            TLog.Warning($"Could not load shader '{shaderName}'");
            return null;
        }
        return shader;
    }
        
    public static Shader LoadShader(string shaderName)
    {
        if (lookupShades == null)
            lookupShades = new Dictionary<string, Shader>();

        if (AtmosBundle != null)
        {
            if (!lookupShades.ContainsKey(shaderName))
                lookupShades[shaderName] = AtmosBundle.LoadAsset<Shader>(shaderName);
        }

        if (!lookupShades.TryGetValue(shaderName, out var shader) || shader == null)
        {
            TLog.Warning($"Could not load shader '{shaderName}'");
            return ShaderDatabase.DefaultShader;
        }
        return shader;
    }


    public static Material LoadMaterial(string materialName)
    {
        lookupMats ??= new Dictionary<string, Material>();

        if (AtmosBundle != null)
        {
            if (!lookupMats.ContainsKey(materialName))
                lookupMats[materialName] = AtmosBundle.LoadAsset<Material>(materialName);
        }

        if (!lookupMats.TryGetValue(materialName, out var mat) || mat == null)
        {
            Log.Warning($"Could not load material '{materialName}'");
            return BaseContent.BadMat;
        }
        return mat;
    }

}