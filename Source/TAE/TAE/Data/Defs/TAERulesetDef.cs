using System.Collections.Generic;
using RimWorld;
using TeleCore;
using Verse;

namespace TAE;

public class TAERulesetDef : Def
{
    public AtmosphericRealm Realm = AtmosphericRealm.AnyBiome;
    public List<BiomeDef> biomes;
    public List<DefValueLoadable<AtmosphericDef, float>> atmospheres;

    //IncidentRules
    public List<AtmosphericIncidentFilter> incidentFilters;
}
