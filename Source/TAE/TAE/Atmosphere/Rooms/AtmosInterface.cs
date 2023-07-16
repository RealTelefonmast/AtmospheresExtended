using TAE.AtmosphericFlow;

namespace TAE.Atmosphere.Rooms;

public class AtmosInterface
{
    public double NextFlow { get; set; } = 0;
    public double PrevFlow { get; set; } = 0;
    public double Move { get; set; } = 0;
    
    public double FlowRate { get; set; }
    
    public AtmosphericVolume From { get; private set; }
    public AtmosphericVolume To { get; private set; }

    public AtmosInterface(AtmosphericVolume from, AtmosphericVolume to)
    {
        From = from;
        To = to;
    }
    
    public void UpdateBasedOnFlow(double flow)
    {
        if (flow < 0)
        {
            (From, To) = (To, From);
        }
    }
    
    public AtmosphericVolume Opposite(AtmosphericVolume volume)
    {
        return From == volume ? To : From;
    }
}