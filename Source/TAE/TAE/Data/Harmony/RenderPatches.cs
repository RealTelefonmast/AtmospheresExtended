using HarmonyLib;
using RimWorld;
using TeleCore;
using Verse;

namespace TAC;

internal static class RenderPatches
{
    [HarmonyPatch(typeof(WeatherManager))]
    [HarmonyPatch(nameof(WeatherManager.DrawAllWeather))]
    public static class DrawAllWeatherPatch
    {
        public static void Postfix(Map ___map)
        {
            //TODO: Fixup renderer
            //___map.GetMapInfo<AtmosphericMapInfo>().DrawSkyOverlays();
        }
    }
}