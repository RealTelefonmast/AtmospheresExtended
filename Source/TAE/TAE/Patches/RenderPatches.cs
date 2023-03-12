using HarmonyLib;
using RimWorld;
using TeleCore;
using Verse;

namespace TAE
{
    internal static class RenderPatches
    {
        [HarmonyPatch(typeof(WeatherManager))]
        [HarmonyPatch(nameof(WeatherManager.DrawAllWeather))]
        public static class DrawAllWeatherPatch
        {
            public static void Postfix(Map ___map)
            {
                ___map.GetMapInfo<AtmosphericMapInfo>().DrawSkyOverlays();
            }
        }
    }
}
