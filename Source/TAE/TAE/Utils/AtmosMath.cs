using TeleCore.FlowCore;
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

    public static FlowResult TryEqualizeVia(
        AtmosphericPortal portal,
        AtmosphericTransferWorker worker,
        AtmosphericContainer from,
        AtmosphericContainer to,
        AtmosphericDef atmosDef)
    {
        const float BaseValueScale = 0.5f;
        const float ValueLimit = 100f;

        if (!FlowValueUtils.NeedsEqualizing(from, to, atmosDef, MIN_EQ_VAL, out var flow, out var diffAbs))
        {
            return FlowResult.None;
        }

        var sender = flow == ValueFlowDirection.Positive ? from : to;
        var receiver = flow == ValueFlowDirection.Positive ? to : from;

        // Calculate base transfer part
        var baseValue = CalculateBaseValue(sender, atmosDef, portal, flow, BaseValueScale, ValueLimit);

        // Calculate flow amount
        var flowAmount = CalculateFlowAmount(baseValue, diffAbs, worker, portal, atmosDef);

        // Process Flow depending on flow amount
        return ProcessFlow(sender, receiver, atmosDef, flowAmount, worker, flow);
    }

    private static float CalculateBaseValue(AtmosphericContainer sender, AtmosphericDef atmosDef,
        AtmosphericPortal portal, ValueFlowDirection flow, float scale, float limit)
    {
        var value = (sender.StoredValueOf(atmosDef) / portal[new FlowResult(flow).FromIndex].ConnectorCount) * scale;
        return Mathf.Clamp(value, 0, limit);
    }

    private static float CalculateFlowAmount(float baseValue, float diffAbs,
        AtmosphericTransferWorker worker, AtmosphericPortal portal, AtmosphericDef atmosDef)
    {
        var flowAmount = baseValue * diffAbs * worker.GetBaseTransferRate(portal.Thing) * atmosDef.FlowRate;
        return Mathf.Floor(flowAmount);
    }

    private static FlowResult ProcessFlow(AtmosphericContainer sender, AtmosphericContainer receiver,
        AtmosphericDef atmosDef, float flowAmount, AtmosphericTransferWorker worker, ValueFlowDirection flow)
    {
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