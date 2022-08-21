using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore;
using Verse;

namespace TAE
{
    internal class PawnCompInjection : DefInjectBase
    {
        public override void OnPawnInject(ThingDef pawnDef)
        {
            pawnDef.comps.Add(new CompProperties_PathFollowerExtra());
            pawnDef.comps.Add(new CompProperties_PawnAtmosphereTracker());
        }
    }
}
