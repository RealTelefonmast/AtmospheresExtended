﻿using System;
using System.Collections.Generic;
using System.Linq;
using TeleCore;
using TeleCore.FlowCore;
using UnityEngine;
using Verse;

namespace TAE;

[StaticConstructorOnStartup]
public class RoomComponent_Atmospheric : RoomComponent, IContainerHolderRoom<AtmosphericDef>, IContainerImplementer<AtmosphericDef, IContainerHolderRoom<AtmosphericDef>, AtmosphericContainer>
{
    //
    private static readonly Material FilledMat = SolidColorMaterials.NewSolidColorMaterial(Color.green, ShaderDatabase.MetaOverlay);
    private static readonly Material UnFilledMat = SolidColorMaterials.NewSolidColorMaterial(TColor.LightBlack, ShaderDatabase.MetaOverlay);

    //
    private int dirtyMarks;

    //
    private AtmosphericContainer container;
    private List<AtmosphericPortal> portals;
    private AtmosphericPortal selfPortal;

    private RoomOverlay_Atmospheric renderer;

    //
    public bool IsDoorway => Room.IsDoorway;

    //
    public bool IsOutdoors => Parent.IsOutside;
    public bool IsDirty => dirtyMarks > 0;
    public bool IsConnector => selfPortal != null;

    public int ConnectorCount => IsOutdoors ? AtmosphericInfo.ConnectorCount : portals.Count;

    //
    public AtmosphericPortal Portal => selfPortal;

    //
    public AtmosphericMapInfo AtmosphericInfo => Map.GetMapInfo<AtmosphericMapInfo>();

    public AtmosphericContainer OutsideContainer => AtmosphericInfo.MapContainer;
    
    public AtmosphericContainer Container => container;

    public AtmosphericContainer CurrentContainer => IsOutdoors ? OutsideContainer : (IsConnector ? selfPortal[0].CurrentContainer : Container);

    //
    public void Notify_ContainerStateChanged(NotifyContainerChangedArgs<AtmosphericDef> args)
    {
        
    }

    public string ContainerTitle => "Atmosphers be here";
    public RoomComponent RoomComponent => this;

    //private IEnumerable<Thing> PhysicalGas => Parent.ListerThings.AllThings.Where(t => t is SpreadingGas);
    public override void Notify_BorderThingAdded(Thing thing)
    {
        //Add any non 100% filled building
        if (thing is not Building b) return;
        ProcessPotentialPortal(b);
    }

    private void ProcessPotentialPortal(Building b)
    {
        //If blocked by impassible (wall, etc), skip
        var cacheInfo = b.Map.GetMapInfo<DynamicDataCacheMapInfo>();

        //Otherwise register the structure as a portal
        if (!AtmosphereUtility.IsAtmosphericPortal(b)) return;

        var bRoom = b.GetRoom();
        if (bRoom == null)
        {
            var otherRoom = b.NeighborRoomOf(Room);
            if (otherRoom == null) return;

            var portal = new AtmosphericPortal(b, this, otherRoom.GetRoomComp<RoomComponent_Atmospheric>());
            portals.Add(portal);
            AtmosphericInfo.Notify_NewPortal(portal);
        }
        else
        {
            var bAtmos = bRoom.GetRoomComp<RoomComponent_Atmospheric>();
            if (bAtmos != null)
            {
                if (bAtmos.Portal?.IsValid ?? false)
                {
                    portals.Add(bAtmos.Portal);
                    AtmosphericInfo.Notify_NewPortal(bAtmos.Portal);
                }
                else
                {
                    var otherRoom = b.NeighborRoomOf(Room);
                    if (otherRoom == null) return;

                    var portal = new AtmosphericPortal(b, this, otherRoom.GetRoomComp<RoomComponent_Atmospheric>());
                    bAtmos.Noitfy_SetSelfPortal(portal);

                    portals.Add(portal);
                    AtmosphericInfo.Notify_NewPortal(portal);
                }
            }
        }
    }

