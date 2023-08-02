using System;
using System.Collections.Generic;
using System.Linq;
using TAE.AtmosphericFlow;
using TeleCore;
using TeleCore.FlowCore;
using TeleCore.Network.Data;
using TeleCore.Network.Flow;
using TeleCore.Network.Flow.Clamping;
using TeleCore.Primitive;
using Verse;

namespace TAE.Atmosphere.Rooms;

//TODO: Add a visual readout of a graph between all roomcomps via interfaces
//TODO: Like those burst graphs

public class AtmosphericSystem : FlowSystem<RoomComponent, AtmosphericVolume, AtmosphericValueDef>
{
    private AtmosphericVolume _mapVolume;
    private readonly List<DefValue<AtmosphericValueDef, float>> _naturalAtmospheres = new();
    private readonly List<IAtmosphericSource> _atmosphericSources;
    //Note: Interface represents a portal between rooms
    
    public AtmosphericVolume MapVolume => _mapVolume;
    
    //Note: Friction is key!!
    public static double Friction => 0.01;
    public static double CSquared => 0.03;
    public static double DampFriction => 0.01; //TODO: Extract into global flowsystem config xml or mod settings
    
    public AtmosphericSystem(int mapCellSize)
    {
        _mapVolume = new AtmosphericVolume(AtmosResources.DefaultAtmosConfig(mapCellSize));
        _naturalAtmospheres = new List<DefValue<AtmosphericValueDef, float>>();
        _atmosphericSources = new List<IAtmosphericSource>();
        Notify_Regenerate(mapCellSize);
        
        AddVolume(_mapVolume);
    }
    
    public AtmosphericSystem(Map map) : this(map.cellIndices.NumGridCells)
    {
    }
    
    public void Init(Map map)
    {
        GenerateNaturalAtmospheres(map);
        PushNaturalSaturation(); //Add all natural atmospheres once
    }
    
    public void Notify_UpdateRoomComp(RoomComponent_Atmosphere comp)
    {
        if (Relations.TryGetValue(comp, out var volume))
        {
            volume.UpdateVolume(comp.Room.CellCount);
        }
    }
    
    protected override AtmosphericVolume CreateVolume(RoomComponent part)
    {
        return new AtmosphericVolume(AtmosResources.DefaultAtmosConfig(part.Room.CellCount));
    }
    
    public void Notify_AddRoomComp(RoomComponent_Atmosphere comp)
    {
        if (Relations.ContainsKey(comp))
        {
            TLog.Error("This technically shouldn't happen");
            return;
        }

        if (comp.IsOutdoors)
        {
            foreach (var adjComp in comp.CompNeighbors.Neighbors)
            {
                if (!Relations.TryGetValue(adjComp, out var adjVolume)) continue;
                var conn = new FlowInterface<AtmosphericVolume, AtmosphericValueDef>(_mapVolume, adjVolume);
                AddConnection(_mapVolume, conn);
                AddInterface((comp, adjComp), conn);
            }
            return;
        }
        
        TLog.Debug($"Adding room {comp.Room.ID} to system relations...");
        var volume = new AtmosphericVolume(AtmosResources.DefaultAtmosConfig(comp.Room.CellCount));
        volume.UpdateVolume(comp.Room.CellCount);
        AddVolume(volume);
        foreach (var adjComp in comp.CompNeighbors.Neighbors)
        {
            if (!Relations.TryGetValue(adjComp, out var adjVolume)) continue;
            var conn = new FlowInterface<AtmosphericVolume, AtmosphericValueDef>(volume, adjVolume);
            AddConnection(volume, conn);
            AddInterface((comp, adjComp), conn);
        }
    }

    public void Notify_RemoveRoomComp(RoomComponent_Atmosphere comp)
    {
        if (!Relations.ContainsKey(comp)) return;

        if (comp.IsOutdoors)
        {
            //TODO: Needs serious testing
            Relations.Remove(comp);

            bool Match(FlowInterface<AtmosphericVolume, AtmosphericValueDef> iface)
            {
                var firstMatch = Relations.FirstOrDefault(c => c.Value == iface.To);
                if (firstMatch.Key == null) return false;
                var contains = firstMatch.Key.CompNeighbors.Neighbors.Contains(comp);
                return contains;
            }
            
            RemoveInterfacesWhere(Match);
            return;
        }
        
        var volume = Relations[comp];
        RemoveVolume(volume);
        RemoveRelation(comp);
        RemoveInterfacesWhere(t => t.From == volume || t.To == volume);
    }
    
