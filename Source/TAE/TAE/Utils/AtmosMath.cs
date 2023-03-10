using TeleCore;
using TeleCore.FlowCore;
using TeleCore.Static.Utilities;
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
    public const float MIN_EQ_VAL = 2;
    
    public static float GetCellCap(int maxDensity)
    {
        return Mathf.Pow(2, maxDensity) * CELL_CAPACITY;
    }
    
    public static FlowResult TryEqualizeVia(AtmosphericPortal portal, AtmosphericTransferWorker worker, AtmosphericContainer from, AtmosphericContainer to, AtmosphericDef atmosDef)
    {
        if (!FlowValueUtils.NeedsEqualizing(from, to, atmosDef, MIN_EQ_VAL, out var flow, out var diffAbs))
        {
            return FlowResult.None;
        }
        //FlowResult flowResult = new FlowResult(flow);

        //
        var sender = flow == ValueFlowDirection.Positive ? from : to;
        var receiver = flow == ValueFlowDirection.Positive ? to : from;

        //Get base transfer part
        var value = (sender.StoredValueOf(atmosDef) / portal[new FlowResult(flow).FromIndex].ConnectorCount) * 0.5f;
        value = Mathf.Clamp(value, 0, 100);

        //
        var flowAmount = value * diffAbs * worker.GetBaseTransferRate(portal.Thing) * atmosDef.FlowRate;
        flowAmount = Mathf.Floor(flowAmount);

        //
        if (sender.CanTransferAmountTo(receiver, atmosDef, flowAmount))
        {
            if (sender.TryRemoveValue(atmosDef, flowAmount, out var result))
            {
                FlowResult customFlow = worker.CustomTransferFunc(sender, receiver, atmosDef, result.ActualAmount);
                if (customFlow.FlowsToOther)
                {
                    receiver.TryAddValue(atmosDef, result.ActualAmount, out _);
                }
                customFlow.SetFlow(flow);
                return customFlow;
            }
        }
        return FlowResult.None;
    }
}