    public override void PostCreate(RoomTracker parent)
    {
        portals = new List<AtmosphericPortal>();
        AtmosphericInfo.Notify_NewComp(this);
        renderer = new RoomOverlay_Atmospheric();
    }

    public override void Disband(RoomTracker parent, Map map)
    {
        AtmosphericInfo.Notify_DisbandedComp(this);
        foreach (var portal in portals)
        {
            portal.MarkInvalid();
        }
    }

    public override void FinalizeMapInit()
    {
        
    }

    public override void Init(RoomTracker[] previous = null)
    {
        base.Init(previous);
    }

    public override void PostInit(RoomTracker[] previous)
    {
        CreateContainer();
        MarkDirty();

        //
        Regenerate(previous);

        if (Parent.IsProper)
        {
            TeleUpdateManager.Notify_EnqueueNewSingleAction(() =>
                renderer.UpdateMesh(Room.Cells, Parent.MinVec, Parent.Size.x, Parent.Size.z));
        }

        //--
    }
    
    public override void Notify_Reused()
    {
        base.Notify_Reused();
        portals.Clear();
    }

    public override void Notify_RoofClosed()
    {
        AtmosphericInfo.RegenerateMapInfo();
        //Data_GetCachedRegionalAtmosphere();
        Data_CaptureOutsideAtmosphere();
    }

    /// <summary>
    /// Push any Room-Atmosphere into the Map-Atmosphere
    /// </summary>
    public override void Notify_RoofOpened()
    {
        //TODO: Figure out event-chain when roof opens in room
        //eg. what to do when an oxygen rich room is open under water?
        Container.Clear();
    }

    public override void Notify_PawnEnteredRoom(Pawn pawn)
    {
        var tracker = pawn.TryGetComp<Comp_PawnAtmosphereTracker>();
        if (tracker == null) return;
        tracker.Notify_EnteredAtmosphere(this);
    }

    public override void Notify_PawnLeftRoom(Pawn pawn)
    {
        var tracker = pawn.TryGetComp<Comp_PawnAtmosphereTracker>();
        if (tracker == null) return;
        tracker.Notify_Clear();
    }

    private void Noitfy_SetSelfPortal(AtmosphericPortal portal)
    {
        selfPortal = portal;
    }

    public override void Notify_RoofChanged()
    {
        base.Notify_RoofChanged();
    }

    private void MarkDirty()
    {
        dirtyMarks = Mathf.Clamp(dirtyMarks + 1, 0, Int32.MaxValue);
    }

    //Value Manipulation
    public bool  TryAddValueToRoom(AtmosphericDef def, float amount, out ValueResult<AtmosphericDef> result)
    {
        if (!CurrentContainer.TryAddValue(def, amount, out result)) return false;
        Notify_AddedContainerValue(def, result.ActualAmount);
        return true;
    }

    public bool TryRemoveValue(AtmosphericDef def, float amount, out ValueResult<AtmosphericDef> result)
    {
        return CurrentContainer.TryRemoveValue(def, amount, out result);
    }

    //RoomComp Generation
    private void Regenerate(RoomTracker[] previous)
    {
        if (!IsDirty) return;
        container.Notify_RoomChanged(this, Parent.CellCount);

        if (previous != null)
        {
            foreach (var oldTracker in previous)
            {
                var comp = oldTracker.GetRoomComp<RoomComponent_Atmospheric>();
                var container = oldTracker.IsOutside ? comp.OutsideContainer : comp.Container;
                foreach (var value in container.ValueStack)
                {
                    var transferPct = oldTracker.CellCount > Parent.CellCount
                        ? (oldTracker.CellCount / (float) Parent.CellCount)
                        : 1;
                    Container.TryAddValue(value * transferPct);
                    renderer.TryRegisterNewOverlayPart(value.Def);
                }
            }
        }
    }

