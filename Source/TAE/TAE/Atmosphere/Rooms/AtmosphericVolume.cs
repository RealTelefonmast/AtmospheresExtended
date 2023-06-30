using TeleCore.FlowCore;
using TeleCore.Generics.Container;
using TeleCore.Network.Flow;
using TeleCore.Network.Flow.Values;

namespace TAE.AtmosphericFlow;

/// <summary>
/// Similar to <see cref="FlowBox"/> for volumes of atmospheric values.
/// </summary>
public class AtmosphericVolume : FlowVolume<AtmosphericDef>
{
    private int _cells;
    
    public double FlowRate { get; set; }
    
    public override double MaxCapacity => _cells * AtmosResources.CELL_CAPACITY;

    public void UpdateVolume(int cellCount)
    {
        _cells = cellCount;
    }

    public FlowValueStack RemoveContent(double movPct)
    {
        var rem = _stack * movPct;
        _stack -= rem;
        return rem;
    }

    public void AddContent(FlowValueStack stack)
    {
        _stack += stack;
    }
}