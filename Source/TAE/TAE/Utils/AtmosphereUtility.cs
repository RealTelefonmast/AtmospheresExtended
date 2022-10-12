using System.Collections.Generic;
using RimWorld;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE;

public static class AtmosphereUtility
{
    public static readonly int SampleNumCells = GenRadial.NumCellsInRadius(8.9f);
    
    public static List<IntVec3> relevantCells = new List<IntVec3>();
    private static List<Room> visibleRooms = new List<Room>();

    public static void DrawAtmosphereAroundMouse()
    {
        if (!AtmosphereMod.Mod.Settings.DrawAtmosphereAroundMouse) return;
        
        FillBeautyRelevantCells(UI.MouseCell(), Find.CurrentMap);
        for (int i = 0; i < relevantCells.Count; i++)
        {
            IntVec3 intVec = relevantCells[i];
            float num = CellAtmosphere(intVec, Find.CurrentMap, out Color color);
            if (num != 0f)
            {
                GenMapUI.DrawThingLabel(GenMapUI.LabelDrawPosFor(intVec), Mathf.RoundToInt(num).ToStringCached(), color);
            }
        }
    }
    
    public static void FillBeautyRelevantCells(IntVec3 root, Map map)
    {
        relevantCells.Clear();
        Room room = root.GetRoom(map);
        if (room == null)
        {
            return;
        }
        visibleRooms.Clear();
        visibleRooms.Add(room);
        if (room.IsDoorway)
        {
            foreach (Region region in room.FirstRegion.Neighbors)
            {
                if (!visibleRooms.Contains(region.Room))
                {
                    visibleRooms.Add(region.Room);
                }
            }
        }
        for (int i = 0; i < SampleNumCells; i++)
        {
            IntVec3 intVec = root + GenRadial.RadialPattern[i];
            if (intVec.InBounds(map) && !intVec.Fogged(map))
            {
                Room room2 = intVec.GetRoom(map);
                if (!visibleRooms.Contains(room2))
                {
                    bool flag = false;
                    for (int j = 0; j < 8; j++)
                    {
                        IntVec3 loc = intVec + GenAdj.AdjacentCells[j];
                        if (visibleRooms.Contains(loc.GetRoom(map)))
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        continue;
                    }
                }
                relevantCells.Add(intVec);
            }
        }
        visibleRooms.Clear();
    }
    
    public static float CellAtmosphere(IntVec3 c, Map map, out Color color)
    {
        var mapInfo = map.GetMapInfo<AtmosphericMapInfo>();
        color = mapInfo.Renderer.CellBoolDrawerGetExtraColorInt(c.Index(map), AtmosDefOf.Oxygen);
        return mapInfo.Renderer.CalculateAtmosphereAt(c, AtmosDefOf.Oxygen);
    }
}