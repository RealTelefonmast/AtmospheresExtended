﻿using System.Collections.Generic;
using RimWorld;
using TeleCore;
using Verse;

namespace TAE;

public class TAERulesetDef : Def
{
    public AtmosphericRealm realm = AtmosphericRealm.AnyBiome;
    public List<BiomeDef> biomes;
    public List<DefValueLoadable<AtmosphericValueDef, float>> atmospheres;

    //IncidentRules
    public List<AtmosphericIncidentFilter> incidentFilters;
}
