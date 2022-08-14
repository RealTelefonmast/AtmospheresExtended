using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace TAE
{
    public class AtmosphereMod : Mod
    {
        private static AtmosphereMod modInt;
        private static Harmony atmosphere;

        public static AtmosphereMod Mod => modInt;
        public static Harmony TAE => atmosphere ??= new Harmony("telefonmast.AtmosphereExtended");

        internal AssetBundle MainBundle
        {
            get
            {
                TLog.Message("Loading AssetBundle");
                string pathPart = "";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    pathPart = "StandaloneOSX";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    pathPart = "StandaloneWindows";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    pathPart = "StandaloneLinux64";

                string mainBundlePath = Path.Combine(Mod.Content.RootDir, $@"Resources\AssetBundles\{pathPart}\taebundle");
                var bundle = AssetBundle.LoadFromFile(mainBundlePath);

                foreach (var allAssetName in bundle.GetAllAssetNames())
                {
                    TLog.Message($"- [{allAssetName}]");
                }

                return bundle;
            }
        }

        public AtmosphereMod(ModContentPack content) : base(content)
        {
            modInt = this;
            var assembly = Assembly.GetExecutingAssembly();

            //
            TAE.PatchAll(assembly);
        }
    }
}
