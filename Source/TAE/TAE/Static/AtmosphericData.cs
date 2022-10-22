﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TAE.Static;

[StaticConstructorOnStartup]
public static class AtmosphericData
{
    public readonly static Dictionary<StuffCategoryDef, float> PassPercentByStuff;

    static AtmosphericData()
    {
        PassPercentByStuff = new Dictionary<StuffCategoryDef, float>
        {
            {StuffCategoryDefOf.Fabric  , 0.9f},
            {StuffCategoryDefOf.Leathery, 0.5f},
            {StuffCategoryDefOf.Woody   , 0.25f},
            {StuffCategoryDefOf.Stony   , 0.0625f},
            {StuffCategoryDefOf.Metallic, 0f},
        };
    }
}