using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TeleCore;
using Verse;

namespace TAE
{
    public class AtmosphericTransferWorker
    {
        private AtmosphericDef def;

        public AtmosphericTransferWorker(AtmosphericDef def)
        {
            this.def = def;
        }

        public float GetBaseTransferRate(Thing thing)
        {
            return FinalizeTransferRate(thing, AtmosphericPassPercent(thing));
        }

        protected virtual float FinalizeTransferRate(Thing thing, float baseTransferRate)
        {
            return baseTransferRate;
        }

        //
        internal static float AtmosphericPassPercent(Thing forThing)
        {
            if (forThing == null) return 1;
            
            //Custom Worker Subroutine
            if (AtmosPortalData.TryGetWorkerFor(forThing.def, out var worker))
            {
                return worker.Worker.PassPercent(forThing);
            }

            //
            var fullFillage = forThing.def.Fillage == FillCategory.Full;
            var fillage = forThing.def.fillPercent;
            var flowPct = fullFillage ? 0 : 1f - fillage;
            return forThing switch
            {
                Building_Door door => door.Open ? 1 : flowPct,
                Building_Vent vent => FlickUtility.WantsToBeOn(vent) ? 1 : 0,
                Building_Cooler cooler => cooler.IsPoweredOn() ? 1.5f : 0,
                { } b => flowPct,
                _ => 0
            };
        }

        internal static bool IsPassBuilding(Building building)
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

        public bool TryTransferVia(AtmosphericPortal atmosphericPortal, AtmosphericContainer @from, AtmosphericContainer to, AtmosphericDef atmosDef)
        {
            var diff = (from.StoredPercentOf(atmosDef) - to.StoredPercentOf(atmosDef));
            var diffAbs = Math.Abs(diff);
            if (!(diffAbs > 0.01f)) return false;
            var positiveFlow = diff > 0;

            var sendingContainer = positiveFlow ? from : to;
            var receivingContainer = positiveFlow ? to : from;
            var flowAmount = AtmosphericMapInfo.CELL_CAPACITY * diffAbs * GetBaseTransferRate(atmosphericPortal.Thing) * atmosDef.FlowRate;
            return sendingContainer.TryTransferTo(receivingContainer, atmosDef, flowAmount);
        }
    }
}
