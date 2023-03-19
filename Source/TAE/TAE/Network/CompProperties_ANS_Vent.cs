using System.Collections.Generic;
using System.Linq;
using TAE.Static;
using TeleCore;
using Verse;

namespace TAE;

public enum AtmosphericVentMode
{
    Intake,
    Output,
    TwoWay
}

public class CompProperties_ANS_Vent : CompProperties_ANS
{
    [Unsaved()]
    private List<AtmosphericDef> allowedValuesInt;
        
    //
    public IntVec3 intakeOffset;
    public AtmosphericVentMode ventMode = AtmosphericVentMode.Intake;
    public int gasThroughPut = 1;

    //Filter
    private AtmosphericVentFilter filter;
    
    //
    public List<DefFloat<AtmosphericDef>> upkeepLevels;
    
    private class AtmosphericVentFilter
    {
        public string acceptedTag;
        public List<AtmosphericDef> acceptedAtmospheres;
    }
    
    public List<AtmosphericDef> AllowedValues
    {
        get
        {
            if (allowedValuesInt == null)
            {
                var list = new List<AtmosphericDef>();
                if (filter.acceptedTag != null)
                {
                    list.AddRange(AtmosphericReferenceCache.AtmospheresOfTag(filter.acceptedTag));
                }
                if (!filter.acceptedAtmospheres.NullOrEmpty())
                {
                    list.AddRange(filter.acceptedAtmospheres);
                }
                allowedValuesInt = list.Distinct().ToList();
            }
            return allowedValuesInt;
        }
    }
    
    public IntVec3 GetIntakePos(IntVec3 basePos, Rot4 rotation)
    {
        return basePos + intakeOffset.RotatedBy(rotation);
    }

}