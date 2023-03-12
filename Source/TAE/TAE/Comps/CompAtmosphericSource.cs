using TeleCore;
using Verse;

namespace TAE
{
    public class CompAtmosphericSource : ThingComp, IAtmosphericSource
    {
        public CompProperties_AtmosphericSource Props => (CompProperties_AtmosphericSource)props;
        public Thing Thing => parent;
        public Room Room => parent.GetRoomIndirect();

        //
        public AtmosphericDef AtmosphericDef => Props.atmosphericDef;
        public int PushInterval => Props.pushInterval;
        public int PushAmount => Props.pushAmount;

        public virtual bool IsActive => true;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Thing.Map.GetMapInfo<AtmosphericMapInfo>().RegisterSource(this);
        }

        public override void PostDeSpawn(Map map)
        {
            map.GetMapInfo<AtmosphericMapInfo>().DeregisterSource(this);
            base.PostDeSpawn(map);
        }
    }
}
