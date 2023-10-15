using System.Collections.Generic;
using Verse;

namespace TAC.Atmosphere;

public abstract class AtmosphericConverterFromThingRule
{
    internal void ConverterFor(Thing thing, List<AtmosphereConverterBase> results)
    {
        var converter = ConverterFor(thing);
        if (converter != null)
        {
            TLog.Message($"Adding converter for {thing}: {converter.GetType()}");
            results.Add(converter);
        }
    }
    
    public abstract AtmosphereConverterBase? ConverterFor(Thing thing);
}