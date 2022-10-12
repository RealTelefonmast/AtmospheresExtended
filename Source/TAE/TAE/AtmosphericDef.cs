using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TAE.Static;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE
{
    public enum AtmosphericOccurence
    {
        AnyBiome,
        SpecificBiome
    }

    public class AtmosphericDef : Def
    {
        //
        private AtmosphericTransferWorker workerInt;
        public Type transferWorker = typeof(AtmosphericTransferWorker);

        //Maximum saturation relative to the room's capacity
        public float maxSaturation = 1f;
        //The rate at which gas flows between rooms
        public float viscosity = 1;

        //
        public string labelShort;
        public Color valueColor;

        //The corresponding network value (if available)
        public NetworkValueDef networkValue;

        //Interaction Rules
        public bool dissipatesIntoAir;
        public bool dissipatesIntoTerrain;

        public string atmosphericTag;
        public List<string> displaceTags;
        public List<string> dissipationTerrainTags;

        //Rendering
        public NaturalOverlayProperties naturalOverlay;
        public RoomOverlayProperties roomOverlay;
        public bool useRenderLayer = false;

        //Runtime
        public float FlowRate => 1f / viscosity;

        public AtmosphericTransferWorker TransferWorker => workerInt ??= (AtmosphericTransferWorker)Activator.CreateInstance(transferWorker, this);

        public override void PostLoad()
        {
            //
            AtmosphericReferenceCache.RegisterDef(this);
        }
    }
}
