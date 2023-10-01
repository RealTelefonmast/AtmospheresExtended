using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using TAE.Atmosphere.Rooms;
using TeleCore;
using TeleCore.Network.Utility;
using TeleCore.Rendering;
using TeleCore.Static.Utilities;
using UnityEngine;
using Verse;
using GridLayout = Verse.GridLayout;
using WidgetRow = Verse.WidgetRow;

namespace TAE;

public class Building_Debug : Building
{

    private RoomComponent_Atmosphere _atmos;
    
    public RoomComponent_Atmosphere Atmos
    {
        get
        {
            if (_atmos?.Disbanded ?? false)
            {
                _atmos = this.GetRoom().GetRoomComp<RoomComponent_Atmosphere>();
            }
            return _atmos ??= this.GetRoom().GetRoomComp<RoomComponent_Atmosphere>();
        }
    }

    //
    public bool ShowAtmosPortals = true;
    public bool ShowAtmosComps = false;
    public bool ShowAllBorderThings = true;
    public bool ShowContainerReadout = false;
    
    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
    }
    
    public override void Tick()
    {
        base.Tick();
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
            var comp = room?.GetRoomComp<RoomComponent_Atmosphere>();
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
                //WidgetStackPanel.DrawRow($"Is Connector:", $"{comp.IsConnector}");
                WidgetStackPanel.DrawRow($"Is Doorway:", $"{comp.IsDoorway}");
                WidgetStackPanel.DrawRow($"Is Outdoors:", $"{comp.IsOutdoors}");
                //WidgetStackPanel.DrawRow($"Has Portal:", $"{comp.Portal != null}");
                //WidgetStackPanel.DrawRow($"Portal:", comp.Portal?.ToString());
                
                WidgetStackPanel.DrawRow("Comp:", $"{comp.GetType().Name}");
            }
            WidgetStackPanel.End();
        }

        if (ShowContainerReadout)
        {
            var mouse = UI.MouseCell();
            if (!mouse.InBounds(Map)) return;
            var room = mouse.GetRoomFast(Find.CurrentMap);
            var comp = room?.GetRoomComp<RoomComponent_Atmosphere>();
            if (comp == null) return;
            
            Vector2 mousePosition = Event.current.mousePosition;
            Vector2 containerReadoutSize = FlowUI<AtmosphericValueDef>.GetFlowBoxReadoutSize(comp.Volume);
            
            //.DrawValueContainerReadout(new Rect(mousePosition.x, mousePosition.y - containerReadoutSize.y, containerReadoutSize.x, containerReadoutSize.y), comp.CurrentContainer);
            FlowUI<AtmosphericValueDef>.DrawFlowBoxReadout(new Rect(mousePosition.x, mousePosition.y - containerReadoutSize.y, containerReadoutSize.x, containerReadoutSize.y), comp.Volume);
            //TWidgets.DrawValueContainerReadout(new Rect(mousePosition3.x, mousePosition3.y - containerReadoutSize3.y, containerReadoutSize3.x, containerReadoutSize3.y), comp.OutsideContainer);
        }
    }
    
    public override void Draw()
    {
        if(_atmos == null) return;
        foreach (var room in Atmos.AtmosphericInfo.AllAtmosphericRooms)
        {
            GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
            r.center = room.Parent.MinMaxCorners[0].ToVector3() + new Vector3(0.075f, 0, 0.75f);
            r.size = new Vector2(1.5f, 0.15f);
            r.fillPercent = (float)room.Volume.FillPercent;
            r.filledMat = AtmosContent.FilledMat;
            r.unfilledMat = AtmosContent.UnFilledMat;
            r.margin = 0f;
            r.rotation = Rot4.East;
            GenDraw.DrawFillableBar(r);
        }
        
        
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
    }
}