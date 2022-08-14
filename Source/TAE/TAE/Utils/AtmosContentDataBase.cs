using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TAE
{
    [StaticConstructorOnStartup]
    internal static class AtmosContent
    {
        private static AssetBundle bundleInt;
        private static Dictionary<string, Shader> lookupShades;
        private static Dictionary<string, ComputeShader> lookupComputeShades;
        private static Dictionary<string, Material> lookupMats;

        //Shaders
        public static readonly Shader TextureBlend = LoadShader("TextureBlend");
        public static readonly Shader WavesShader = LoadShader("Waves");

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

    }
}
