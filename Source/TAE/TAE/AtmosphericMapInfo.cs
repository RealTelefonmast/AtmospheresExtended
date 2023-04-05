﻿using System.Collections.Generic;
using TAE.Caching;
using TeleCore;
using TeleCore.FlowCore;
using UnityEngine;
using Verse;

namespace TAE
{
    public class AtmosphericMapInfo : MapInformation, IContainerHolderRoom<AtmosphericDef>, IContainerImplementer<AtmosphericDef, IContainerHolderRoom<AtmosphericDef>, AtmosphericContainer>
    {
        //
        private AtmosphericCache _cache;
        private readonly AtmosphericContainer mapContainer;
        private readonly AtmosphereRenderer renderer;

        private readonly Dictionary<Room, RoomComponent_Atmospheric> compByRoom;
        private readonly List<RoomComponent_Atmospheric> allComps;

        private readonly List<IAtmosphericSource> allSources;

        //Data
        private readonly List<DefFloat<AtmosphericDef>> naturalAtmospheres = new();
        private readonly List<SkyOverlay> naturalOverlays = new();

        private readonly List<AtmosphericPortal> allConnections;

        private AtmosphericContainer container;
        //private List<AtmosphericPortal> allConnectionsToOutside;

        //public int TotalMapPollution => OutsideContainer.Atmospheric + AllComps.Sum(c => c.PollutionContainer.Atmospheric);

        //
        public AtmosphericContainer MapContainer => mapContainer;
        public List<RoomComponent_Atmospheric> AllAtmosphericRooms => allComps;
        public AtmosphericCache Cache => _cache;
        public int ConnectorCount => allConnections.Count; //{ get; set; }
        public AtmosphereRenderer Renderer => renderer;
        
        public Room Room { get; }
        public RoomComponent RoomComponent { get; }
        
        public string ContainerTitle { get; }
        public AtmosphericContainer Container => container;
        
        //Container
        public void Notify_ContainerStateChanged(NotifyContainerChangedArgs<AtmosphericDef> args)
        {
            
        }

        public AtmosphericMapInfo(Map map) : base(map)
        {
            _cache = new AtmosphericCache(map);
            mapContainer = new AtmosphericContainer(null, AtmosResources.DefaultAtmosConfig(map.cellIndices.NumGridCells));
            
            //
            compByRoom = new Dictionary<Room, RoomComponent_Atmospheric>();
            allComps = new List<RoomComponent_Atmospheric>();

            allSources = new List<IAtmosphericSource>();
            allConnections = new List<AtmosphericPortal>();
            //allConnectionsToOutside = new List<AtmosphericPortal>();
            
            //
            renderer = new AtmosphereRenderer(map);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref _cache, "atmosCache", map);
        }

        //
        public RoomComponent_Atmospheric ComponentAt(IntVec3 pos)
        {
            var room = pos.GetRoomFast(Map);
            return ComponentAt(room);
        }

        public RoomComponent_Atmospheric ComponentAt(District district)
        {
            if (district is null) return null;
            return ComponentAt(district.Room);
        }

        public RoomComponent_Atmospheric ComponentAt(Room room)
        {
            if (room is null) return null;
            if (!compByRoom.TryGetValue(room, out var value))
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
            MapContainer.Notify_RoomChanged(null, totalCells);
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

            //Equalize between rooms
            if (tick % 10 == 0)
            {
                foreach (var connector in allConnections)
                {
                    connector.TryEqualize();
                }
            }

            foreach (var source in allSources)
            {
                if(!source.Thing.Spawned) continue;
                if (source.Thing.IsHashIntervalTick(source.PushInterval))
                {
                    TryAddToAtmosphereFromSource(source);
                }
            }

            foreach (var overlay in naturalOverlays)
            {
                overlay.OverlayColor = naturalAtmospheres[0].Def.valueColor;
                overlay.TickOverlay(map);
            }
        }

