using System.Collections.Generic;
using TAC.Static;
using TeleCore;
using Verse;

namespace TAC;

public class AtmosphericValueDef : FlowValueDef
{
    //The corresponding network value (if available)
    public NetworkValueDef networkValue;
    public DissipationConfig dissipation;
    
    /// <summary>
    /// The tag group this atmospheric def belongs to.
    /// </summary>
    public string atmosphericTag;
    
    /// <summary>
    /// The atmospheric group tags that will be displaced by this atmospheric def.
    /// </summary>
    public List<string> displaceTags;
    
    /// <summary>
    /// Sets the physical elevation range of the gas within a cell. 0 being floor and 1 being ceiling.
    /// This can be used whether or not a gas affects a Pawn.
    /// </summary>
    public FloatRange fillRange = new(0, 1);

    //Rendering
    public NaturalOverlayProperties naturalOverlay;
    public RoomOverlayProperties roomOverlay;
    public bool useRenderLayer = false;
    public double friction;
    
    
    public override void PostLoad()
    {
        //
        base.PostLoad();
        //Unit in liters
        valueUnit = "L";
        AtmosphericReferenceCache.RegisterDef(this);
    }
}