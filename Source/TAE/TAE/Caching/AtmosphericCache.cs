using System.Collections.Generic;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE.Caching;

/// <summary>
/// Wraps the scribed Atmospheric data into an object for data encapsulation in XML.
/// </summary>
public class AtmosphericCache : IExposable
{
    private AtmosphericScriber _scriber;

    public AtmosphericCache(AtmosphericMapInfo map)
    {
        _scriber = new AtmosphericScriber(map);
    }

    public void ExposeData()
    {
        _scriber.ScribeData();
    }
}