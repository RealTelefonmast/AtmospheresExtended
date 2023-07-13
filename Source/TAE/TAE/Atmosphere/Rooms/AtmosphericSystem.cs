using System;
using System.Collections.Generic;
using System.Linq;
using TAE.AtmosphericFlow;
using TeleCore;
using TeleCore.Primitive;
using Verse;

namespace TAE.Atmosphere.Rooms;

public class AtmosphericSystem
{
    //Map
    private AtmosphericVolume _mapVolume;
    private readonly List<DefValue<AtmosphericDef, float>> _naturalAtmospheres = new();
    private readonly List<IAtmosphericSource> _atmosphericSources;
    
    //Rooms
    private List<AtmosphericVolume> _volumes;
    private Dictionary<RoomComponent, AtmosphericVolume> _relations;
    private Dictionary<AtmosphericVolume, List<AtmosInterface>> _connections;
    //Note: Interface represents a portal between rooms
    
    public AtmosphericVolume MapVolume => _mapVolume;
    public Dictionary<RoomComponent, AtmosphericVolume> Relations => _relations;

    public AtmosphericSystem(int mapCellSize)
    {
        //
        _mapVolume = new AtmosphericVolume();
        _naturalAtmospheres = new List<DefValue<AtmosphericDef, float>>();
        _atmosphericSources = new List<IAtmosphericSource>();
        Notify_Regenerate(mapCellSize);
        
        //
        _volumes = new List<AtmosphericVolume>();
        _relations = new Dictionary<RoomComponent, AtmosphericVolume>();
        _connections = new Dictionary<AtmosphericVolume, List<AtmosInterface>>();
        
        //Prepare Map Volume
        _volumes.Add(_mapVolume);
        _connections.Add(_mapVolume, new List<AtmosInterface>());
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
        if (_relations.TryGetValue(comp, out var volume))
        {
            volume.UpdateVolume(comp.Room.CellCount);
        }
    }
    
    public void Notify_AddRoomComp(RoomComponent_Atmosphere comp)
    {
        if (_relations.ContainsKey(comp)) return;
        
        if (comp.IsOutdoors)
        {
            _relations.Add(comp, _mapVolume);
            foreach (var adjComp in comp.AdjRoomComps)
            {
                if (!_relations.TryGetValue(adjComp, out var adjVolume)) continue;
                var conn = new AtmosInterface(_mapVolume, adjVolume);
                _connections[_mapVolume].Add(conn);
            }
            return;
        }
        
        TLog.Debug($"Adding room {comp.Room.ID} to system relations...");
        var volume = new AtmosphericVolume();
        volume.UpdateVolume(comp.Room.CellCount);
        _volumes.Add(volume);
        _relations.Add(comp, volume);
        _connections.Add(volume, new List<AtmosInterface>());

        foreach (var adjComp in comp.AdjRoomComps)
        {
            if (!_relations.TryGetValue(adjComp, out var adjVolume)) continue;
            var conn = new AtmosInterface(volume, adjVolume);
            _connections[volume].Add(conn);
        }
    }

    public void Notify_RemoveRoomComp(RoomComponent_Atmosphere comp)
    {
        if (!_relations.ContainsKey(comp)) return;

        if (comp.IsOutdoors)
        {
            //TODO: Needs serious testing
            _relations.Remove(comp);

            bool Match(AtmosInterface iface)
            {
                var firstMatch = _relations.FirstOrDefault(c => c.Value == iface.To);
                if (firstMatch.Key == null) return false;
                var contains = firstMatch.Key.AdjRoomComps.Contains(comp);
                return contains;
            }

            _connections[_mapVolume].RemoveAll(Match); 
            return;
        }
        
        var volume = _relations[comp];
        _volumes.Remove(volume);
        _relations.Remove(comp);
        _connections.Remove(volume);
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
    
    public void Notify_Regenerate(int cells)
    {
        _mapVolume.UpdateVolume(cells);
    }
    
    public void Tick(int tick)
    {
        PreTickProcessor(tick);

        foreach (var volume in _volumes)
        {
            volume.PrevStack = volume.Stack;
            _connections[volume].ForEach(c => c.Notify_SetDirty());
        }

        foreach (var volume in _volumes)
        {
            foreach (var conn in _connections[volume])
            {
                if(conn.ResolvedFlow) continue;
                var flow = conn.NextFlow;
                flow = FlowFunc(conn.From, conn.To, flow);
                conn.NextFlow = ClampFunc(conn.From, conn.To, flow);
                conn.Move = ClampFunc(conn.From, conn.To, flow);
                conn.Notify_ResolvedFlow();
            }
        }

        foreach (var volume in _volumes)
        {
            for (var i = 0; i < _connections[volume].Count; i++)
            {
                var conn = _connections[volume][i];
                if(conn.ResolvedMove) continue;
                var res = conn.To.RemoveContent(conn.Move);
                volume.AddContent(res);
                conn.Notify_ResolvedMove();

                //TODO: Structify for: _connections[fb][i] = conn;
            }
        }

        foreach (var volume in _volumes)
        {
            double fp = 0;
            double fn = 0;

            foreach (var conn in _connections[volume])
            {
                Add(conn.Move);
            }

            volume.FlowRate = Math.Max(fp, fn);
            continue;

            void Add(double f)
            {
                if (f > 0)
                    fp += f;
                else
                    fn -= f;
            }
        }
    }

    private void PreTickProcessor(int tick)
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
    
    public double Friction => 0;
    public double CSquared => 0.03;
    public double DampFriction => 0.01; //TODO: Extract into global flowsystem config xml or mod settings

    //TODO:Adjust flow based on gas/fluid properties
    private double FlowFunc(AtmosphericVolume from, AtmosphericVolume to, double f)
    {
        var dp = Pressure(from) - Pressure(to); // pressure differential
        var src = f > 0 ? from : to;
        var dc = Math.Max(0, src.PrevStack.TotalValue - src.TotalValue);
        f += dp * CSquared;
        f *= 1 - GetTotalFriction(src); //Note: Might be unnecessary slow-down
        f *= 1 - Math.Min(0.5, DampFriction * dc);
        return f;
    }

    private static double GetTotalFriction(AtmosphericVolume volume)
    {
        double totalFriction = 0;
        double totalVolume = 0;
            
        foreach (var fluid in volume.Stack)
        {
            totalFriction += fluid.Def.friction * fluid.Value;
            totalVolume += fluid.Value;
        }

        if (totalVolume == 0) return 0;
    
        var averageFriction = totalFriction / totalVolume;
        return averageFriction;
    }

    private static double Pressure(AtmosphericVolume volume)
    {
        if (volume.MaxCapacity <= 0)
        {
            TLog.Warning($"Tried to get pressure from container with {volume.MaxCapacity} capacity!");
            return 0;
        }
        return volume.TotalValue / volume.MaxCapacity * 100d;
    }

    private static double ClampFlow(double content, double flow, double limit)
    {
        if (content <= 0) return 0;

        if (flow >= 0) return flow <= limit ? flow : limit;
        return flow >= -limit ? flow : -limit;
    }

    private double ClampFunc(AtmosphericVolume from, AtmosphericVolume to, double f, bool enforceMinPipe = true,
        bool enforceMaxPipe = true)
    {
        var d0 = 1d / Math.Max(1, _connections[from].Count);
        var d1 = 1d / Math.Max(1, _connections[to].Count);

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
}