    private void Data_CaptureOutsideAtmosphere()
    {
        var outside = OutsideContainer.ValueStack;
        var parts = outside.Length;
        var partSize = Container.Capacity / parts;
        foreach (var value in outside)
        {
            Container.TryAddValue(value.Def, partSize * OutsideContainer.StoredPercent);
        }
        
        /*
        foreach (var atmosphericDef in OutsideContainer.StoredDefs)
        {
            var pct = OutsideContainer.StoredPercentOf(atmosphericDef);
            var newValue = Mathf.Round(container.Capacity * pct);

            TryAddValueToRoom(atmosphericDef, newValue, out _);
        }
        */
    }

    private void CreateContainer()
    {
        container = new AtmosphericContainer(this, AtmosResources.DefaultAtmosConfig(Parent.CellCount));
    }

    public override void CompTick()
    {
        /*
        foreach (var portal in Parent.RoomPortals)
        {
            var portalComp = portal.PortalRoom.GetRoomComp<RoomComponent_Atmospheric>();
            var eqRoom = RoomContainer;
            var portalCont = portalComp.RoomContainer;
            
            var tempTypes = RoomContainer.AllStoredTypes.Union(portalComp.RoomContainer.AllStoredTypes).ToArray();
            foreach (var atmosDef in tempTypes)
            {
                AtmosMath.TryEqualize(eqRoom, portalCont, atmosDef);
            }
        }
        */
    }

    /*
    [Obsolete]
    public void Equalize()
    {
        //
        if (ActualValue <= 0) return;

        //EqualizeWith
        if (Parent.OpenRoofCount <= 0) return;
        if (Outside.FullySaturated) return;

        if (ActualSaturation > Outside.Saturation)
        {
            ActualContainer.TryEqualize(Outside, 1f, out _);
        }
    }
    */

    //Rendering
    private bool openColorPicker = false;

    public override void OnGUI()
    {
        if (!IsOutdoors)
        {
            if (renderWindow)
            {
                var immRect = new Rect(100, 100, 220, 400);
                var newR = immRect.AtZero();
                Find.WindowStack.ImmediateWindow(1453564359, immRect, WindowLayer.GameUI, delegate
                {
                    TWidgets.DrawColoredBox(newR, TColor.White025, Color.white, 1);
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.UpperLeft;
                    float height = 5;
                    foreach (var type in OutsideContainer.StoredDefs)
                    {
                        Rect typeRect = new Rect(5, height, 220, 10);
                        var pct = OutsideContainer.StoredPercentOf(type);
                        WidgetRow row = new WidgetRow(typeRect.x, typeRect.y, UIDirection.RightThenDown);
                        row.Label(type.defName, 100);
                        row.FillableBar(100, 20, 1f, pct.ToStringPercent(), BaseContent.YellowTex, BaseContent.BlackTex);
                        height += 10 + 2;
                    }

                    Text.Font = default;
                    Text.Anchor = default;

                }, false);
            }
        }
    }


    private bool renderWindow = false;

