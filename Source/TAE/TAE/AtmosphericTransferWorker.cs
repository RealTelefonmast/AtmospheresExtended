using RimWorld;
using TeleCore.FlowCore;
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
            return FinalizeTransferRate(thing, AtmosphereUtility.DefaultAtmosphericPassPercent(thing));
        }

        protected virtual float FinalizeTransferRate(Thing thing, float baseTransferRate)
        {
            return baseTransferRate;
        }

        internal virtual FlowResult CustomTransferFunc(AtmosphericContainer @from, AtmosphericContainer to, AtmosphericDef valueType, float value)
        {
            return FlowResult.ResultNormalFlow;
        }

        public void ProcessFlow(Rot4 flowDir, IntVec3 flowOut, RoomComponent_Atmospheric flowInto)
        {
            FleckMaker.ThrowExplosionCell(flowOut, flowInto.Map, FleckDefOf.ExplosionFlash, Color.cyan);
        }
    }
}
