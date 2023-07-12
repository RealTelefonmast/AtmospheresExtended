using TAE.AtmosphericFlow;

namespace TAE.Atmosphere.Rooms;

public class AtmosInterface
{
    public double NextFlow { get; set; } = 0;
    public double PrevFlow { get; set; } = 0;
    public double Move { get; set; } = 0;
    
    
    public bool ResolvedFlow { get; private set; }
    public bool ResolvedMove { get; private set; }
    
    public AtmosphericVolume From { get; }
    public AtmosphericVolume To { get; }

    public AtmosInterface(AtmosphericVolume from, AtmosphericVolume to)
    {
        From = from;
        To = to;
    }

    internal void Notify_SetDirty()
    {
        ResolvedMove = false;
        ResolvedFlow = false;
    }

    internal void Notify_ResolvedMove()
    {
        ResolvedMove = true;
    }

    internal void Notify_ResolvedFlow()
    {
        ResolvedFlow = true;
    }
}