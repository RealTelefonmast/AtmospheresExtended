using System.Collections.Generic;
using System.Linq;
using TAE.AtmosphericFlow;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE.Atmosphere.Rooms;

public class RoomComponent_Atmosphere : RoomComponent
{
    private RoomOverlay_Atmospheric _renderer;
    private AtmosphericMapInfo _atmosphericInfo;

    public AtmosphericMapInfo AtmosphericInfo => _atmosphericInfo;

    public AtmosphericVolume Volume
    {
        get
        {
            if (AtmosphericInfo.System.Relations.TryGetValue(this, out var volume))
            {
                return volume;
            }
            TLog.Warning($"Tried to access volume for room {Room.ID} from {AtmosphericInfo.System.Relations.Count} relations.");
            TLog.Warning($"{AtmosphericInfo.System.Relations.Select(s => s.Key.Room.ID).ToStringSafeEnumerable()}");
            return null;
        }
    }

    public bool IsOutdoors => Parent.IsOutside;

    public override void Init(RoomTracker[] previous = null)
    {
        _atmosphericInfo = Map.GetMapInfo<AtmosphericMapInfo>();
    }

    public override void PostInit(RoomTracker[] previous = null)
    {
        _renderer = new RoomOverlay_Atmospheric();
        AtmosphericInfo.Notify_AddRoomComp(this);
    }

    public override void Disband(RoomTracker parent, Map map)
    {
        if (_atmosphericInfo == null)
        {
            TLog.Warning("Tried to disband roomcomp without atmospheric info.");
            return;
        }
        AtmosphericInfo.Notify_RemoveRoomComp(this);
    }

    public override void Notify_Reused()
    {
        AtmosphericInfo.Notify_UpdateRoomComp(this);
    }

    //Note: Used to check whether a border thing is relevant in setting up a link to another roomcomp of the same type.
    public override bool IsRelevantLink(Thing thing)
    {
        return AtmosphereUtility.IsAtmosphericLink(thing);
    }

    #region Data Notifiers
    
    public void Notify_InterfacingThingChanged(RoomComponent_Atmosphere toOther, Thing thing, string signal)
    {
        //TODO use new interface lookup
        var interFace = _atmosphericInfo.System.Relations[this];
        var conns = _atmosphericInfo.System.Connections[interFace];
    }
    
    public override void Notify_PawnEnteredRoom(Pawn pawn)
    {
        var tracker = pawn.TryGetComp<Comp_PawnAtmosphereTracker>();
        if (tracker == null) return;
        tracker.Notify_EnteredAtmosphere(this);
    }

    public override void Notify_PawnLeftRoom(Pawn pawn)
    {
        var tracker = pawn.TryGetComp<Comp_PawnAtmosphereTracker>();
        if (tracker == null) return;
        tracker.Notify_Clear();
    }
    
    #endregion

    #region Updates

    public override void CompTick()
    {
        base.CompTick();
    }

    #endregion

    #region Rendering

    public override void Draw_DebugExtra(Rect inRect)
    {
        var bounds = inRect.ContractedBy(10);
        var center = bounds.center;
        Widgets.DrawHighlight(inRect);
        Widgets.DrawHighlight(bounds);
        
        var system = AtmosphericInfo.System;
        var volumeCount = system.Relations.Count;
        var width =  bounds.width;
        var rad = width/3f;
        var points = GetPointsOnCircle(rad, volumeCount);

        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];
            var pointRect = new Rect(center.x + (point.x - 2), center.y + (point.y - 2), 4, 4);
            var volume = system.Relations.ElementAt(i);
            
            Widgets.Label(new Rect(pointRect.x, pointRect.y-30, 40, 26), $"{volume.Key.Room.ID}");
            Widgets.DrawBoxSolid(pointRect, Color.white);
        }
    }
    
    public List<Vector2> GetPointsOnCircle(float radius, int totalPoints)
    {
        List<Vector2> points = new List<Vector2>();
        float angleStep = 360f / totalPoints;

        for(int i = 0; i < totalPoints; i++)
        {
            float angleInDegrees = angleStep * i;
            float angleInRadians = angleInDegrees * (Mathf.PI / 180f);

            float x = radius * Mathf.Cos(angleInRadians);
            float y = radius * Mathf.Sin(angleInRadians);

            points.Add(new Vector2(x, y));
        }

        return points;
    }

    public override void OnGUI()
    {
        base.OnGUI();
    }

    public override void Draw()
    {
        base.Draw();
    }

    #endregion
}
