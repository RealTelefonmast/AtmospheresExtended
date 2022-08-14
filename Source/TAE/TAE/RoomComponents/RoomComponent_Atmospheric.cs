using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TAE.Rendering;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE
{
    public class RoomComponent_Atmospheric : RoomComponent
    {
        //
        private int dirtyMarks;

        //
        private AtmosphericContainer container;
        private AtmosphericPortal selfPortal;

        private RoomOverlay_Atmospheric renderer;

        //
        public bool IsOutdoors => Parent.IsOutside;
        public bool IsDirty => dirtyMarks > 0;
        public bool IsConnector => selfPortal.IsValid;

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
            AtmosphericInfo.Notify_NewPortal(portal, this);
        }

        public override void Create(RoomTracker parent)
        {
            base.Create(parent);
            TLog.Message($"[TEA] Creating new Atmospheric Room: {Room.ID}");
            AtmosphericInfo.Notify_NewComp(this);

            renderer = new RoomOverlay_Atmospheric();
        }

        public override void Disband(RoomTracker parent, Map map)
        {
            base.Disband(parent, map);
            TLog.Message($"[TEA] Disbanding old Atmospheric Room: {Room.ID}");
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
                renderer.UpdateMesh(Room.Cells, Parent.MinVec, Parent.Size.x, Parent.Size.z);

            //--
        }

        public override void Notify_Reused()
        {
            base.Notify_Reused();
            Log.Message($"[TEA] Reusing Atmospheric Room: {Room.ID}[{IsOutdoors}]");
        }

        public override void Notify_RoofClosed()
        {
            base.Notify_RoofClosed();
            AtmosphericInfo.RegenerateMapInfo();
            Data_CaptureOutsideAtmosphere();
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

        //Value Manipulation
        public bool TryAddValue(AtmosphericDef def, float amount, out float actualAmount)
        {
            //Log.Message($"Adding {value} ({amount}) to RoomComp {Room.ID} | Outside: {Outside}");
            return CurrentContainer.TryAddValue(def, amount, out actualAmount);
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
            //Assign starting pollution based on position
            if (AtmosphericInfo.Cache.TryGetAtmosphericValuesForRoom(Room, out var stack))
            {
                RoomContainer.Data_LoadFromStack(stack);
            }
        }

        private void Data_CaptureOutsideAtmosphere()
        {
            foreach (var atmosphericDef in OutsideContainer.AllStoredTypes)
            {
                var pct = OutsideContainer.StoredPercentOf(atmosphericDef);
                var newValue = Mathf.Round(container.Capacity * pct);

                TryAddValue(atmosphericDef, newValue, out _);
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
        public override void OnGUI()
        {
            if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest && !IsOutdoors && !IsConnector)
            {
                if (Room.CellCount <= 0) return;
                DrawMenu(Room.Cells.First());
            }
        }

        private void DrawMenu(IntVec3 pos)
        {
            var v = DrawPosFor(pos) - new Vector2(0, 92);

            //46 - approx cell size when zoomed in
            var rect = new Rect(v.x, v.y, 138, 92);
            TWidgets.DrawColoredBox(rect, new Color(1, 1, 1, 0.125f), Color.white, 1);

            rect = rect.ContractedBy(5);
            Widgets.BeginGroup(rect);

            var innerRect = new Rect(0, 0, rect.width, rect.height);

            DrawAtmosContainerReadout(innerRect, RoomContainer, OutsideContainer);

            TWidgets.DoTinyLabel(innerRect.RightPartPixels(20).BottomPartPixels(20), $"[{Room.ID}]");

            if (Widgets.ButtonText(innerRect.BottomPartPixels(20).LeftPartPixels(40), "Add"))
            {
                TryAddValue(DefDatabase<AtmosphericDef>.GetNamed("Oxygen"), 100, out _);
            }

            Widgets.EndGroup();
        }

        private void DrawAtmosContainerReadout(Rect rect, AtmosphericContainer container, AtmosphericContainer outside)
        {
            float height = 5;
            Widgets.BeginGroup(rect);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            foreach (var type in container.AllStoredTypes)
            {
                string label = $"{type.labelShort}: {container.TotalStoredOf(type)} | {outside.TotalStoredOf(type)}";
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
                renderer.Draw(Parent.DrawPos,1);
            }
            if(selfPortal.IsValid)
                GenDraw.DrawTargetingHighlight_Cell(selfPortal.Thing.Position);
        }
    }
}
