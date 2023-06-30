using TeleCore.Network;
using TeleCore.Network.Flow.Values;
using UnityEngine;

namespace TAE;

public static class AtmosMath
{
    public static float GetCellCap(int maxDensity)
    {
        return Mathf.Pow(2, maxDensity) * AtmosResources.CELL_CAPACITY;
    }
}