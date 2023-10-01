﻿using System.Collections.Generic;
using TAE.Atmosphere.Rooms;
using TAE.AtmosphericFlow;
using TeleCore;
using TeleCore.Data.Events;
using TeleCore.Events;
using TeleCore.Primitive;
using Verse;

namespace TAE.Caching;

public struct CachedAtmosphere
{
    public int RoomID { get; set; }
    public int NumCells { get; set; }
    public DefValueStack<AtmosphericValueDef, double> Atmosphere { get; set; }

    public CachedAtmosphere Reset()
    {
        RoomID = 0;
        NumCells = 0;
        Atmosphere.Reset();
        return this;
    }
}

[StaticConstructorOnStartup]
public static class AtmosphericCacheEventNotifiers
{
    static AtmosphericCacheEventNotifiers()
    {
        GlobalEventHandler.CachingRegionStateInfoRoomUpdate += GlobalEventHandlerOnCachingRegionStateInfo;
        GlobalEventHandler.GettingRegionStateInfoRoomUpdate += GlobalEventHandlerOnGettingRegionStateInfo;
        GlobalEventHandler.ResettingRegionStateInfoRoomUpdate += GlobalEventHandlerOnResettingRegionStateInfo;
    }

    private static void GlobalEventHandlerOnResettingRegionStateInfo(RegionStateChangedArgs args)
    {
        if (Current.ProgramState != ProgramState.Playing) return;
        args.Map.GetMapInfo<AtmosphericMapInfo>().Cache.ResetCachedCellInfo(args.Cell, args.Map);
    }

    private static void GlobalEventHandlerOnGettingRegionStateInfo(RegionStateChangedArgs args)
    {
        if (Current.ProgramState != ProgramState.Playing) return;
        args.Map.GetMapInfo<AtmosphericMapInfo>().Cache.TryGetAndSetCachedRoomAtmosphereForRoom(args.Room, args.Map);
    }

    private static void GlobalEventHandlerOnCachingRegionStateInfo(RegionStateChangedArgs args)
    {
        if (Current.ProgramState != ProgramState.Playing) return;
        args.Map.GetMapInfo<AtmosphericMapInfo>().Cache.TryCacheRegionAtmosphere(args.Cell, args.Map, args.Region);
    }
}

public class AtmosphericCache
{
    private CachedAtmosphere[] _tempGrid;
    
    //
    private HashSet<int> _procRoomIDs;
    private List<CachedAtmosphere> _relevantCacheList;
    
    public AtmosphericCache(Map map)
    {
        _relevantCacheList = new List<CachedAtmosphere>();
        _procRoomIDs = new HashSet<int>();
        _tempGrid = new CachedAtmosphere[map.cellIndices.NumGridCells];
    }
    
    public void TryCacheRegionAtmosphere(IntVec3 c, Map map, Region reg)
    {
        Room room = reg.Room;
        if (room != null)
        {      
            var volume = room.GetRoomComp<RoomComponent_Atmosphere>()?.Volume;
            if(volume == null) return;
            SetCachedCell(c, map,new CachedAtmosphere
            {
                RoomID = room.ID,
                NumCells = room.CellCount,
                Atmosphere = volume.Stack
            });
        }
    }

    private void SetCachedCell(IntVec3 c, Map map, CachedAtmosphere cache)
    {
        _tempGrid[map.cellIndices.CellToIndex(c)] = cache;
    }

    public void ResetCachedCellInfo(IntVec3 c, Map map)
    {
        _tempGrid[map.cellIndices.CellToIndex(c)].Reset();
    }

    public bool TryGetAndSetCachedRoomAtmosphereForRoom(Room r, Map map) //, out DefValueStack<AtmosphericValueDef, double> result
    {
        var cellIndices = map.cellIndices;
        foreach (var c in r.Cells)
        {
            var cachedAtmos = _tempGrid[cellIndices.CellToIndex(c)];
            if (cachedAtmos.NumCells > 0 && !_procRoomIDs.Contains(cachedAtmos.RoomID))
            {
                _relevantCacheList.Add(cachedAtmos);
                _procRoomIDs.Add(cachedAtmos.RoomID);
            }
        }

        //var num = 0;
        var stack = new DefValueStack<AtmosphericValueDef, double>();
        foreach (var cachedAtmos2 in _relevantCacheList)
        {
            //num += cachedAtmos2.NumCells;
            stack += cachedAtmos2.Atmosphere;
        }

        var result = stack;
        var result2 = !_relevantCacheList.NullOrEmpty();
        _procRoomIDs.Clear();
        _relevantCacheList.Clear();
        
        var roomComp = r.GetRoomComp<RoomComponent_Atmosphere>();
        roomComp?.Volume?.LoadFromStack(result);
        
        return result2;
    }
}