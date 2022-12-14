using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TeleCore;
using Verse;
using Verse.Profile;

namespace TAE
{
    internal static class LoadingPatches
    {
        [HarmonyPatch(typeof(TemperatureCache))]
        [HarmonyPatch("TryCacheRegionTempInfo")]
        public static class TryCacheRegionTempInfoPatch
        {
            public static void Postfix(IntVec3 c, Region reg, Map ___map)
            {
                ___map.GetMapInfo<AtmosphericMapInfo>().Cache.TryCacheRegionAtmosphericInfo(c, reg);
            }
        }

        [HarmonyPatch(typeof(TemperatureCache))]
        [HarmonyPatch("ResetCachedCellInfo")]
        public static class ResetCachedCellInfoPatch
        {
            public static void Postfix(IntVec3 c, Map ___map)
            {
                ___map.GetMapInfo<AtmosphericMapInfo>().Cache.ResetInfo(c);
            }
        }

        [HarmonyPatch(typeof(TemperatureSaveLoad))]
        [HarmonyPatch(nameof(TemperatureSaveLoad.ApplyLoadedDataToRegions))]
        public static class ApplyLoadedDataToRegionsPatch
        {
            public static void Postfix(Map ___map)
            {
                TLog.Debug("## ApplyLoadedDataToRegionsPatch");
                ___map.GetMapInfo<AtmosphericMapInfo>().Cache.scriber.ApplyLoadedDataToRegions();
            }
        }

        [HarmonyPatch(typeof(MemoryUtility))]
        [HarmonyPatch(nameof(MemoryUtility.ClearAllMapsAndWorld))]
        internal static class MemoryUtility_ClearAllMapsAndWorldPatch
        {
            static void Postfix()
            {
                TLog.Debug("Cleaing All Maps And World");
            }
        }
    }
}
