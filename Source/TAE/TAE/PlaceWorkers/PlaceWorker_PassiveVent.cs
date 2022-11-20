using System.Collections.Generic;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE;

public class PlaceWorker_PassiveVent : PlaceWorker
{
    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        base.DrawGhost(def, center, rot, ghostCol, thing);
        var intakeCell = Comp_ANS_PassiveVent.IntakePos(center, rot);
        GenDraw.DrawFieldEdges(new List<IntVec3>
        {
            intakeCell
        }, TColor.BlueHighlight, null);
    }

    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
    {
        var intakeCell = Comp_ANS_PassiveVent.IntakePos(loc, rot);

        if (intakeCell.GetEdifice(map) != null)
            return "TELE.PassiveVent.PlacingBlocked".Translate();
        return base.AllowsPlacing(checkingDef, loc, rot, map, thingToIgnore, thing);
    }
}