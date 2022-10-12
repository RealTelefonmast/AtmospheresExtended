using System;
using System.Collections.Generic;
using System.Linq;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE;

public class AtmosphereRenderer
{
    private Map map;
    private CellBoolDrawer drawerInt;
    private readonly List<AtmosphericDef> atmospheres;

    private AtmosphericDef selectedAtmosphere;
    
    public AtmosphereRenderer(Map map)
    {
        this.map = map;
        atmospheres = DefDatabase<AtmosphericDef>.AllDefs.Where(t => t.useRenderLayer).ToList();
    }
    
    private CellBoolDrawer Drawer => drawerInt ??= new CellBoolDrawer(CellBoolDrawerGetBoolInt, CellBoolDrawerColorInt, CellBoolDrawerGetExtraColorInt, map.Size.x, map.Size.z, 3610);

    private float AtmosphereAt(IntVec3 loc)
    {
        return CalculateAtmosphereAt(loc);
    }

    public float CalculateAtmosphereAt(IntVec3 loc, AtmosphericDef def = null)
    {
        var room = loc.GetRoomFast(map);
        var roomComp = room?.GetRoomComp<RoomComponent_Atmospheric>();
        if (roomComp != null)
        {
            return roomComp.CurrentContainer.StoredPercentOf(def ?? selectedAtmosphere);
        }
        return 0;
    }
    
    public void AtmosphereDrawerUpdate()
    {
        if (AtmosphereMod.Mod.Settings.DrawAtmospheres)
        {
            Drawer.MarkForDraw();
            Drawer.CellBoolDrawerUpdate();
        }
    }

    public void Drawer_SetDirty()
    {
        Drawer.SetDirty();
    }
    
    private bool CellBoolDrawerGetBoolInt(int index)
    {
        var intVec = CellIndicesUtility.IndexToCell(index, map.Size.x);
        return !intVec.Filled(map) && !intVec.Fogged(map) && AtmosphereAt(intVec) > 0.69f;
    }
    
    private Color CellBoolDrawerColorInt()
    {
        return Color.white;
    }
    
    public Color CellBoolDrawerGetExtraColorInt(int index)
    {
        return Color.Lerp(Color.clear, selectedAtmosphere.valueColor, AtmosphereAt(CellIndicesUtility.IndexToCell(index, map.Size.x)));
    }
    
    public Color CellBoolDrawerGetExtraColorInt(int index, AtmosphericDef def)
    {
        return Color.Lerp(Color.clear, def.valueColor, AtmosphereAt(CellIndicesUtility.IndexToCell(index, map.Size.x)));
    }
    
    //Selection
    public void OpenAtmosphereLayerMenu(Action<bool> callback)
    {
        List<FloatMenuOption> options = new List<FloatMenuOption>();
        foreach (var atmosphericDef in atmospheres)
        {
            options.Add(new FloatMenuOption(atmosphericDef.LabelCap, delegate
            {
                var diff = atmosphericDef != selectedAtmosphere;
                callback.Invoke(diff);
                if(diff)
                    selectedAtmosphere = atmosphericDef;
                else
                    selectedAtmosphere = null;
            }));
        }
        
        Find.WindowStack.Add(new FloatMenu(options, "TAE_SelectLayer".Translate()));
    }
}