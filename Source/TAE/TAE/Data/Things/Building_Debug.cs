using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using TeleCore;
using TeleCore.Rendering;
using TeleCore.Static.Utilities;
using UnityEngine;
using Verse;
using GridLayout = Verse.GridLayout;
using WidgetRow = Verse.WidgetRow;

namespace TAE;

public class Building_Debug : Building
{
    //
    public RoomComponent_Atmospheric Atmos;
    public HashSet<AtmosphericPortal> ActivePortals;
    
    //
    public bool ShowAtmosPortals = true;
    public bool ShowAtmosComps = false;
    public bool ShowAllBorderThings = true;
    public bool ShowContainerReadout = false;


    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        Atmos = this.GetRoom().GetRoomComp<RoomComponent_Atmospheric>();
        ActivePortals = new HashSet<AtmosphericPortal>();
    }

    public void Notify_ActivatePortal(AtmosphericPortal portal)
    {
        ActivePortals.Add(portal);
    }

    public void Notify_DeactivatePortal(AtmosphericPortal portal)
    {
        ActivePortals.Remove(portal);
    }
    
    public override void Tick()
    {
        base.Tick();
        if (Atmos.Disbanded)
        {
            Atmos = this.GetRoom().GetRoomComp<RoomComponent_Atmospheric>();;
        }
    }

    public override void DrawGUIOverlay()
    {
        GenMapUI.DrawThingLabel(GenMapUI.LabelDrawPosFor(Position), $"[{Atmos?.Room?.ID}]", Color.white);
        
        if (ShowAtmosComps)
        {
            var mouse = UI.MouseCell();
            if (!mouse.InBounds(Map)) return;
            var room = mouse.GetRoomFast(Find.CurrentMap);
            var tracker = room?.RoomTracker();
            var comp = room?.GetRoomComp<RoomComponent_Atmospheric>();
            var comp2 = room?.GetRoomComp<RoomComponent_AirLock>();
            
            Rect rect = new Rect(Event.current.mousePosition.x + 32, Event.current.mousePosition.y + 32, 250, 400);
            Widgets.DrawWindowBackground(rect);
            WidgetStackPanel.Begin(rect.ContractedBy(5));
            WidgetStackPanel.DrawHeader("Atmospheric");
            WidgetStackPanel.DrawRow("Room:", $"[{room?.ID}]: {room?.CellCount}");
            WidgetStackPanel.DrawRow("Tracker:", $"{tracker}");
            if (comp != null)
            {
                WidgetStackPanel.DrawDivider();
                WidgetStackPanel.DrawHeader("Atmospheric");
                WidgetStackPanel.DrawRow($"Is Connector:", $"{comp.IsConnector}");
                WidgetStackPanel.DrawRow($"Is Doorway:", $"{comp.IsDoorway}");
                WidgetStackPanel.DrawRow($"Is Outdoors:", $"{comp.IsOutdoors}");
                WidgetStackPanel.DrawRow($"Has Portal:", $"{comp.Portal != null}");
                WidgetStackPanel.DrawRow($"Portal:", comp.Portal?.ToString());
            }

            WidgetStackPanel.DrawRow("Comp:", $"{comp}");
            WidgetStackPanel.End();
        }

        if (ShowContainerReadout)
        {
            var mouse = UI.MouseCell();
            if (!mouse.InBounds(Map)) return;
            var room = mouse.GetRoomFast(Find.CurrentMap);
            var comp = room?.GetRoomComp<RoomComponent_Atmospheric>();
            if (comp == null) return;
            
            Vector2 mousePosition = Event.current.mousePosition;
            Vector2 containerReadoutSize = TWidgets.GetValueContainerReadoutSize(comp.CurrentContainer);
            
            Vector2 mousePosition2 = new Vector2(mousePosition.x + containerReadoutSize.x, mousePosition.y);
            Vector2 containerReadoutSize2 = TWidgets.GetValueContainerReadoutSize(comp.Container);

            Vector2 mousePosition3 = new Vector2(mousePosition2.x + containerReadoutSize2.x, mousePosition2.y);
            Vector2 containerReadoutSize3 = TWidgets.GetValueContainerReadoutSize(comp.OutsideContainer);
            
            TWidgets.DrawValueContainerReadout(new Rect(mousePosition.x, mousePosition.y - containerReadoutSize.y, containerReadoutSize.x, containerReadoutSize.y), comp.CurrentContainer);
            TWidgets.DrawValueContainerReadout(new Rect(mousePosition2.x, mousePosition2.y - containerReadoutSize2.y, containerReadoutSize2.x, containerReadoutSize2.y), comp.Container);
            TWidgets.DrawValueContainerReadout(new Rect(mousePosition3.x, mousePosition3.y - containerReadoutSize3.y, containerReadoutSize3.x, containerReadoutSize3.y), comp.OutsideContainer);
        }
    }

    public override void Draw()
    {
        if (ShowAtmosComps)
        {
            var room = UI.MouseCell().GetRoomFast(Map);
            if (room != null)
                GenDraw.DrawFieldEdges(room.Cells.ToList());
        }

        if (Find.Selector.IsSelected(this))
        {
            foreach (var thing in Atmos?.Parent?.BorderListerThings?.AllThings)
            {
                DebugCellRenderer.RenderCell(thing.Position, Color.clear, Color.cyan, 1);
            }
        }

        foreach (var portal in ActivePortals)
        {
            if (portal?.IsValid ?? false)
            {
                GenDraw.DrawTargetingHighlight_Cell(portal.Thing.Position);
                if (Find.Selector.IsSelected(portal.Thing))
                {
                    portal.DrawDebug();
                }
            }   
        }
    }
}