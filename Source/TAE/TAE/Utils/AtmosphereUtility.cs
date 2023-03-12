using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using TAE.Static;
using TeleCore;
using TeleCore.Static.Utilities;
using UnityEngine;
using Verse;

namespace TAE;

public static class AtmosphereUtility
{
    private static readonly int SampleNumCells = GenRadial.NumCellsInRadius(8.9f);
    private static readonly List<IntVec3> relevantCells = new();
    private static readonly List<Room> visibleRooms = new();

    public static void DrawPassPercentCells()
    {
        if (!AtmosphereMod.Mod.Settings.DrawAtmosphereAroundMouse) return;
        
        //
        var root = UI.MouseCell();
        var map = Find.CurrentMap;
        //FillAtmosphereRelevantCells(UI.MouseCell(), Find.CurrentMap);
        
        var cacheInfo = Find.CurrentMap.GetMapInfo<DynamicDataCacheMapInfo>();
        for (int i = 0; i < SampleNumCells; i++)
        {
            IntVec3 intVec = root + GenRadial.RadialPattern[i];
            if (intVec.InBounds(map) && !intVec.Fogged(map))
            {
                var value = cacheInfo.AtmosphericPassGrid[intVec];
                DebugCellRenderer.RenderCell(intVec, Color.black, Color.green, value);
                //CellRenderer.RenderCell(intVec, SolidColorMaterials.NewSolidColorMaterial(Color.Lerp(Color.black, Color.green, value), ShaderDatabase.MetaOverlay));
            }
        }
    }
    
    public static void DrawSpreadingGasAroundMouse()
    {
        if (!AtmosphereMod.Mod.Settings.DrawAtmosphereAroundMouse) return;

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

        Rect rect = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 336f,
            (float) CellInspectorDrawer.numLines * 24f + 24f);
        CellInspectorDrawer.numLines = 0;

        rect.x += 26f;
        rect.y += 26f;
        if (rect.xMax > (float) UI.screenWidth)
        {
            rect.x -= rect.width + 52f;
        }

        if (rect.yMax > (float) UI.screenHeight)
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
        var gasGrid = Find.CurrentMap.GetMapInfo<SpreadingGasGrid>();
        var gasStack = gasGrid.CellStackAt(intVec.Index(gasGrid.Map));

        CellInspectorDrawer.DrawHeader("Stack Inspection");
        CellInspectorDrawer.DrawRow("Gas Grid: ", gasGrid.GasGrid.Length.ToString());
        CellInspectorDrawer.DrawRow("Has Any Gas: ", gasGrid.HasAnyGas.ToString());
        CellInspectorDrawer.DrawRow("Total Gas Count: ", gasGrid.TotalGasCount.ToString());
        CellInspectorDrawer.DrawRow("Total Gas Value: ", gasGrid.TotalGasValue.ToString());
        CellInspectorDrawer.DrawDivider();
        CellInspectorDrawer.DrawRow("Stack Types: ", gasStack.Length.ToString());
        CellInspectorDrawer.DrawRow("Any Gas In Stack: ", gasStack.HasAnyGas.ToString());
        CellInspectorDrawer.DrawRow("Total Stack Value: ", (gasStack.totalValue).ToString());
        CellInspectorDrawer.DrawDivider();
        for (int i = 0; i < gasStack.Length; i++)
        {
            var value = gasStack[i];
            CellInspectorDrawer.DrawRow($"{(SpreadingGasTypeDef) value.defID}:", String.Empty);
            CellInspectorDrawer.DrawRow($"Total Gas Count:", gasGrid.TotalSubGasCount[value.defID].ToString());
            CellInspectorDrawer.DrawRow($"Total Gas Value:", gasGrid.TotalSubGasValue[value.defID].ToString());
            CellInspectorDrawer.DrawRow($"{nameof(GasCellValue.value)}:", value.value.ToString());
            CellInspectorDrawer.DrawRow($"{nameof(GasCellValue.overflow)}:", value.overflow.ToString());
            CellInspectorDrawer.DrawRow($"{nameof(GasCellValue.totalBitVal)}:", value.totalBitVal.ToString());
        }

        Text.WordWrap = true;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    //TODO: Radial readout was only used for oxygen, kinda useless
    public static void DrawAtmosphereAroundMouse()
    {
        if (!AtmosphereMod.Mod.Settings.DrawAtmosphereAroundMouse) return;

        FillAtmosphereRelevantCells(UI.MouseCell(), Find.CurrentMap);
        for (int i = 0; i < relevantCells.Count; i++)
        {
            IntVec3 intVec = relevantCells[i];
            // float num = CellAtmosphere(intVec, Find.CurrentMap, out Color color);
            // if (num != 0f)
            // {
            //     GenMapUI.DrawThingLabel(GenMapUI.LabelDrawPosFor(intVec), Mathf.RoundToInt(num).ToStringCached(), color);
            // }
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

    //
    private static float PassiveFlowPctForDoor(Building_Door door, float fillage)
    {
        var categories = door.Stuff?.stuffProps?.categories;
        if (categories is null || categories.Count == 0) return 1f - fillage;

        //TODO:Add Stat for gas permeability
        var permeability = categories.Sum(c => AtmosphericData.PassPercentByStuff.GetValueOrDefault(c, 0)) /
                           categories.Count;
        return (float) Math.Round(1f - (fillage * (1f - permeability)), 4);
    }

    public static float DefaultAtmosphericPassPercentAtCell(IntVec3 pos, Map map)
    {
        var thingList = pos.GetThingList(map);
        if (thingList.NullOrEmpty()) return 1f;
        var passPct = 1f;
        for (var i = 0; i < thingList.Count; i++)
        {
            var thing = thingList[i];
            if (thing is Building b)
            {
                var buildingPassPct = DefaultAtmosphericPassPercent(b);
                
                //Set maximum passage to minimum possible
                if (buildingPassPct < passPct)
                    passPct = buildingPassPct;
            }
        }
        return passPct;
    }
    
    public static float DefaultAtmosphericPassPercent(Thing forThing)
    {
        if (forThing == null) return 1f;

        //Custom Worker Subroutine
        if (AtmosPortalData.TryGetWorkerFor(forThing.def, out var worker))
        {
            return worker.Worker.PassPercent(forThing);
        }

        //
        var fullFillage = forThing.def.Fillage == FillCategory.Full;
        var fillage = forThing.def.fillPercent;
        var flowPct = fullFillage ? 0f : 1f - fillage;
        return forThing switch
        {
            Building_Door door => door.Open ? 1 : PassiveFlowPctForDoor(door, fillage),
            Building_Vent vent => FlickUtility.WantsToBeOn(vent) ? 1f : 0f,
            Building_Cooler cooler => cooler.IsPoweredOn() ? 2f : 0f,
            { } b => flowPct,
            _ => 0.0f
        };
    }

    public static bool IsPassBuilding(Building building)
    {
        if (building == null) return false;

        //Custom PassBuilding Subroutine
        if (AtmosPortalData.IsPassBuilding(building.def))
        {
            return true;
        }

        var fullFillage = building.def.Fillage == FillCategory.Full;
        return building switch
        {
            Building_Door => true,
            Building_Vent => true,
            Building_Cooler => true,
            { } b => !fullFillage,
            _ => false
        };
    }
}