using System.Collections.Generic;
using TeleCore;
using Verse;

namespace TAE;

/// <summary>
/// Allows you to set biome-wide atmopsheres
/// </summary>
public class TAE_BiomeExtension : DefModExtension
{
    public List<DefValueLoadable<AtmosphericValueDef, float>> uniqueAtmospheres;
}