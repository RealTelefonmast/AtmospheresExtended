using TAE.AtmosphericFlow;
using TeleCore;
using Verse;

namespace TAE.Atmosphere.Rooms;

public class RoomComponent_Atmosphere : RoomComponent
{
    private RoomOverlay_Atmospheric _renderer;
    private AtmosphericMapInfo _atmosphericInfo;

    public AtmosphericMapInfo AtmosphericInfo => _atmosphericInfo;

    public AtmosphericVolume Volume => AtmosphericInfo.System.Relations[this];
    public bool IsOutdoors => Parent.IsOutside;

    public override void Init(RoomTracker[] previous = null)
    {
        base.Init(previous);
    }

    public override void PostInit(RoomTracker[] previous = null)
    {
        base.PostInit(previous);
        _renderer = new RoomOverlay_Atmospheric();
        AtmosphericInfo.Notify_AddRoomComp(this);
    }

    public override void Disband(RoomTracker parent, Map map)
    {
        base.Disband(parent, map);
        AtmosphericInfo.Notify_RemoveRoomComp(this);
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
