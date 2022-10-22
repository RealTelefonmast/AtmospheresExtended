using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using TAE.Static;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE
{
    public class AtmosphericTransferWorker
    {
        private AtmosphericDef def;
        
        public AtmosphericTransferWorker(SpreadingGasTypeDef def)
        {
            this.def = def.dissipateTo;
        }
        
        public AtmosphericTransferWorker(AtmosphericDef def)
        {
            this.def = def;
        }

        public float GetBaseTransferRate(Thing thing)
        {
            return FinalizeTransferRate(thing, DefaultAtmosphericPassPercent(thing));
        }

        protected virtual float FinalizeTransferRate(Thing thing, float baseTransferRate)
        {
            return baseTransferRate;
        }

        internal virtual FlowResult CustomTransferFunc(AtmosphericContainer @from, AtmosphericContainer to, AtmosphericDef valueType, float value)
        {
            return FlowResult.ResultNormalFlow;
        }

        //
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
                Building_Door door => door.Open ? 1 : FlowPctForDoor(door, fillage),
                Building_Vent vent => FlickUtility.WantsToBeOn(vent) ? 1f : 0f,
                Building_Cooler cooler => cooler.IsPoweredOn() ? 2f : 0f,
                { } b => flowPct,
                _ => 0.0f
            };
        }

        private static float FlowPctForDoor(Building_Door door, float fillage)
        {
            var categories = door.Stuff?.stuffProps?.categories;
            if (categories is null || categories.Count == 0) return 1f - fillage;

            //TODO:Add Stat for gas permeability
            var permeability = categories.Sum(c => AtmosphericData.PassPercentByStuff.GetValueOrDefault(c, 0)) / categories.Count;
            return (float)Math.Round(1f - (fillage * (1f - permeability)), 4);
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

        public void ProcessFlow(Rot4 flowDir, IntVec3 flowOut, RoomComponent_Atmospheric flowInto)
        {
            FleckMaker.ThrowExplosionCell(flowOut, flowInto.Map, FleckDefOf.ExplosionFlash, Color.cyan);
        }
    }
}
