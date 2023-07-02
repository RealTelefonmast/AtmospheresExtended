using System.Collections.Generic;
using RimWorld;
using TeleCore;
using Verse;

namespace TAE.Static;

internal static class IncidentFilterDB
{
    public static Dictionary<IncidentDef, AtmosphericIncidentFilter> filtersByIncident;

    public static bool Inject_CanFireNow(IncidentDef def, Map map)
    {
        if (filtersByIncident.TryGetValue(def, out var value))
        {
            var volume = map.GetMapInfo<AtmosphericMapInfo>().MapVolume.Volume;
            return volume.StoredPercentOf(value.atmosDef) >= value.threshold;
        }

        return true;
    }
}