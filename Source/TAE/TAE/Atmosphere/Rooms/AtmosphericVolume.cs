using JetBrains.Annotations;
using TeleCore.FlowCore;
using TeleCore.Network.Data;

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

    public AtmosphericVolume(FlowVolumeConfig<AtmosphericDef> config) : base(config)
    {
    }
}