using System.Collections.Generic;
using System.Xml;
using RimWorld;
using TeleCore;
using Verse;

namespace TAE
{
    public class TAE_RulesetDef : Def
    {
        public AtmosphericOccurence occurence = AtmosphericOccurence.AnyBiome;
        public List<BiomeDef> biomes;
        public List<DefFloat<AtmosphericDef>> atmospheres;

        //IncidentRules
        public List<AtmosphericIncidentFilter> incidentFilters;
    }

    public class AtmosphericIncidentFilter : Editable
    {
        public IncidentDef incidentDef;
        public AtmosphericDef atmosDef;
        public float threshold = 0;

        public override void PostLoad()
        {
            base.PostLoad();
            TLog.Message("Postloading");
        }

        // IncidentDef -> >(AtmosDef, 0.5)
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {

        }
    }

    internal static class IncidentFilterDB
    {
        public static Dictionary<IncidentDef, AtmosphericIncidentFilter> filtersByIncident;

        public static bool Inject_CanFireNow(IncidentDef def, Map map)
        {
            if (filtersByIncident.TryGetValue(def, out var value))
            {
                var container = map.GetMapInfo<AtmosphericMapInfo>().MapContainer;
                return container.StoredPercentOf(value.atmosDef) >= value.threshold;
            }

            return true;
        }
    }
}
