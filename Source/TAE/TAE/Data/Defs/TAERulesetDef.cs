using System.Collections.Generic;
using RimWorld;
using TeleCore;
using Verse;

namespace TAE;

public class TAERulesetDef : Def
{
    public AtmosphericRealm Realm = AtmosphericRealm.AnyBiome;
    public List<BiomeDef> biomes;
    public List<DefFloatRef<AtmosphericDef>> atmospheres;

    //IncidentRules
    public List<AtmosphericIncidentFilter> incidentFilters;
}
