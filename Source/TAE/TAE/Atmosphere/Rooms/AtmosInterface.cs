using TAE.AtmosphericFlow;

namespace TAE.Atmosphere.Rooms;

public class AtmosInterface
{
    private AtmosphericVolume _from;
    private AtmosphericVolume _to;
    
    public double NextFlow { get; set; }
    public double PrevFlow { get; set; }
    public double Move { get; set; }
    
    public AtmosphericVolume From => _from;
    public AtmosphericVolume To => _to;

    public AtmosInterface(AtmosphericVolume from, AtmosphericVolume to)
    {
        _from = from;
        _to = to;
    }

    public void Notify_SetDirty()
    {
        throw new System.NotImplementedException();
    }
}