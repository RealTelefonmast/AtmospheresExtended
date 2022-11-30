using System.Collections.Generic;
using System.Linq;
using RimWorld;
using TAE.Static;
using TeleCore;
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
        _intakeCell = IntakePos(parent.Position, parent.Rotation);
        
        TLog.Message($"Vent for types: {Props.AllowedValues.ToStringSafeEnumerable()}");
    }

    internal static IntVec3 IntakePos(IntVec3 basePos, Rot4 rotation)
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
        var roomCOntainer = roomComp.CurrentContainer;
        var networkComp = AtmosphericComp;

        foreach (var atmosphericDef in Props.AllowedValues)
        {
            //Get percentage of atmosDef
            var roomValuePct = roomCOntainer.StoredPercentOf(atmosphericDef);
            var ventValuePct = networkComp.Container.StoredPercentOf(atmosphericDef.networkValue);
            var pctDiff = roomValuePct - ventValuePct;

            TLog.Debug($"(Room){roomValuePct.ToStringPercent()} - (Vent){ventValuePct.ToStringPercent()} = pctDiff: {pctDiff.ToStringPercent()}");

            switch (pctDiff)
            {
                //Push Room Into Vent
                case > 0.0078125f:
                {
                    var value = (roomCOntainer.TotalStoredOf(atmosphericDef) / Props.AllowedValues.Count) * 0.5f;
                    value = Mathf.Clamp(value, 0, networkComp.Container.Capacity / Props.AllowedValues.Count);
                    var flowAmount = value * atmosphericDef.FlowRate;
                    TLog.Debug($"Pushing Into Vent: {value} => {flowAmount} => {Mathf.Round(flowAmount)}");
                    if (roomCOntainer.TryTransferTo(networkComp.Container, atmosphericDef, Mathf.Round(flowAmount)))
                    {
                        //...
                    }

                    break;
                }
                //Push From Vent Into Room
                case < -0.0078125f:
                {
                    var value = (networkComp.Container.TotalStoredOf(atmosphericDef.networkValue)) * 0.5f;
                    value = Mathf.Clamp(value, 0, Props.gasThroughPut * tickRate);
                    var flowAmount = value * atmosphericDef.FlowRate;

                    TLog.Debug($"Pushing Into Room: {value} => {flowAmount} => {Mathf.RoundToInt(flowAmount)}");
                    if (roomCOntainer.TryReceiveFrom(networkComp.Container, atmosphericDef,
                            Mathf.RoundToInt(flowAmount)))
                    {
                        //...
                    }

                    break;
                }
            }
        }
    }

    public override void NetworkPartProcessorTick(INetworkSubPart netPart)
    {
        base.NetworkPartProcessorTick(netPart);
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