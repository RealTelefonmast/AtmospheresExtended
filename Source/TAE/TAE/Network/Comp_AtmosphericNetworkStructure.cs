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

        public NetworkSubPart AtmosphericComp => null; //this[TiberiumDefOf.AtmosphericNetwork];

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
