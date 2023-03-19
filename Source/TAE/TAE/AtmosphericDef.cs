using System;
using System.Collections.Generic;
using TAE.Static;
using TeleCore;
using TeleCore.FlowCore;
using UnityEngine;

namespace TAE
{
    public enum AtmosphericOccurence
    {
        AnyBiome,
        SpecificBiome
    }

    public class AtmosphericDef : FlowValueDef
    {
        //
        private AtmosphericTransferWorker workerInt;
        public Type transferWorker = typeof(AtmosphericTransferWorker);

        //
        public string labelShort;
        public Color valueColor;

        //The corresponding network value (if available)
        public NetworkValueDef networkValue;
        public SpreadingGasTypeDef dissipationGasDef;
        
        //Interaction Rules On Transfer (when going outside)
        public bool dissipatesIntoAir;
        public bool dissipatesIntoTerrain;

        public string atmosphericTag;
        public List<string> displaceTags;
        public List<string> dissipationTerrainTags;

        //Rendering
        public NaturalOverlayProperties naturalOverlay;
        public RoomOverlayProperties roomOverlay;
        public bool useRenderLayer = false;
        
        public AtmosphericTransferWorker TransferWorker => workerInt ??= (AtmosphericTransferWorker)Activator.CreateInstance(transferWorker, this);

        public override void PostLoad()
        {
            //
            base.PostLoad();
            AtmosphericReferenceCache.RegisterDef(this);
        }
    }
}
