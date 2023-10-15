using System.Collections.Generic;
using Verse;

namespace TAC;


/// <summary>
/// Defines properties of any gas or fluid that can dissipate into air or ground.
/// </summary>
public class DissipationConfig : Editable
{
    public SpreadingGasTypeDef toGas;
    public DissipationMode mode;
    
    //TODO: Add terrainfilter from TR
    public List<string> terrainFilter;
}