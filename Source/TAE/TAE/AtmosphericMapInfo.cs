using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAE.Caching;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE
{
    public class AtmosphericMapInfo : MapInformation
    {
        //
        internal static int CELL_CAPACITY = 100;

        //
        private AtmosphericCache _cache;
        private AtmosphericContainer mapContainer;

        private Dictionary<Room, RoomComponent_Atmospheric> compByRoom;
        private List<RoomComponent_Atmospheric> allComps;

        private List<IAtmosphericSource> allSources;

        //Data
        private static List<DefFloat<AtmosphericDef>> naturalAtmospheres = new();

        private List<AtmosphericPortal> allConnections;
        private List<AtmosphericPortal> allConnectionsToOutside;

        //public int TotalMapPollution => OutsideContainer.Atmospheric + AllComps.Sum(c => c.PollutionContainer.Atmospheric);

        //
        public AtmosphericContainer MapContainer => mapContainer;
        public List<RoomComponent_Atmospheric> AllAtmosphericRooms => allComps;
        public AtmosphericCache Cache => _cache;

        public AtmosphericMapInfo(Map map) : base(map)
        {
            _cache = new AtmosphericCache(map);
            mapContainer = new AtmosphericContainer(null);

            //
            compByRoom = new Dictionary<Room, RoomComponent_Atmospheric>();
            allComps = new List<RoomComponent_Atmospheric>();

            allSources = new List<IAtmosphericSource>();
            allConnections = new List<AtmosphericPortal>();
            allConnectionsToOutside = new List<AtmosphericPortal>();
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
        }

        public void RegenerateMapInfo()
        {
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
                /*
                foreach (var pollutionComp in AllComps)
                {
                    pollutionComp.Equalize();
                }
                */

                foreach (var connector in allConnections)
                {
                    connector.TryEqualize();
                }
            }

            foreach (var source in allSources)
            {
                if(!source.Thing.Spawned) continue;
                if (source.Thing.IsHashIntervalTick(source.CreationInterval))
                {
                    TryAddToAtmosphere(source);
                }
            }
        }

        private void PushNaturalSaturation()
        {
            //
            if (naturalAtmospheres.NullOrEmpty())
            {
                var extension = map.Biome.GetModExtension<AtmosphereBiomeExtension>();
                if (extension?.uniqueAtmospheres != null)
                {
                    foreach (var atmosphere in extension.uniqueAtmospheres)
                    {
                        naturalAtmospheres.Add(atmosphere);
                    }
                    return;
                }

                foreach (var ruleSet in DefDatabase<AtmosphericBiomeRuleSetDef>.AllDefs)
                {
                    TLog.Message($"Found Ruleset: {ruleSet}");
                    if (ruleSet.occurence == AtmosphericOccurence.AnyBiome)
                    {
                        TLog.Message("Any Biome --");
                        naturalAtmospheres.AddRange(ruleSet.atmospheres);
                        continue;
                    }

                    if (ruleSet.occurence == AtmosphericOccurence.SpecificBiome)
                    {
                        if (ruleSet.biomes.Contains(map.Biome))
                        {
                            naturalAtmospheres.AddRange(ruleSet.atmospheres);
                        }
                    }
                }
            }

            foreach (var atmosphere in naturalAtmospheres)
            {
                TLog.Message($"Adding natural atmosphere: {atmosphere}");
                var storedOf = mapContainer.TotalStoredOf(atmosphere.Def);
                var desired = mapContainer.Capacity * atmosphere.Value;
                var diff = Mathf.Round(desired - storedOf);
                if(diff <= 0) continue;
                mapContainer.TryAddValue(atmosphere.Def, diff, out _);
            }
        }

        private void TryAddToAtmosphere(IAtmosphericSource source)
        {
            if (!source.IsActive) return;
            if (compByRoom[source.Room].TryAddValue(source.AtmosphericDef, source.CreationAmount, out _))
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

            Log.Message($"[TAE] Disbanding roomComp: {comp.Room.ID}");

            //Remove Portals
            allConnections.RemoveAll(p => p.Connects(comp));
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
        public void Notify_NewPortal(AtmosphericPortal connection, RoomComponent viaRoom)
        {
            if (allConnections.Any(p => p.Thing == connection.Thing))
            {
                return;
            }
            allConnections.Add(connection);
        }

        /*
        public void Notify_AddConnection(AtmosphericPortal connection)
        {
            var same = AllConnections.Find(t => t.IsSameBuilding(connection));
            if (same != null)
            {
                Notify_RemoveConnection(same);
            }

            if (connection.ConnectsOutside())
            {
                ConnectionsToOutside.Add(connection);
            }

            AllConnections.Add(connection);
        }

        public void Notify_RemoveConnection(AtmosphericPortal connection)
        {
            ConnectionsToOutside.RemoveAll(c => c.IsSameBuilding(connection));
            AllConnections.RemoveAll(c => c.IsSameBuilding(connection));
        }
        */

        public override void Update()
        {
            base.Update();
            if (allConnections != null)
                GenDraw.DrawFieldEdges(allConnections.Select(p => p.Thing.Position).ToList(), Color.cyan);
        }

        //
        public bool TrySpawnGasAt(IntVec3 cell, ThingDef def, int value)
        {
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
    }
}
