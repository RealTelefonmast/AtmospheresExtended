using System.Collections.Generic;
using TAE.Atmosphere.Rooms;
using TAE.AtmosphericFlow;
using TAE.Caching;
using TeleCore;
using TeleCore.Primitive;
using UnityEngine;
using Verse;

namespace TAE;

public class AtmosphericOutdoorSet
{
    private AtmosphericVolume _volume;
    
    public AtmosphericVolume Volume => _volume;
    
    public AtmosphericOutdoorSet(int cellCount)
    {
        //var alldefs = AtmosResources.AllAtmosphericDefs;
        _volume = new AtmosphericVolume();
        _volume.UpdateVolume(cellCount);
    }
}

public class AtmosphericMapInfo : MapInformation
{
    private AtmosphericCache _cache;
    private AtmosphericSystem _system;
    private AtmosphericOutdoorSet _mapVolume;
    
    private readonly AtmosphereRenderer _renderer;

    private readonly Dictionary<Room, RoomComponent_Atmosphere> _compLookUp;
    private readonly List<RoomComponent_Atmosphere> _allComps;

    private readonly List<IAtmosphericSource> _atmosphericSources;

    //Data
    private readonly List<DefFloat<AtmosphericDef>> naturalAtmospheres = new();
    
    //
    public List<RoomComponent_Atmosphere> AllAtmosphericRooms => _allComps;
    public AtmosphericCache Cache => _cache;
    public AtmosphereRenderer Renderer => _renderer;
    public AtmosphericOutdoorSet MapVolume => _mapVolume;

    public AtmosphericMapInfo(Map map) : base(map)
    {
        _cache = new AtmosphericCache(map);
        _system = new AtmosphericSystem();
        _mapVolume = new AtmosphericOutdoorSet(map.cellIndices.NumGridCells);
        
        //mapContainer = new AtmosphericContainer(null, AtmosResources.DefaultAtmosConfig(map.cellIndices.NumGridCells));
            
        //
        _compLookUp = new Dictionary<Room, RoomComponent_Atmosphere>();
        _allComps = new List<RoomComponent_Atmosphere>();
        _atmosphericSources = new List<IAtmosphericSource>();
            
        //
        _renderer = new AtmosphereRenderer(map);
    }

    public override void ExposeDataExtra()
    {
        Scribe_Deep.Look(ref _cache, "atmosCache", map);
    }

    //
    public RoomComponent_Atmosphere ComponentAt(IntVec3 pos)
    {
        var room = pos.GetRoomFast(Map);
        return ComponentAt(room);
    }

    public RoomComponent_Atmosphere ComponentAt(District district)
    {
        if (district is null) return null;
        return ComponentAt(district.Room);
    }

    public RoomComponent_Atmosphere ComponentAt(Room room)
    {
        if (room is null) return null;
        if (!_compLookUp.TryGetValue(room, out var value))
        {
            Log.Warning($"Could not find RoomComponent_Atmospheric at room {room.ID}");
            return null;
        }
        return value;
    }

    public override void InfoInit(bool initAfterReload = false)
    {
        base.InfoInit(initAfterReload);

        RegenerateMapInfo();
        PushNaturalSaturation(); //Add all natural atmospheres once
        //map.GameConditionManager.RegisterCondition(GameConditionMaker.MakeConditionPermanent(AtmosDefOf.AtmosphericCondition));
    }

    public void RegenerateMapInfo()
    {
        TLog.Message("Regenerating map info...");
        var totalCells = Map.cellIndices.NumGridCells; //AllComps.Where(c => c.IsOutdoors).Sum(c => c.Room.CellCount) 
        _mapVolume.Volume.UpdateVolume(totalCells);
    }

