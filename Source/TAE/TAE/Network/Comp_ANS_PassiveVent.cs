using System.Collections.Generic;
using System.Linq;
using RimWorld;
using TAE.Static;
using TeleCore;
using TeleCore.Static.Utilities;
using UnityEngine;
using Verse;

namespace TAE;

/// <summary>
/// Takes in atmospheric gasses passively, or pushes them out
/// </summary>
public class Comp_ANS_PassiveVent : Comp_AtmosphericNetworkStructure
{
    private IntVec3 _intakeCell;
    
    private CompFlickable flickableComp;

    public RoomComponent_Atmospheric Atmos => _intakeCell.GetRoom(parent.Map).GetRoomComp<RoomComponent_Atmospheric>();
    public CompProperties_ANS_PassiveVent Props => (CompProperties_ANS_PassiveVent)base.props;
    
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        flickableComp = parent.GetComp<CompFlickable>();
        _intakeCell = GetIntakePos(parent.Position, parent.Rotation);
    }

    internal static IntVec3 GetIntakePos(IntVec3 basePos, Rot4 rotation)
    {
        return basePos + IntVec3.South.RotatedBy(rotation);
    }

    public override void CompTick()
    {
        //TLog.Message($"NormalTikcing... {Props.AllowedValues.Count}");
        base.CompTick();
        
        if(parent.IsHashIntervalTick(10))
            Tick();
    }

    public override void CompTickRare()
    {
    }
    
    private void Tick(int tickRate = 1)
    {
        var roomComp = Atmos;
        var roomContainer = roomComp.CurrentContainer;
        var networkComp = AtmosphericComp;

        foreach (var atmosphericDef in Props.AllowedValues)
        {
            //var roomValuePct = roomCOntainer.StoredPercentOf(atmosphericDef);
            //var ventValuePct = networkComp.Container.StoredPercentOf(atmosphericDef.networkValue);
            //var pctDiff = roomValuePct - ventValuePct;
            if (FlowValueUtils.NeedsEqualizing(roomContainer, networkComp.Container, out var flowDir, out var diffPct))
            {
                diffPct = Mathf.Abs(diffPct);
                //TLog.Debug($"Equalizing {roomComp.Room.ID} <=> {networkComp} | FlowDir: {flowDir} | Diff: {diffPct.ToStringPercent()}");   
                switch (flowDir)
                {
                    //Push From Room Into Vent
                    case ValueFlowDirection.Positive:
                    {
                        //var value = (roomContainer.TotalStoredOf(atmosphericDef) / Props.AllowedValues.Count) * 0.5f;
                        //value = Mathf.Clamp(value, 0, networkComp.Container.Capacity / Props.AllowedValues.Count);
                        //var flowAmount = value * atmosphericDef.FlowRate;
                        var value = roomContainer.StoredValueOf(atmosphericDef) * 0.5f;
                        var flowAmount = networkComp.Container.GetMaxTransferRate(atmosphericDef.networkValue, Mathf.CeilToInt(value * diffPct * atmosphericDef.networkValue.FlowRate));
                        if (roomContainer.TryTransferTo(networkComp.Container, atmosphericDef, Mathf.Round(flowAmount)))
                        {
                            //...
                        }
                        break;
                    }
                    //Push From Vent Into Room
                    case ValueFlowDirection.Negative:
                    {
                        //var value = (networkComp.Container.TotalStoredOf(atmosphericDef.networkValue)) * 0.5f;
                        //value = Mathf.Clamp(value, 0, Props.gasThroughPut * tickRate);
                        //var flowAmount = value * atmosphericDef.FlowRate;
                        if (ConnectedToLowPressure()) return;
                        
                        var value = (networkComp.Container.StoredValueOf(atmosphericDef.networkValue)) * 0.5f;
                        var flowAmount = networkComp.Container.GetMaxTransferRate(atmosphericDef.networkValue, Mathf.CeilToInt(value * diffPct * atmosphericDef.networkValue.FlowRate));
                        if (roomContainer.TryReceiveFrom(networkComp.Container, atmosphericDef, Mathf.RoundToInt(flowAmount)))
                        {
                            //...
                        }
                        break;
                    }
                }   
            }
        }
    }

    public override bool CanInteractWith(INetworkSubPart interactor, INetworkSubPart otherPart)
    {
        var interactorPVent = interactor.Parent as Comp_ANS_PassiveVent;
        var otherPVent = otherPart.Parent as Comp_ANS_PassiveVent;
        return otherPVent?.NeedsToReceiveFrom(interactorPVent) ?? false;
    }
    
    public override void NetworkPartProcessorTick(INetworkSubPart netPart)
    {
        base.NetworkPartProcessorTick(netPart);
    }
    
    //Helpers
    private bool NeedsToReceiveFrom(Comp_ANS_PassiveVent other)
    {
        return Atmos.Container.StoredPercent < other.Atmos.Container.StoredPercent;
    }

    private bool ConnectedToLowPressure()
    {
        var adjacencyList = AtmosphericComp.Network.Graph.GetAdjacencyList(AtmosphericComp);
        if (adjacencyList == null || !adjacencyList.Any()) return false;
        return adjacencyList.Any(c => c.Parent is Comp_ANS_PassiveVent pvent && pvent.NeedsToReceiveFrom(this));
    }

}

public class CompProperties_ANS_PassiveVent : CompProperties_ANS
{
    [Unsaved()]
    private List<AtmosphericDef> allowedValuesInt;
        
    //
    public AtmosphericVentMode ventMode = AtmosphericVentMode.Intake;
    public int gasThroughPut = 1;

    //Filter
    private AtmosphericVentFilter filter;
    
    //
    public List<DefValue<AtmosphericDef, float>> upkeepLevels;
    
    private class AtmosphericVentFilter
    {
        public string acceptedTag;
        public List<AtmosphericDef> acceptedAtmospheres;
    }
    
    public List<AtmosphericDef> AllowedValues
    {
        get
        {
            if (allowedValuesInt == null)
            {
                var list = new List<AtmosphericDef>();
                if (filter.acceptedTag != null)
                {
                    list.AddRange(AtmosphericReferenceCache.AtmospheresOfTag(filter.acceptedTag));
                }
                if (!filter.acceptedAtmospheres.NullOrEmpty())
                {
                    list.AddRange(filter.acceptedAtmospheres);
                }
                allowedValuesInt = list.Distinct().ToList();
            }
            return allowedValuesInt;
        }
    }
}