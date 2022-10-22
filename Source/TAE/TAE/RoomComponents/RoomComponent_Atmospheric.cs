using System;
using System.Collections.Generic;
using System.Linq;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE
{
    public class RoomComponent_Atmospheric : RoomComponent
    {
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
        public bool IsConnector => selfPortal.IsValid;

        public int ConnectorCount => IsOutdoors ? AtmosphericInfo.ConnectorCount : portals.Count;
        
        //
        public AtmosphericPortal Portal => selfPortal;
        
        //
        public AtmosphericMapInfo AtmosphericInfo => Map.GetMapInfo<AtmosphericMapInfo>();
        
        public AtmosphericContainer OutsideContainer => AtmosphericInfo.MapContainer;
        public AtmosphericContainer RoomContainer => container;
        public AtmosphericContainer CurrentContainer => IsOutdoors ? OutsideContainer : RoomContainer;

        //
        //private IEnumerable<Thing> PhysicalGas => Parent.ListerThings.AllThings.Where(t => t is SpreadingGas);
        public override void Notify_BorderThingAdded(Thing thing)
        {
            //Add any non 100% filled building
            if (thing is not Building b) return;
            if (!AtmosphericTransferWorker.IsPassBuilding(b)) return;

            //Generate Portal
            var otherRoom = b.NeighborRoomOf(Room);
            if (otherRoom == null) return;

            var portal = new AtmosphericPortal(b, this, otherRoom.GetRoomComp<RoomComponent_Atmospheric>());
            
            //If Portal Region - Add Portal as self
            if (Room.FirstRegion.type == RegionType.Portal)
            {
                selfPortal = portal;
                return;
            }

            //Otherwise add portal to border list
            portals.Add(portal);
            AtmosphericInfo.Notify_NewPortal(portal, this);
        }

        public override void Create(RoomTracker parent)
        {
            base.Create(parent);
            portals = new List<AtmosphericPortal>();

            AtmosphericInfo.Notify_NewComp(this);
            renderer = new RoomOverlay_Atmospheric();
        }

        public override void Disband(RoomTracker parent, Map map)
        {
            base.Disband(parent, map);
            AtmosphericInfo.Notify_DisbandedComp(this);
        }

        public override void Reset()
        {
        }

        public override void PreApply()
        {
            base.PreApply();
        }

        public override void FinalizeApply()
        {
            base.FinalizeApply();
            CreateContainer();
            MarkDirty();

            //
            Regenerate();
            if (Parent.IsProper)
            {
                TeleUpdateManager.Notify_EnqueueNewSingleAction(() => renderer.UpdateMesh(Room.Cells, Parent.MinVec, Parent.Size.x, Parent.Size.z));
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
            base.Notify_RoofClosed();
            
            AtmosphericInfo.RegenerateMapInfo();
            Data_GetCachedRegionalAtmosphere();
            //Data_CaptureOutsideAtmosphere();
        }

        public override void Notify_RoofOpened()
        {
            if (RoomContainer.TotalStored > 0)
            {
                RoomContainer.TransferAllTo(OutsideContainer);
            }
        }

        public override void Notify_RoofChanged()
        {
            base.Notify_RoofChanged();
        }

        private void MarkDirty()
        {
            dirtyMarks = Mathf.Clamp(dirtyMarks + 1, 0, Int32.MaxValue);
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

        //Value Manipulation
        public bool TryAddValueToRoom(AtmosphericDef def, float amount, out float actualAmount)
        {
            if (!AtmosMath.TryAddValueTo(CurrentContainer, def, amount, out actualAmount)) return false;
            Notify_AddedContainerValue(def, actualAmount);
            return true;
        }

        public bool TryRemoveValue(AtmosphericDef def, float amount, out float actualAmount)
        {
            return CurrentContainer.TryRemoveValue(def, amount, out actualAmount);
        }
        
        //RoomComp Generation
        private void Regenerate()
        {
            if (!IsDirty) return;

            container.Notify_RoomChanged(this, Parent.CellCount);
            Data_GetCachedRegionalAtmosphere();
        }

        private void Data_GetCachedRegionalAtmosphere()
        {
            //Assign starting atmosphere based on position
            if (AtmosphericInfo.Cache.TryGetAtmosphericValuesForRoom(Room, out var stack))
            {
                RoomContainer.Data_LoadFromStack(stack);
                foreach (var atmosphericDef in stack.AllTypes)
                {
                    renderer.TryRegisterNewOverlayPart(atmosphericDef);
                }
            }
        }

        private void Data_CaptureOutsideAtmosphere()
        {
            foreach (var atmosphericDef in OutsideContainer.AllStoredTypes)
            {
                var pct = OutsideContainer.StoredPercentOf(atmosphericDef);
                var newValue = Mathf.Round(container.Capacity * pct);

                TryAddValueToRoom(atmosphericDef, newValue, out _);
            }
        }

        private void CreateContainer()
        {
            container = new AtmosphericContainer(this);
            container.Notify_RoomChanged(this, Parent.CellCount);
        }

        public override void CompTick()
        {
            base.CompTick();
            
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
            if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest && !IsOutdoors && !IsConnector)
            {
                if (Room.CellCount <= 0) return;
                var cell = Room.Cells.First();
                DrawMenu(cell);
                if (openColorPicker)
                {
                    DrawColorPicker(cell);
                }
            }
        }

        private void DrawMenu(IntVec3 pos)
        {
            var v = DrawPosFor(pos) - new Vector2(0, 92);

            //46 - approx cell size when zoomed in
            var rect = new Rect(v.x, v.y, 230, 92);
            TWidgets.DrawColoredBox(rect, new Color(1, 1, 1, 0.125f), Color.white, 1);

            rect = rect.ContractedBy(5);
            Widgets.BeginGroup(rect);

            var innerRect = new Rect(0, 0, rect.width, rect.height);

            DrawAtmosContainerReadout(innerRect, RoomContainer, OutsideContainer);

            TWidgets.DoTinyLabel(innerRect.RightPartPixels(100).BottomPartPixels(20), $"[{Room.ID}][{AdjacentComps.Count}]|[{Parent.RoomPortals.Count}]:[{Parent.AdjacentTrackers.Count}]");

            var addRect = innerRect.BottomPartPixels(20).LeftPartPixels(40);
            if (Widgets.ButtonText(addRect, "Add"))
            {
                FloatMenu floatMenu = new FloatMenu(
                    DefDatabase<AtmosphericDef>.AllDefsListForReading.Select(d => new FloatMenuOption(d.defName, 
                        delegate { TryAddValueToRoom(d, container.Capacity * 0.25f, out _); })).ToList());

                Find.WindowStack.Add(floatMenu);
            }
            if (Widgets.ButtonText(new Rect(addRect.xMax, addRect.y, addRect.width, addRect.height), "Clear"))
            {
                RoomContainer.Data_Clear();
                //TryAddValue(DefDatabase<AtmosphericDef>.GetNamed("Oxygen"), 100, out _);
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

        /*
        private Texture2D GenColorTex(int width, int height)
        {
            if (colorTex != null) return colorTex;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {

                }
            }

            return colorTex;
        }
        */

        private void DrawAtmosContainerReadout(Rect rect, AtmosphericContainer container, AtmosphericContainer outside)
        {
            float height = 5;
            Widgets.BeginGroup(rect);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            foreach (var type in container.AllStoredTypes)
            {
                string label = $"{type.labelShort}: {container.TotalStoredOf(type)}({container.StoredPercentOf(type)}) | {outside.TotalStoredOf(type)}({outside.StoredPercentOf(type).ToStringPercent()})";
                Rect typeRect = new Rect(5, height, 10, 10);
                Vector2 typeSize = Text.CalcSize(label);
                Rect typeLabelRect = new Rect(20, height - 2, typeSize.x, typeSize.y);
                Widgets.DrawBoxSolid(typeRect, type.valueColor);
                Widgets.Label(typeLabelRect, label);
                height += 10 + 2;
            }
            Text.Font = default;
            Text.Anchor = default;
            Widgets.EndGroup();
        }

        private Vector2 DrawPosFor(IntVec3 pos)
        {
            Vector3 position = new Vector3((float)pos.x, (float)pos.y + AltitudeLayer.MetaOverlays.AltitudeFor(), (float)pos.z);
            Vector2 vector = Find.Camera.WorldToScreenPoint(position) / Prefs.UIScale;
            vector.y = (float)UI.screenHeight - vector.y;
            return vector;
        }

        public override void Draw()
        {
            if (Parent.IsProper)
            {
                renderer.UpdateTick();
                foreach (var renderDef in renderer.Defs)
                {
                    var value = RoomContainer.StoredPercentOf(renderDef);
                    if(value <= 0) continue;
                    renderer.DrawFor(renderDef, Parent.DrawPos, value);
                }
            }

            if(selfPortal.IsValid)
                GenDraw.DrawTargetingHighlight_Cell(selfPortal.Thing.Position);
        }

        public void Notify_AddedContainerValue(AtmosphericDef def, float value)
        {
            renderer.TryRegisterNewOverlayPart(def);
        }
    }
}