        private void PushNaturalSaturation()
        {
            //
            GenerateNaturalAtmospheres();

            //
            foreach (var atmosphere in naturalAtmospheres)
            {
                var storedOf = MapContainer.StoredValueOf(atmosphere.Def);
                var desired = MapContainer.Capacity * atmosphere.Value;
                var diff = Mathf.Round(desired - storedOf);
                if(diff <= 0) continue;
                MapContainer.TryAddValue(atmosphere.Def, diff, out _);
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
                    mapContainer.Data_RegisterSourceType(atmosphere.Def);
                }
                useRulesets = false;
            }

            if (useRulesets)
            {
                foreach (var ruleSet in DefDatabase<TAERulesetDef>.AllDefs)
                {
                    if (ruleSet.occurence == AtmosphericOccurence.AnyBiome)
                    {
                        foreach (var floatRef in ruleSet.atmospheres)
                        {
                            naturalAtmospheres.Add(floatRef);   
                        }
                        continue;
                    }

                    if (ruleSet.occurence == AtmosphericOccurence.SpecificBiome)
                    {
                        if (ruleSet.biomes.Contains(map.Biome))
                        {
                            foreach (var atmosphere in ruleSet.atmospheres)
                            {
                                naturalAtmospheres.Add(atmosphere);
                                mapContainer.Data_RegisterSourceType(atmosphere.Def);
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

                        naturalOverlays.Add(newOverlay);
                    });
                }
            }
        }

        private void TryAddToAtmosphereFromSource(IAtmosphericSource source)
        {
            if (!source.IsActive) return;
            if (compByRoom[source.Room].TryAddValueToRoom(source.AtmosphericDef, source.PushAmount, out _))
            {
                //TODO: effect on source...
            }

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
        public void Notify_NewComp(RoomComponent_Atmospheric comp)
        {
            allComps.Add(comp);
            compByRoom.Add(comp.Room, comp);
        }

        public void Notify_DisbandedComp(RoomComponent_Atmospheric comp)
        {
            allComps.Remove(comp);
            compByRoom.Remove(comp.Room);

            //Remove Portals - check for validity after despawning
            LongTickHandler.EnqueueActionForMainThread(delegate
            {
                allConnections.RemoveAll(p => !p.IsValid); 
            });
        }

        // -- Atmosphere Sources
        public void RegisterSource(IAtmosphericSource source)
        {
            if (allSources.Contains(source)) return;
            allSources.Add(source);
        }

        public void DeregisterSource(IAtmosphericSource source)
        {
            allSources.Remove(source);
        }

        // -- Portal Data
        public void Notify_NewPortal(AtmosphericPortal connection)
        {
            if (allConnections.Any(p => p.Thing == connection.Thing))
            {
                return;
            }
            allConnections.Add(connection);
        }

        public override void UpdateOnGUI()
        {
        }

        public override void Update()
        {
            base.Update();
            /*
            if (!allConnections.NullOrEmpty())
            {
                List<IntVec3> list = new List<IntVec3>();
                foreach (var p in allConnections)
                {
                    if(p?.Thing != null)
                        list.Add(p.Thing.Position);
                }

                GenDraw.DrawFieldEdges(list, Color.blue);
            }
            */

            //
            renderer.AtmosphereDrawerUpdate();
        }

        public void DrawSkyOverlays()
        {
            if (naturalOverlays.NullOrEmpty()) return;
            for (var i = 0; i < naturalOverlays.Count; i++)
            {
                naturalOverlays[i].DrawOverlay(map);
            }
        }
        
        //
        public bool TrySpawnGasAt(IntVec3 cell, SpreadingGasTypeDef gasType, float value)
        {
            Map.GetMapInfo<SpreadingGasGrid>().Notify_SpawnGasAt(cell, gasType, value);
            return false;
            /*
               if (!ComponentAt(cell).CanHaveTangibleGas) return false;
               if (cell.GetGas(Map) is SpreadingGas existingGas)
               {
                   existingGas.AdjustSaturation(value, out _);
                   return true;
               }
               ((SpreadingGas)GenSpawn.Spawn(def, cell, Map)).AdjustSaturation(value, out _);
               return true;
           */
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
    }
}
