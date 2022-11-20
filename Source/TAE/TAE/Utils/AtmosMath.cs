using UnityEngine;

namespace TAE;

/// <summary>
///  ### Room-Comp Specificiation
/// Unit type: Integer
/// 
/// RoomContainer:
/// A room container can hold n amount of atmospheric types
///
/// Equalization:
/// EQ only happens on rooms with a density >= <see cref="CELL_MINEQ"/>
/// </summary>

public static class AtmosMath
{
    //
    internal const int CELL_CAPACITY = 128;

    public static float GetCellCap(int maxDensity)
    {
        return Mathf.Pow(2, maxDensity) * CELL_CAPACITY;
    }
    
    public static FlowResult TryEqualizeVia(AtmosphericPortal portal, AtmosphericTransferWorker worker, AtmosphericContainer from, AtmosphericContainer to, AtmosphericDef atmosDef)
    {
        if (!NeedsEqualizing(from, to, atmosDef, CELL_MINEQ, out var flow, out var diffAbs))
        {
            return FlowResult.None;
        }
        FlowResult flowResult = new FlowResult(flow);

        //
        var sender = flow == AtmosPortalFlow.Positive ? from : to;
        var receiver = flow == AtmosPortalFlow.Positive ? to : from;

        //Get base transfer part
        var value = (sender.TotalStoredOf(atmosDef) / portal[flowResult.FromIndex].ConnectorCount) * 0.5f;
        value = Mathf.Clamp(value, 0, 100);

        //
        var flowAmount = value * diffAbs * worker.GetBaseTransferRate(portal.Thing) * atmosDef.FlowRate;
        flowAmount = Mathf.Floor(flowAmount);

        //
        if (sender.CanFullyTransferTo(receiver, atmosDef, flowAmount))
        {
            if (sender.TryRemoveValue(atmosDef, flowAmount, out float actualVal))
            {
                FlowResult result = worker.CustomTransferFunc(sender, receiver, atmosDef, actualVal);
                if (result.FlowsToOther)
                {
                    receiver.TryAddValue(atmosDef, actualVal, out _);
                }
                result.SetFlow(flow);
                return result;
            }
        }
        return FlowResult.None;
    }
}