using TAE.Atmosphere.Rooms;
using TeleCore;
using TeleCore.Network.Data;
using Verse;

namespace TAE
{
    public class Comp_AtmosphericNetworkStructure : Comp_Network
    {
        private RoomComponent_Atmosphere atmosphericInt;

        //
        public INetworkPart AtmosNetwork { get; private set; }
        public RoomComponent_Atmosphere AtmosRoom
        {
            get
            {
                if (atmosphericInt == null || atmosphericInt.Parent.IsDisbanded)
                {
                    atmosphericInt = AtmosphericSource.GetRoomComp<RoomComponent_Atmosphere>();
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

    public class CompProperties_ANS : CompProperties_Network
    {
        public CompProperties_ANS()
        {
            this.compClass = typeof(Comp_AtmosphericNetworkStructure);
        }
    }
}
