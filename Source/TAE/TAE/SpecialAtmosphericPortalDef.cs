using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace TAE
{
    public class SpecialAtmosphericPortalDef
    {
        private AtmosphericPortalWorker workerInt;

        public Type transferWorker = typeof(AtmosphericPortalWorker);
        public ThingDef portalThingDef;

        //
        public AtmosphericPortalWorker Worker => workerInt ??= (AtmosphericPortalWorker)Activator.CreateInstance(transferWorker);

    }

    public class AtmosphericPortalWorker
    {
        public virtual float PassPercent(Thing thing)
        {
            return 0;
        }
    }

    public static class AtmosPortalData
    {
        private static readonly Dictionary<ThingDef, SpecialAtmosphericPortalDef> workerByDef = new();
        private static readonly HashSet<ThingDef> specialPassBuildings = new ();

        public static bool TryGetWorkerFor(ThingDef def, out SpecialAtmosphericPortalDef portalDef)
        {
            return workerByDef.TryGetValue(def, out portalDef);
        }

        public static bool IsPassBuilding(ThingDef def)
        {
            return specialPassBuildings.Contains(def);
        }

        internal static void RegisterPortalDef(SpecialAtmosphericPortalDef def)
        {
            if (workerByDef.TryAdd(def.portalThingDef, def))
            {
                specialPassBuildings.Add(def.portalThingDef);
                return;
            }
            Log.Warning($"[TAE] Added {def} with existing ThingDef {def.portalThingDef} already in Dictionary.");
        }
    }
}
