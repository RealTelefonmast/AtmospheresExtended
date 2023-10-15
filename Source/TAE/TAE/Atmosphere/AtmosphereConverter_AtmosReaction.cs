﻿using TAC.Atmosphere;
using Verse;

namespace TAC;

public class AtmosReactionDefExtension : DefModExtension
{
    public AtmosphericValueDef input;
    public AtmosphericValueDef output;
    public int interval = 90;
    public float reactionRate;
}

public class AtmosphericConverterFromThingRule_Reaction : AtmosphericConverterFromThingRule
{
    public override AtmosphereConverterBase ConverterFor(Thing thing)
    {
        var extension = thing.def.GetModExtension<AtmosReactionDefExtension>();
        if (extension != null)
        {
            return new AtmosphereConverter_AtmosReaction(thing, extension);
        }
        return null;
    }
}

public class AtmosphereConverter_AtmosReaction : AtmosphereConverterBase
{
    public AtmosReactionDefExtension properties;

    public AtmosphericValueDef Input => properties.input;
    public AtmosphericValueDef Output => properties.output;
    public float ReactionRate => properties.reactionRate;

    public override bool IsActive => Atmosphere.Volume.StoredValueOf(Input) >= ReactionRate;

    public AtmosphereConverter_AtmosReaction(Thing thing, AtmosReactionDefExtension properties) : base(thing)
    {
        this.properties = properties;
    }

    public override void Tick()
    {
        if (GenTicks.TicksAbs % properties.interval != 0) return;
        //TODO: Generic conversion and stuff
    }
}