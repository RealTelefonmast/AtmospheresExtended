using System;
using System.Linq;
using RimWorld;
using TAE.AtmosphericFlow;
using TeleCore;
using TeleCore.FlowCore;
using TeleCore.Network.Flow;
using TeleCore.Network.Flow.Clamping;
using TeleCore.Primitive;
using UnityEngine;
using Verse;

namespace TAE;

public class Comp_ANS_Vent : Comp_AtmosphericNetworkStructure
{
    private CompFlickable _flickableComp;
    private IntVec3 intakeCell;

    public CompProperties_ANS_Vent VentProps => (CompProperties_ANS_Vent)base.props;

    public IntVec3 IntakeCell => intakeCell;
    public bool IntakeCellBlocked => intakeCell.GetEdifice(parent.Map) != null;

    //
    protected override Room AtmosphericSource => IntakeCell.GetRoom(parent.Map);
    
    //State Bools
    public bool CanTickNow => OwnedAtmosPart.IsReady && AtmosNetwork.IsWorking && (VentProps.passive || IsPowered);
    public virtual bool CanManipulateNow => !IntakeCellBlocked;
    
    protected bool CanWork_Obsolete
    {
        get
        {
            if (!IsPowered) return false;
            foreach (var def in VentProps.AllowedValues)
            {
                switch (VentProps.ventMode)
                {
                    case AtmosphericVentMode.Intake:
                        if (AtmosRoom.Volume.StoredValueOf(def) <= 0) return false;
                        if (OwnedAtmosPart.Volume.Full) return false;
                        break;
                    case AtmosphericVentMode.Output:
                        if (AtmosRoom.Volume.StoredValueOf(def) >= 1) return false;
                        if (OwnedAtmosPart.Volume.Empty) return false;
                        break;
                    case AtmosphericVentMode.TwoWay:
                        break;
                }
            }
            return true;
        }
    }

    
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        _flickableComp = parent.GetComp<CompFlickable>();
        intakeCell = VentProps.GetIntakePos(parent.Position, parent.Rotation);
        //TLog.Debug($"Created vent with allowed values: {VentProps.AllowedValues.ToStringSafeEnumerable()} and\n{AtmosNetwork.Props.containerConfig.AllowedValues}");
    }
    
    public double NextFlow { get; set; } = 0;
    public double PrevFlow { get; set; } = 0;
    public double Move { get; set; } = 0;
    public double FlowRate { get; set; }
    
    public DefValueStack<NetworkValueDef, double> PrevStackNetwork { get; set; }
    public DefValueStack<AtmosphericValueDef, double> PrevStackAtmos { get; set; }
    
    public override void CompTick()
    {
        base.CompTick();
        if (!CanTickNow || !CanManipulateNow) return;
        
        //Begin Exchange
        var selfVolume = this.OwnedAtmosPart.Volume;
        var roomVolume = this.AtmosRoom.Volume;

        //Setup
        PrevStackNetwork = selfVolume.Stack;
        PrevStackAtmos = roomVolume.Stack;
            
            
        //Flow
        bool? fromTo = null;
        //Equalize Based On Simple Pressure And Clamping
        double flow = NextFlow;
        flow = FlowFunc(selfVolume, roomVolume, flow);
        fromTo = flow switch
        {
            < 0 => false,
            > 0 => true,
            _ => fromTo
        };
        if (fromTo == null) return;
            
        flow = Math.Abs(flow);
        NextFlow = ClampFunc(selfVolume, roomVolume, flow, fromTo.Value);
        Move = ClampFunc(selfVolume, roomVolume, flow, fromTo.Value);

        //Update Content
        if (fromTo.Value)
        {
            DefValueStack<NetworkValueDef, double> res = selfVolume.RemoveContent(Move);
            var res2 = new DefValueStack<AtmosphericValueDef, double>();
            for(var i = 0; i < res.Length; i++)
            {
                var def = res[i].Def;
                var atmosDef = AtmosRoom.Volume.AllowedValues.FirstOrDefault(d => d.networkValue == def);
                if (atmosDef != null)
                {
                    res2 += new DefValue<AtmosphericValueDef, double>(atmosDef, res[i].Value);
                }
            }
            roomVolume.AddContent(res2);
        }
        else
        {
            DefValueStack<AtmosphericValueDef, double> res = roomVolume.RemoveContent(Move);
            var res2 = new DefValueStack<NetworkValueDef, double>();
            for(var i = 0; i < res.Length; i++)
            {
                var def = res[i].Def;
                if (def.networkValue != null)
                {
                    res2 += new DefValue<NetworkValueDef, double>(def.networkValue, res[i].Value);
                }
            }
            selfVolume.AddContent(res2);
        }
    }
    
    public static double Pressure<T>(FlowVolume<T> volume) where T : FlowValueDef
    {
        if (volume.MaxCapacity <= 0)
        {
            TLog.Warning($"Tried to get pressure from container with {volume.MaxCapacity} capacity!");
            return 0;
        }
        return volume.TotalValue / volume.MaxCapacity * 100d;
    }
    
    //Note: Friction is key!!
    public static double Friction => 0;
    public static double CSquared => 0.5;
    public static double DampFriction => 0.01; //TODO: Extract into global flowsystem config xml or mod settings

    
    public double FlowFunc(NetworkVolume network, AtmosphericVolume atmos, double f)
    {
        var dp = Pressure(network) - Pressure(atmos);
        return f > 0 ? HandleNetworkSource(network, f, dp) : HandleAtmosSource(atmos, f, dp);
    }

    private double HandleNetworkSource(NetworkVolume src, double f, double dp)
    {
        var dc = Math.Max(0, PrevStackNetwork.TotalValue - src.TotalValue);
        f += dp * CSquared;
        f *= 1 - Friction;
        //f *= 1 - Math.Min(0.5, DampFriction * dc);
        return f;
    }
    
    private double HandleAtmosSource(AtmosphericVolume src, double f, double dp)
    {
        var dc = Math.Max(0, PrevStackAtmos.TotalValue - src.TotalValue);
        f += dp * CSquared;
        f *= 1 - Friction;
        f *= 1 - Math.Min(0.5, DampFriction * dc);
        return f;
    }

    public double ClampFunc(NetworkVolume network, AtmosphericVolume atmos, double f, bool fromTo)
    {
        double totalFrom = fromTo ? network.TotalValue : atmos.TotalValue ;
        double maxCapFrom = fromTo ? network.MaxCapacity : atmos.MaxCapacity;
        double totalTo = fromTo ? atmos.TotalValue : network.TotalValue;
        double maxCapTo = fromTo ? atmos.MaxCapacity : network.MaxCapacity;
        
        var limit = 0.5;
        double c, r;
        if (f > 0)
        {
            c = totalFrom;
            f = ClampFlow(c, f, limit * c);
        }
        
        else if (f < 0)
        {
            c = totalTo;
            f = -ClampFlow(c, -f, limit* c);
        }


        if (f > 0)
        {
            r = maxCapTo - totalTo;
            f = ClampFlow(r, f, limit * r);
        }
        else if (f < 0)
        {
            r = maxCapFrom - totalFrom;
            f = -ClampFlow(r, -f, limit * r);
        }
        return f;

        static double ClampFlow(double content, double flow, double limit)
        {
            if (content <= 0) return 0;

            if (flow >= 0) return flow <= limit ? flow : limit;
            return flow >= -limit ? flow : -limit;
        }
    }


    /*private void TryEqualize(AtmosphericDef atmosphericDef, int tickRate = 1)
    {
        var roomComp = AtmosRoom;
        var roomContainer = roomComp.CurrentContainer;
        var networkComp = AtmosNetwork;
        
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
                    var flowAmount = networkComp.Container.GetMaxTransferRate(atmosphericDef.networkValue,
                        Mathf.CeilToInt(value * diffPct * atmosphericDef.networkValue.FlowRate));
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
                    if (IsAtmosphericProvider()) return;

                    var value = (networkComp.Container.StoredValueOf(atmosphericDef.networkValue)) * 0.5f;
                    var flowAmount = networkComp.Container.GetMaxTransferRate(atmosphericDef.networkValue,
                        Mathf.CeilToInt(value * diffPct * atmosphericDef.networkValue.FlowRate));
                    if (roomContainer.TryReceiveFrom(networkComp.Container, atmosphericDef,
                            Mathf.RoundToInt(flowAmount)))
                    {
                        //...
                    }

                    break;
                }
            }
        }
    }*/

    /*private bool TryManipulateAtmosphere(int tick)
    {
        int totalThroughput = VentProps.gasThroughPut * tick;
        foreach (var def in VentProps.AllowedValues)
        {
            switch (VentProps.ventMode)
            {
                //Pull atmosphere into the container
                case AtmosphericVentMode.Intake:
                    if (AtmosRoom.Container.TryTransferTo(AtmosNetwork.Container, def, totalThroughput))
                    {
                        return true;
                    }

                    break;
                //Push atmosphere into the room
                case AtmosphericVentMode.Output:
                    if(def.networkValue == null) continue;
                    if (AtmosNetwork.Container.TryConsume(def.networkValue, totalThroughput))
                    {
                        //Atmospheric.RoomContainer.TryAddValue(def, 1, out _);
                        ValueResult<AtmosphericDef> result;
                        if (def.dissipationGasDef != null)
                        {
                            parent.Map.GetMapInfo<AtmosphericMapInfo>().TrySpawnGasAt(parent.Position,
                                def.dissipationGasDef, totalThroughput * def.dissipationGasDef.maxDensityPerCell);
                            result = ValueResult<AtmosphericDef>.Init(totalThroughput, def).Complete(totalThroughput);
                        }
                        else
                        {
                            result = AtmosRoom.Container.TryAddValue(def, totalThroughput);
                        }
                        return result;
                    }
                    break;
                case AtmosphericVentMode.TwoWay:
                    TryEqualize(def, tick);
                    break;
            }
        }
        return false;
    }*/
    
    //Helpers
    //Pressure check hack, if other container has higher room pressure it can send to this current vent
    private bool NeedsToReceiveFrom(Comp_AtmosphericNetworkStructure other)
    {
        return VentProps.ventMode switch
        {
            AtmosphericVentMode.Intake => false,
            AtmosphericVentMode.Output => true,
            AtmosphericVentMode.TwoWay => AtmosRoom.Volume.FillPercent < other.AtmosRoom.Volume.FillPercent,
            _ => false
        };
    }

    //Check whether we have vent neighbours that can receive
    private bool IsAtmosphericProvider()
    {
        var adjacencyList = OwnedAtmosPart.Network.Graph.GetAdjacencyList(OwnedAtmosPart);
        if (adjacencyList == null || !adjacencyList.Any()) return false;
        return adjacencyList.Any(c => c.Item2.Value.Parent is Comp_ANS_Vent pvent && pvent.NeedsToReceiveFrom(this));
    }
}