using JetBrains.Annotations;
using TeleCore.FlowCore;
using TeleCore.Network.Data;

namespace TAC.AtmosphericFlow;

/// <summary>
/// Similar to <see cref="FlowBox"/> for volumes of atmospheric values.
/// </summary>
public class AtmosphericVolume : FlowVolumeShared<AtmosphericValueDef>
{
    private int _cells;
    
    public override double MaxCapacity => CapacityPerType * _config.AllowedValues.Count;
    public override double CapacityPerType => _cells * AtmosResources.CELL_CAPACITY;

    public void UpdateVolume(int cellCount)
    {
        _cells = cellCount;
    }

    public AtmosphericVolume(FlowVolumeConfig<AtmosphericValueDef> config) : base(config)
    {
    }
}