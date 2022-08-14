using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
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

    public class AtmosphereBiomeExtension : DefModExtension
    {
        public List<DefValue<AtmosphericDef, float>> uniqueAtmospheres;
    }

    public class AtmosphericBiomeRuleSetDef : Def
    {
        public AtmosphericOccurence occurence = AtmosphericOccurence.AnyBiome;
        public List<BiomeDef> biomes;
        public List<DefValue<AtmosphericDef, float>> atmospheres;
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
        //The base value occuring naturally "Outside"


        public bool dissipatesIntoAir;
        public bool dissipatesIntoTerrain;

        //
        public string labelShort;
        public Color valueColor;

        //The corresponding network value (if available)
        public NetworkValueDef networkValue;

        public string atmosphericTag;
        public List<string> replaceTags;
        public List<string> dissipationTerrainTags;

        public float FlowRate => 1f / viscosity;

        public AtmosphericTransferWorker TransferWorker => workerInt ??= (AtmosphericTransferWorker)Activator.CreateInstance(transferWorker);

    }
}
