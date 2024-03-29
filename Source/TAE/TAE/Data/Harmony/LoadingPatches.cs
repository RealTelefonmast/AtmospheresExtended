﻿using HarmonyLib;
using TeleCore;
using Verse;
using Verse.Profile;

namespace TAC;

internal static class LoadingPatches
{
    [HarmonyPatch(typeof(TemperatureSaveLoad))]
    [HarmonyPatch(nameof(TemperatureSaveLoad.ApplyLoadedDataToRegions))]
    public static class ApplyLoadedDataToRegionsPatch
    {
        public static void Postfix(Map ___map)
        {
            ___map.GetMapInfo<AtmosphericMapInfo>().Notify_ApplyLoadedData();
            
        }
    }
}