namespace TAE;

public struct FlowResult
{
    private bool hadFlow = false;
    private bool flowToOther = false;
    private bool isVoided = false;
    private AtmosPortalFlow flowDirection = AtmosPortalFlow.None;

    public bool NoFlow => !hadFlow;
    public bool FlowsToOther => flowToOther;
    public bool IsVoided => isVoided;

    public int FromIndex
    {
        get
        {
            return flowDirection switch
            {
                AtmosPortalFlow.Positive => 0,
                AtmosPortalFlow.Negative => 1,
                _ => -1
            };
        }
    }

    public int ToIndex
    {
        get
        {
            return flowDirection switch
            {
                AtmosPortalFlow.Positive => 1,
                AtmosPortalFlow.Negative => 0,
                _ => -1
            };
        }
    }

    public FlowResult() { }

    public FlowResult(AtmosPortalFlow flowDir)
    {
        hadFlow = flowToOther = true;
        flowDirection = flowDir;
    }
    
    public void SetFlow(AtmosPortalFlow flowDir)
    {
        hadFlow = flowToOther = true;
        this.flowDirection = flowDir;
    }

    public static FlowResult None => new() {hadFlow = false};
    public static FlowResult ResultVoided => new() {isVoided = true, hadFlow = true };
    public static FlowResult ResultNormalFlow => new() {flowToOther = true, hadFlow = true};

    public override string ToString()
    {
        return $"HadFlow: {hadFlow}; FlowToOther: {flowToOther}; IsVoided: {isVoided}; FlowDir: {flowDirection} [{FromIndex} -> {ToIndex}]";
    }
}