    public void Notify_AddSource(IAtmosphericSource source)
    {
        if (_atmosphericSources.Contains(source)) return;
        _atmosphericSources.Add(source);
    }
    
    public void Notify_RemoveSource(IAtmosphericSource source)
    {
        _atmosphericSources.Remove(source);
    }
    

    #region Ticking
    
    protected override void PreTickProcessor(int tick)
    {
        //Keep Natural Saturation Up
        if (tick % 750 == 0)
        {
            PushNaturalSaturation();
        }

        foreach (var source in _atmosphericSources)
        {
            if (!source.Thing.Spawned) continue;
            if (source.Thing.IsHashIntervalTick(source.PushInterval))
            {
                TryAddToAtmosphereFromSource(source);
            }
        }
    }

    #endregion
    
    public void Notify_Regenerate(int cells)
    {
        _mapVolume.UpdateVolume(cells);
    }
    
    public override double FlowFunc(FlowInterface<AtmosphericVolume, AtmosphericValueDef> iface, double f)
    {
        var from = iface.From;
        var to = iface.To;
        
        var dp = Pressure(from) - Pressure(to); // pressure differential
        var src = f > 0 ? from : to;
        var dc = Math.Max(0, src.PrevStack.TotalValue - src.TotalValue);
        f += dp * CSquared;
        f *= 1 - Friction;
        f *= 1 - GetTotalFriction(src); //Additional Friction from each fluid/gas
        f *= 1 - Math.Min(0.5, DampFriction * dc);

        //f += dp * CSquared;
        //f *= 1 - GetTotalFriction(src); //Note: Might be unnecessary slow-down
        //f *= 1 - Math.Min(0.5, DampFriction * dc);
        return f;
    }

    private bool enforceMinPipe = true;
    private bool enforceMaxPipe = true;
    
    public override double ClampFunc(FlowInterface<AtmosphericVolume, AtmosphericValueDef> iface, double f, ClampType clampType)
    {     
        var from = iface.From;
        var to = iface.To;
        
        var d0 = 1d / Math.Max(1, Connections[from].Count);
        var d1 = 1d / Math.Max(1, Connections[to].Count);

        if (enforceMinPipe)
        {
            double c;
            if (f > 0)
            {
                c = from.TotalValue;
                f = ClampFlow(c, f, d0 * c);
            }
            else if (f < 0)
            {
                c = to.TotalValue;
                f = -ClampFlow(c, -f, d1 * c);
            }
        }

        if (enforceMaxPipe)
        {
            double r;
            if (f > 0)
            {
                r = to.MaxCapacity - to.TotalValue;
                f = ClampFlow(r, f, d1 * r);
            }
            else if (f < 0)
            {
                r = from.MaxCapacity - from.TotalValue;
                f = -ClampFlow(r, -f, d0 * r);
            }
        }

        return f;
    }
    
    public static double Pressure(AtmosphericVolume volume)
    {
        if (volume.MaxCapacity <= 0)
        {
            TLog.Warning($"Tried to get pressure from container with {volume.MaxCapacity} capacity!");
            return 0;
        }
        return volume.TotalValue / volume.MaxCapacity * 100d;
    }

    public static double ClampFlow(double content, double flow, double limit)
    {
        if (content <= 0) return 0;

        if (flow >= 0) return flow <= limit ? flow : limit;
        return flow >= -limit ? flow : -limit;
    }
    
    public static double GetTotalFriction(AtmosphericVolume volume)
    {
        double totalFriction = 0;
        double totalVolume = 0;
        
        if (!volume.Stack.IsValid) return 0;
        foreach (var fluid in volume.Stack)
        {
            totalFriction += fluid.Def.friction * fluid.Value;
            totalVolume += fluid.Value;
        }

        if (totalVolume == 0) return 0;
    
        var averageFriction = totalFriction / totalVolume;
        return averageFriction;
    }

