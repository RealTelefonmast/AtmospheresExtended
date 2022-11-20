using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore;
using Verse;

namespace TAE
{
    public class Comp_AtmosphericNetworkStructure : Comp_NetworkStructure
    {
        private RoomComponent_Atmospheric atmosphericInt;

        //
        public NetworkSubPart AtmosphericComp { get; private set; }
        public RoomComponent_Atmospheric Atmospheric
        {
            get
            {
                if (atmosphericInt == null || atmosphericInt.Parent.IsDisbanded)
                {
                    atmosphericInt = parent.GetRoom().GetRoomComp<RoomComponent_Atmospheric>();
                }
                return atmosphericInt;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            AtmosphericComp = this[AtmosDefOf.AtmosphericNetwork];
        }

        public override void CompTick()
        {
            base.CompTick();
        }
    }

    public class CompProperties_ANS : CompProperties_NetworkStructure
    {
        public CompProperties_ANS()
        {
            this.compClass = typeof(Comp_AtmosphericNetworkStructure);
        }
    }
}
