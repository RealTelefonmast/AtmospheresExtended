using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TAE
{
    public class CompProperties_AtmosphericSource : CompProperties
    {
        public int pushAmount;
        public int tickInterval;
        public AtmosphericDef atmosphericDef;

        public CompProperties_AtmosphericSource()
        {
            this.compClass = typeof(CompAtmosphericSource);
        }
    }
}