    #region Atmospheric

    private void PushNaturalSaturation()
    {
        foreach (var atmosphere in _naturalAtmospheres)
        {
            var storedOf = _mapVolume.StoredValueOf(atmosphere.Def);
            var desired = _mapVolume.MaxCapacity * atmosphere.Value;
            var diff = desired - storedOf;
            if (diff <= 0) continue;
            _mapVolume.TryAdd(atmosphere, diff);
        }
    }
    
    //Note: Called once to generate all natural atmospheres
    private void GenerateNaturalAtmospheres(Map map)
    {
        if (!_naturalAtmospheres.NullOrEmpty()) return;

        var extension = map.Biome.GetModExtension<TAE_BiomeExtension>();
        var useRulesets = true;
        if (extension?.uniqueAtmospheres != null)
        {
            foreach (var atmosphere in extension.uniqueAtmospheres)
            {
                _naturalAtmospheres.Add(atmosphere);
                //TODO: MapVolume.Data_RegisterSourceType(atmosphere.Def);
            }

            useRulesets = false;
        }

        if (useRulesets)
        {
            foreach (var ruleSet in DefDatabase<TAERulesetDef>.AllDefs)
            {
                if (ruleSet.realm == AtmosphericRealm.AnyBiome)
                {
                    if (ruleSet.atmospheres != null)
                    {
                        foreach (var floatRef in ruleSet.atmospheres)
                        {
                            _naturalAtmospheres.Add(floatRef);
                        }   
                    }
                    continue;
                }

                if (ruleSet.realm == AtmosphericRealm.SpecificBiome)
                {
                    if (ruleSet.biomes != null)
                    {
                        if (ruleSet.biomes.Contains(map.Biome))
                        {
                            foreach (var atmosphere in ruleSet.atmospheres)
                            {
                                _naturalAtmospheres.Add(atmosphere);
                                //TODO: mapContainer.Data_RegisterSourceType(atmosphere.Def);
                            }
                        }
                    }
                }
            }
        }

        foreach (var atmosphere in _naturalAtmospheres)
        {
            if (atmosphere.Def.naturalOverlay != null)
            {
                TLog.Debug($"Adding Natural Overlay: {atmosphere.Def}");
                TeleUpdateManager.Notify_EnqueueNewSingleAction(() =>
                {
                    var newOverlay = new SkyOverlay_Atmosphere(atmosphere.Def.naturalOverlay);

                    //TODO: naturalOverlays.Add(newOverlay);
                });
            }
        }
    }

    private void TryAddToAtmosphereFromSource(IAtmosphericSource source)
    {
        if (!source.IsActive) return;
        //TODO: 
        // if (_compLookUp[source.Room].TryAddValueToRoom(source.AtmosphericDef, source.PushAmount, out _))
        // {
        //     //TODO: effect on source...
        // }

        /*
        if (Pollution != lastPollutionInt)
        {
            GameCondition_TiberiumBiome mainCondition = (GameCondition_TiberiumBiome)map.GameConditionManager.GetActiveCondition(TiberiumDefOf.TiberiumBiome);
            if (mainCondition == null)
            {
                GameCondition condition = GameConditionMaker.MakeCondition(TiberiumDefOf.TiberiumBiome);
                condition.conditionCauser = TRUtils.Tiberium().GroundZeroInfo.GroundZero;
                condition.Permanent = true;
                mainCondition = (GameCondition_TiberiumBiome)condition;
                map.GameConditionManager.RegisterCondition(condition);
                Log.Message("Adding game condition..");
            }

            if (!mainCondition.AffectedMaps.Contains(this.map))
            {
                mainCondition.AffectedMaps.Add(map);
                Log.Message("Adding map to game condition..");
            }
            //mainCondition.Notify_PollutionChange(map, OutsideContainer.Saturation);
        }

        lastPollutionInt = Pollution;
        */
    }
    
    #endregion
}