using TeleCore;
using Verse;

namespace TAE
{
    public class Comp_AtmosphericNetworkStructure : Comp_NetworkStructure
    {
        private RoomComponent_Atmospheric atmosphericInt;

        //
        public NetworkSubPart AtmosNetwork { get; private set; }
        public RoomComponent_Atmospheric AtmosRoom
        {
            get
            {
                if (atmosphericInt == null || atmosphericInt.Parent.IsDisbanded)
                {
                    atmosphericInt = AtmosphericSource.GetRoomComp<RoomComponent_Atmospheric>();
                }
                return atmosphericInt;
            }
        }
        
        protected virtual Room AtmosphericSource => parent.GetRoom();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            AtmosNetwork = this[AtmosDefOf.AtmosphericNetwork];
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
