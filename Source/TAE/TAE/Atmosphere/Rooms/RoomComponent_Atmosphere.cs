using TAE.AtmosphericFlow;
using TeleCore;
using Verse;

namespace TAE.Atmosphere.Rooms;

public class RoomComponent_Atmosphere : RoomComponent
{
    private AtmosphericVolume _volume;
    private RoomOverlay_Atmospheric _renderer;
    
    public AtmosphericMapInfo AtmosphericInfo => Map.GetMapInfo<AtmosphericMapInfo>();

    public AtmosphericVolume Volume => _volume;
    public bool IsOutdoors => Parent.IsOutside;

    public override void Init(RoomTracker[] previous = null)
    {
        base.Init(previous);
    }

    public override void PostInit(RoomTracker[] previous = null)
    {
        base.PostInit(previous);
        _volume = new AtmosphericVolume();
        _renderer = new RoomOverlay_Atmospheric();
        AtmosphericInfo.Notify_NewAtmosphericRoom(this);
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
    public override void CompTick()
    {
        base.CompTick();
    }

    public override void OnGUI()
    {
        base.OnGUI();
    }

    public override void Draw()
    {
        base.Draw();
    }
}
