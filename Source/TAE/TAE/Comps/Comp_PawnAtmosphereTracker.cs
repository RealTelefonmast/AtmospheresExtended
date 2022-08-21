using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore;
using Verse;

namespace TAE
{
    public class Comp_PawnAtmosphereTracker : ThingComp
    {
        private static readonly Dictionary<Pawn, Comp_PawnAtmosphereTracker> OneOffs = new();
        private RoomComponent_Atmospheric currentAtmosphere;

        public Pawn Pawn => parent as Pawn;

        public bool IsOutside => currentAtmosphere.IsOutdoors;

        public RoomComponent_Atmospheric RoomComp => currentAtmosphere;
        public AtmosphericContainer Container => currentAtmosphere.CurrentContainer;

        public static Comp_PawnAtmosphereTracker CompFor(Pawn pawn)
        {
            if (OneOffs.TryGetValue(pawn, out var value))
            {
                return value;
            }
            return null;
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            if (OneOffs.TryAdd(Pawn, this))
            {
                //...
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public bool IsInAtmosphere(AtmosphericDef def)
        {
            return Container.TotalStoredOf(def) > 0;
        }

        //
        public void Notify_EnteredAtmosphere(RoomComponent_Atmospheric atmosphere)
        {
            currentAtmosphere = atmosphere;
        }

        //Implies leaving outside
        public void Notify_Clear()
        {
            currentAtmosphere = null;
        }
    }
}
