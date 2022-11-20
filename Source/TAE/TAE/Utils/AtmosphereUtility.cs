using System;
using System.Collections.Generic;
using RimWorld;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE;

public static class AtmosphereUtility
{
    private static readonly int SampleNumCells = GenRadial.NumCellsInRadius(8.9f);
    private static readonly List<IntVec3> relevantCells = new ();
    private static readonly List<Room> visibleRooms = new ();

    public static void DrawSpreadingGasAroundMouse()
    {
        if (!AtmosphereMod.Mod.Settings.DrawAtmosphereAroundMouse) return;
        MenuOnGUI();
        
        /*
        FillAtmosphereRelevantCells(UI.MouseCell(), Find.CurrentMap);
        for (int i = 0; i < relevantCells.Count; i++)
        {
            IntVec3 intVec = relevantCells[i];
            var gasGrid =  Find.CurrentMap.GetMapInfo<SpreadingGasGrid>();
            var gasStack = gasGrid.CellStackAt(intVec.Index(gasGrid.Map));

            string stackLabel = "";
            for (int s = 0; s < gasStack.Length; s++)
            {
                stackLabel += $"[{gasStack[s].value}]\n[{gasStack[s].overflow}]";
            }
            
            GenMapUI.DrawThingLabel(GenMapUI.LabelDrawPosFor(intVec), stackLabel, Color.white);
        }
        */
    }

    private static void MenuOnGUI()
    {
        if (!UI.MouseCell().InBounds(Find.CurrentMap)) return;
        
        Rect rect = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 336f, (float)CellInspectorDrawer.numLines * 24f + 24f);
        CellInspectorDrawer.numLines = 0;
        
        rect.x += 26f;
        rect.y += 26f;
        if (rect.xMax > (float)UI.screenWidth)
        {
            rect.x -= rect.width + 52f;
        }
        if (rect.yMax > (float)UI.screenHeight)
        {
            rect.y -= rect.height + 52f;
        }
        Find.WindowStack.ImmediateWindow(733348, rect, WindowLayer.Super, FillWindow);
    }

    private static void FillWindow()
    {
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.WordWrap = false;
        
        IntVec3 intVec = UI.MouseCell();
        if (!intVec.InBounds(Find.CurrentMap)) return;
        var gasGrid =  Find.CurrentMap.GetMapInfo<SpreadingGasGrid>();
        var gasStack = gasGrid.CellStackAt(intVec.Index(gasGrid.Map));
        
        CellInspectorDrawer.DrawHeader("Stack Inspection");
        CellInspectorDrawer.DrawRow("Gas Grid: ", gasGrid.GasGrid.Length.ToString());
        CellInspectorDrawer.DrawRow("Has Any Gas: ",gasGrid.HasAnyGas.ToString());
        CellInspectorDrawer.DrawRow("Total Gas Count: ",gasGrid.TotalGasCount.ToString());
        CellInspectorDrawer.DrawRow("Total Gas Value: ",gasGrid.TotalGasValue.ToString());
        CellInspectorDrawer.DrawDivider();
        CellInspectorDrawer.DrawRow("Stack Types: ",gasStack.Length.ToString());
        CellInspectorDrawer.DrawRow("Any Gas In Stack: ",gasStack.HasAnyGas.ToString());
        CellInspectorDrawer.DrawRow("Total Stack Value: ",(gasStack.totalValue).ToString());
        CellInspectorDrawer.DrawDivider();
        for(int i = 0; i < gasStack.Length; i++)
        {
            var value = gasStack[i];
            CellInspectorDrawer.DrawRow($"{(SpreadingGasTypeDef)value.defID}:", String.Empty);
            CellInspectorDrawer.DrawRow($"Total Gas Count:", gasGrid.TotalSubGasCount[value.defID].ToString());
            CellInspectorDrawer.DrawRow($"Total Gas Value:", gasGrid.TotalSubGasValue[value.defID].ToString());
            CellInspectorDrawer.DrawRow($"{nameof(GasCellValue.value)}:",  value.value.ToString());
            CellInspectorDrawer.DrawRow($"{nameof(GasCellValue.overflow)}:",  value.overflow.ToString());
            CellInspectorDrawer.DrawRow($"{nameof(GasCellValue.totalBitVal)}:",  value.totalBitVal.ToString());
        }

        Text.WordWrap = true;
        Text.Anchor = TextAnchor.UpperLeft;
    }
    
    public static void DrawAtmosphereAroundMouse()
    {
        if (!AtmosphereMod.Mod.Settings.DrawAtmosphereAroundMouse) return;
        
        FillAtmosphereRelevantCells(UI.MouseCell(), Find.CurrentMap);
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

    private static void FillAtmosphereRelevantCells(IntVec3 root, Map map)
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

    private static float CellAtmosphere(IntVec3 c, Map map, out Color color)
    {
        var mapInfo = map.GetMapInfo<AtmosphericMapInfo>();
        color = mapInfo.Renderer.CellBoolDrawerGetExtraColorInt(c.Index(map), AtmosDefOf.Oxygen);
        return mapInfo.Renderer.CalculateAtmosphereAt(c, AtmosDefOf.Oxygen);
    }
}