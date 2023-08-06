using System;
using Verse;

namespace TAE;

//TODO: Revamp of atmospushers
public class AtmospherePusherProps
{
    public Type _worker;
}

public class AtmospherePusher
{
    private AtmospherePushWorker _worker;
    private AtmospherePusherProps _props;

    public AtmospherePusher()
    {
        
    }
    
    public void PushAtmospheres()
    {
    }
}

public abstract class AtmospherePushWorker
{
    public abstract bool IsActive { get; }
}