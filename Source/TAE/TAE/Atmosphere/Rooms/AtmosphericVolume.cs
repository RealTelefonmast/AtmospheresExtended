using TeleCore.FlowCore;

namespace TAE.AtmosphericFlow;

/// <summary>
/// Similar to <see cref="FlowBox"/> for volumes of atmospheric values.
/// </summary>
public class AtmosphericVolume : FlowVolume<AtmosphericDef>
{
    private int _cells;
    
    public override double MaxCapacity => _cells * AtmosResources.CELL_CAPACITY;

    public void UpdateVolume(int cellCount)
    {
        _cells = cellCount;
    }
}