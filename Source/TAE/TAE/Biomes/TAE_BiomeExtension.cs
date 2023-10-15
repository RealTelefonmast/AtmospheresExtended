﻿using System.Collections.Generic;
using TeleCore;
using Verse;

namespace TAC;

/// <summary>
/// Allows you to set biome-wide atmospheres
/// </summary>
public class TAE_BiomeExtension : DefModExtension
{
    public List<DefValueLoadable<AtmosphericValueDef, float>> uniqueAtmospheres;
}