using System.Linq;
using TAE.AtmosphericFlow;
using TeleCore;
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
        AtmosphericInfo.Notify_RemoveRoomComp(this);
    }

    public override void Notify_Reused()
    {
        AtmosphericInfo.Notify_UpdateRoomComp(this);
    }

    #region Data Notifiers

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
