using Verse;

namespace TAE
{
    public class CompProperties_AtmosphericSource : CompProperties
    {
        public int pushAmount;
        public int pushInterval;
        public AtmosphericValueDef AtmosphericValueDef;

        public CompProperties_AtmosphericSource()
        {
            this.compClass = typeof(Comp_AtmosphericSource);
        }
    }
}