    //
    public override void Tick()
    {
        var tick = Find.TickManager.TicksGame;

        //Keep Natural Saturation Up
        if (tick % 750 == 0)
        {
            PushNaturalSaturation();
        }
        
        foreach (var source in _atmosphericSources)
        {
            if(!source.Thing.Spawned) continue;
            if (source.Thing.IsHashIntervalTick(source.PushInterval))
            {
                TryAddToAtmosphereFromSource(source);
            }
        }

        _system.Tick();
        _renderer.Tick();
    }

    private void PushNaturalSaturation()
    {
        GenerateNaturalAtmospheres();

        foreach (var atmosphere in naturalAtmospheres)
        {
            var storedOf = MapVolume.Volume.StoredValueOf(atmosphere.Def);
            var desired =  MapVolume.Volume.MaxCapacity * atmosphere.Value;
            var diff = desired - storedOf;
            if(diff <= 0) continue;
            //TODO: _mapVolume.Volume.TryAddValue(atmosphere.Def, diff, out _);
        }
    }
        
    private void GenerateNaturalAtmospheres()
    {
        if (!naturalAtmospheres.NullOrEmpty()) return;
            
        var extension = map.Biome.GetModExtension<TAE_BiomeExtension>();
        bool useRulesets = true;
        if (extension?.uniqueAtmospheres != null)
        {
            foreach (var atmosphere in extension.uniqueAtmospheres)
            {
                naturalAtmospheres.Add(atmosphere);
                //TODO: MapVolume.Data_RegisterSourceType(atmosphere.Def);
            }
            useRulesets = false;
        }

        if (useRulesets)
        {
            foreach (var ruleSet in DefDatabase<TAERulesetDef>.AllDefs)
            {
                if (ruleSet.Realm == AtmosphericRealm.AnyBiome)
                {
                    foreach (var floatRef in ruleSet.atmospheres)
                    {
                        naturalAtmospheres.Add(floatRef);   
                    }
                    continue;
                }

                if (ruleSet.Realm == AtmosphericRealm.SpecificBiome)
                {
                    if (ruleSet.biomes.Contains(map.Biome))
                    {
                        foreach (var atmosphere in ruleSet.atmospheres)
                        {
                            naturalAtmospheres.Add(atmosphere);
                            //TODO: mapContainer.Data_RegisterSourceType(atmosphere.Def);
                        }
                    }
                }
            }
        }

        foreach (var atmosphere in naturalAtmospheres)
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
        
    //Data
    // -- RoomComponents
    public void Notify_NewComp(RoomComponent_Atmosphere comp)
    {
        _allComps.Add(comp);
        _compLookUp.Add(comp.Room, comp);
    }

    public void Notify_DisbandedComp(RoomComponent_Atmosphere comp)
    {
        _allComps.Remove(comp);
        _compLookUp.Remove(comp.Room);
    }

    // -- Atmosphere Sources
    public void RegisterSource(IAtmosphericSource source)
    {
        if (_atmosphericSources.Contains(source)) return;
        _atmosphericSources.Add(source);
    }

    public void DeregisterSource(IAtmosphericSource source)
    {
        _atmosphericSources.Remove(source);
    }

    public override void UpdateOnGUI()
    {
    }

    public override void Update()
    {
        base.Update();
        _renderer.AtmosphereDrawerUpdate();
        _renderer.Draw();
    }
        
    //
    public void TrySpawnGasAt(IntVec3 cell, SpreadingGasTypeDef gasType, float value)
    {
        Map.GetMapInfo<SpreadingGasGrid>().Notify_SpawnGasAt(cell, gasType, value);
    }

    //
    public void Notify_ContainerFull()
    {
    }

    public void Notify_ContainerStateChanged()
    {
    }
    public void Notify_AddedContainerValue(AtmosphericDef def, float value)
    {
    }

    public void Notify_NewAtmosphericRoom(RoomComponent_Atmosphere roomComp)
    {
        
    }
    
    public void Notify_DisbandedAtmosphericRoom(RoomComponent_Atmosphere roomComp)
    {
        
    }
}