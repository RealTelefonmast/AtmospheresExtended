using System.Collections.Generic;
using RimWorld;
using TeleCore;
using UnityEngine;
using Verse;
using GridLayout = Verse.GridLayout;

namespace TAE;

public class ITab_TAEDebug : ITab
{
    private List<TabRecord> cachedTabs;
    private Vector2 neighborListScrollPos = Vector2.zero;
    
    //
    private static readonly Vector2 WinSize = new Vector2(480f, 480f);

    public Building_Debug Sel => (Building_Debug) SelThing;
    public RoomComponent_Atmospheric Atmos => Sel.Atmos;
    
    private DebugTabs SelTab { get; set; }
    
    public ITab_TAEDebug()
    {
        size = WinSize;
        labelKey = "Debug";
        
        cachedTabs = new List<TabRecord>
        {
            new("Room", delegate { SelTab = DebugTabs.Room; },
                ()=>SelTab == DebugTabs.Room),
            new("Neighbours", delegate { SelTab = DebugTabs.Neighbours; },
                ()=>SelTab == DebugTabs.Neighbours),
            new("Border", delegate { SelTab = DebugTabs.Border; },
                ()=>SelTab == DebugTabs.Border)
        };
    }

    private enum DebugTabs
    {
        Room,
        Neighbours,
        Border
    }
    
    public override void FillTab()
    {
        Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y);
        rect = rect.BottomPartPixels(rect.height - TabDrawer.TabHeight);
        TabDrawer.DrawTabs(rect, cachedTabs);
        rect = rect.ContractedBy(10f);

        //
        switch (SelTab)
        {
            case DebugTabs.Room:
                DrawRoomData(rect);
                break;
            case DebugTabs.Neighbours:
                DrawNeighbourData(rect);
                break;
            case DebugTabs.Border:
                DrawBorderData(rect);
                break;
        }
    }

    private void DrawRoomData(Rect inRect)
    {
        GridLayout layout = new GridLayout(inRect, 3, 2);
        DrawLayout(layout, 3, 2);
        
        Rect roomContainerArea = layout.GetCellRect(0, 0);
        Rect roomContainerLabel = roomContainerArea.TopPart(0.15f);
        Rect roomContainer = roomContainerArea.BottomPart(0.85f);
        
        Rect mapContainerArea = layout.GetCellRect(0, 1);
        Rect mapContainerLabel = mapContainerArea.TopPart(0.15f);
        Rect mapContainer = mapContainerArea.BottomPart(0.85f);
        
        Widgets.Label(roomContainerLabel, "Room Container");
        TWidgets.DrawValueContainerReadout(roomContainer, Atmos.Container);
        TWidgets.HoverContainerReadout(roomContainer, Atmos.Container);
        
        Widgets.Label(mapContainerLabel, "Map Container");
        TWidgets.DrawValueContainerReadout(mapContainer, Atmos.OutsideContainer);
    }

    public void DrawLayout(GridLayout layout, int cols, int rows)
    {
        for (int col = 0; col < cols; col++)
        {
            for(int row = 0; row < rows; row++)
            {
                Rect cell = layout.GetCellRect(col, row);
                Widgets.DrawBoxSolid(cell, TColor.White005);
            }
        }
    }

    private void DrawNeighbourData(Rect inRect)
    {
        GridLayout layout = new GridLayout(inRect, 3, 2);
        DrawLayout(layout, 3, 2);

        Rect nghbListView = layout.GetCellRect(0, 0, 1, 2);
        Rect nghbListLabel = nghbListView.TopPartPixels(30);
        Rect nghbList = nghbListView.BottomPartPixels(nghbListView.height - 30);
        Rect nghbListScrollView = new Rect(nghbList.x, nghbList.y, nghbList.width, Atmos.AdjacentComps.Count * 30);
        
        //
        Widgets.Label(nghbListLabel, "Neighbour Rooms");
        Widgets.BeginScrollView(nghbList, ref neighborListScrollPos, nghbListScrollView);

        int i = 0;
        foreach (var roomComp in Atmos.AdjacentComps)
        {
            Rect nghbRect = new Rect(nghbList.x,  nghbListScrollView.y + i * 30, nghbListScrollView.width, 30);
            Rect checkBoxRect = new Rect(nghbList.xMax-24,  nghbListScrollView.y + i * 30, 24, 30);
            if (i % 2 == 0)
                Widgets.DrawHighlight(nghbRect);
            Widgets.Label(nghbRect, roomComp.ToString());
            var portal = (roomComp as RoomComponent_Atmospheric)?.Portal;
            bool hasItem = Sel.ActivePortals.Contains(portal);
            bool previous = hasItem;
            Widgets.Checkbox(checkBoxRect.position, ref hasItem, disabled: portal == null);
            if (previous != hasItem && portal != null)
            {
                if (hasItem)
                {
                    Sel.ActivePortals.Add(portal);
                }
                else
                {
                    Sel.ActivePortals.Remove(portal);
                }
            }
            i++;
        }
        Widgets.EndScrollView();
    }

    private void DrawBorderData(Rect rect)
    {
        var settingsRect = rect.RightPartPixels(200).ContractedBy(10);
        
        //Settings
        Listing_Standard standard = new Listing_Standard();
        standard.Begin(settingsRect);
        standard.CheckboxLabeled("Show AtmosPortals", ref Sel.ShowAtmosPortals);
        standard.CheckboxLabeled("Show All AtmosComps", ref Sel.ShowAtmosComps);
        standard.CheckboxLabeled("Hover Container Readout", ref Sel.ShowContainerReadout);

        standard.End();
    }

    private void DrawAtmosContainerReadout(Rect rect, AtmosphericContainer container,
        AtmosphericContainer outside)
    {
        float height = 5;
        Widgets.BeginGroup(rect);
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            foreach (var type in container.StoredDefs)
            {
                string label = $"{type.labelShort}: {container.StoredValueOf(type)}({container.StoredPercentOf(type).ToStringPercent()}) | {outside.StoredValueOf(type)}({outside.StoredPercentOf(type).ToStringPercent()})";
                
                Rect typeRect = new Rect(5, height, 10, 10);
                Vector2 typeSize = Text.CalcSize(label);
                Rect typeLabelRect = new Rect(20, height - 2, typeSize.x, typeSize.y);
                Widgets.DrawBoxSolid(typeRect, type.valueColor);
                Widgets.Label(typeLabelRect, label);

                height += 10 + 2;
            }

            Text.Font = default;
            Text.Anchor = default;
        }
        Widgets.EndGroup();
    }
}