    private void DrawMenu(IntVec3 pos)
    {
        var v = DrawPosFor(pos);

        var driver = Find.CameraDriver;
        var width = 5 * driver.CellSizePixels;
        var height = 2 * driver.CellSizePixels;
        var rect = new Rect(v.x - width, v.y - height, width, height);
        var scale = ((driver.CellSizePixels / 46)); //+ (1 - ((UI.CurUICellSize() / 46)));

        //var rect = new Rect(v.x, v.y, 230, 92);
        TWidgets.DrawColoredBox(rect, new Color(1, 1, 1, 0.125f), Color.white, 1);
        rect = rect.ContractedBy(5 * scale);
        Widgets.BeginGroup(rect);
        {
            TWidgets.DrawColoredBox(rect.AtZero(), Color.clear, Color.black, 1);
            var innerRect = new Rect(0, 0, rect.width, rect.height);
            //DrawAtmosContainerReadout(innerRect, new Vector2(scale, scale), CurrentContainer, OutsideContainer);
            var text = $"[{AdjacentComps.Count}]|[{Parent.RoomPortals.Count}]:[{Parent.AdjacentTrackers.Count}]";
            var textPortal = selfPortal != null ? $"{selfPortal[0]}--{selfPortal[1]}" : "";
            TWidgets.DoTinyLabel(innerRect.RightPart(0.65f).BottomPart(0.25f),
                $"[{Room.ID}]{(IsConnector ? textPortal : text)}");
            var addRect = innerRect.BottomPart(0.2f).LeftPart(0.2f);

            WidgetRow row = new WidgetRow(addRect.x, addRect.y, UIDirection.RightThenDown);
            if (row.ButtonText("Add"))
            {
                FloatMenu floatMenu = new FloatMenu(DefDatabase<AtmosphericDef>.AllDefsListForReading.Select(d =>
                    new FloatMenuOption(d.defName,
                        delegate { TryAddValueToRoom(d, container.Capacity * 0.25f, out _); })).ToList());
                Find.WindowStack.Add(floatMenu);
            }

            if (row.ButtonText("Clear"))
            {
                CurrentContainer.Clear();
            }

            if (row.ButtonText("Map"))
            {
                renderWindow = !renderWindow;
            }

            row.Label(textPortal);
        }
        Widgets.EndGroup();

    }

    private static float cellsize = 46;
    private static Texture2D colorTex;

    private void DrawColorPicker(IntVec3 pos)
    {
        var height = cellsize * 4;
        var v = DrawPosFor(pos) - new Vector2(0, (2 * cellsize) + height);
        var rect = new Rect(v.x, v.y, height + cellsize, height);
        TWidgets.DrawColoredBox(rect, new Color(1, 1, 1, 0.125f), Color.white, 1);

        //Get Color Grid

    }

    private void ScaleRender(Rect inRect, Vector2 scale, Action renderAction)
    {
        var previousMatrix = GUI.matrix;
        GUIUtility.ScaleAroundPivot(scale, inRect.position);

        renderAction.Invoke();

        GUI.matrix = previousMatrix;
    }

    private Vector2 DrawPosFor(IntVec3 pos)
    {
        Vector3 position = new Vector3((float) pos.x, (float) pos.y + AltitudeLayer.MetaOverlays.AltitudeFor(),
            (float) pos.z);
        Vector2 vector = Find.Camera.WorldToScreenPoint(position) / Prefs.UIScale;
        vector.y = (float) UI.screenHeight - vector.y;
        return vector;
    }

    public override void Draw()
    {
        if (Parent.IsProper)
        {
            renderer.UpdateTick();
            foreach (var renderDef in renderer.Defs)
            {
                var value = Container.StoredPercentOf(renderDef);
                if (value <= 0) continue;
                renderer.DrawFor(renderDef, Parent.DrawPos, value);
            }
        }

        //
        GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
        r.center = Parent.MinMaxCorners[0].ToVector3() + new Vector3(0.075f, 0, 0.75f);
        r.size = new Vector2(1.5f, 0.15f);
        r.fillPercent = container.StoredPercent;
        r.filledMat = FilledMat;
        r.unfilledMat = UnFilledMat;
        r.margin = 0f;
        r.rotation = Rot4.East;
        GenDraw.DrawFillableBar(r);
    }

    public void Notify_ContainerFull()
    {
    }

    public void Notify_ContainerStateChanged()
    {
    }

    public void Notify_AddedContainerValue(AtmosphericDef def, float value)
    {
        renderer.TryRegisterNewOverlayPart(def);
    }

    public override string ToString()
    {
        return $"[{Room.ID}]";
    }

    public bool Notify_SpradingGasDissipating(SpreadingGasTypeDef def, int dissipatedAmount, out ValueResult<AtmosphericDef> actual)
    {
        return TryAddValueToRoom(def.dissipateTo, dissipatedAmount, out actual);
    }
}
