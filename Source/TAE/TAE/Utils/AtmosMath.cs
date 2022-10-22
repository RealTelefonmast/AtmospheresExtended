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
    internal const int CELL_MINEQ = 2;

    public static float GetCellCap(int maxDensity)
    {
        return Mathf.Pow(2, maxDensity) * CELL_CAPACITY;
    }
    
    //
    public static bool NeedsEqualizing(AtmosphericContainer roomA, AtmosphericContainer roomB, AtmosphericDef def, float minDiffMargin, out AtmosPortalFlow flow, out float diffPct)
    {
        flow = AtmosPortalFlow.None;
        diffPct = 0f;
        if (roomA.Parent.IsOutdoors && roomB.Parent.IsOutdoors) return false;
        var fromTotal = roomA.TotalStoredOf(def);
        var toTotal = roomB.TotalStoredOf(def);

        var fromPct = roomA.StoredPercentOf(def);
        var toPct = roomB.StoredPercentOf(def);

        var totalDiff = Mathf.Abs(fromTotal - toTotal);
        diffPct = fromPct - toPct;

        flow = diffPct switch
        {
            > 0 => AtmosPortalFlow.Positive,
            < 0 => AtmosPortalFlow.Negative,
            _ => AtmosPortalFlow.None
        };
        diffPct = Mathf.Abs(diffPct);
        if (diffPct <= 0.01f) return false;
        return diffPct > 0 && totalDiff >= minDiffMargin;
    }
    
    public static FlowResult TryEqualizeVia(AtmosphericPortal portal, AtmosphericTransferWorker worker, AtmosphericContainer from, AtmosphericContainer to, AtmosphericDef atmosDef)
    {
        if (!NeedsEqualizing(from, to, atmosDef, 2, out var flow, out var diffAbs))
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
    
    public static void TryEqualize(AtmosphericContainer roomA, AtmosphericContainer roomB, AtmosphericDef atmosDef)
    {
        if (!NeedsEqualizing(roomA, roomB, atmosDef, 2, out var flow, out var diffPct))
        {
            return;
        }

        AtmosphericContainer sender   = flow == AtmosPortalFlow.Positive ? roomA : roomB;
        AtmosphericContainer receiver = flow == AtmosPortalFlow.Positive ? roomB : roomA;

        //Get base transfer part
        var value = sender.TotalStoredOf(atmosDef) * 0.5f;
        var flowAmount = sender.GetMaxTransferRateTo(receiver, atmosDef, Mathf.CeilToInt(value * diffPct * atmosDef.FlowRate));

        //
        if (sender.CanFullyTransferTo(receiver, atmosDef, flowAmount))
        {
            if (sender.TryRemoveValue(atmosDef, flowAmount, out float actualVal))
            {
                receiver.TryAddValue(atmosDef, actualVal, out _);
            }
        }
    }

    public static bool TryAddValueTo(AtmosphericContainer container, AtmosphericDef def, float amount, out float actualAmount)
    {
        return container.TryAddValue(def, amount, out actualAmount);
    }
}