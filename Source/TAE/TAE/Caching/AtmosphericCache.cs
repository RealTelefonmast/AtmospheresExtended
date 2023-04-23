using System.Collections.Generic;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE.Caching
{
    public class AtmosphericCache : IExposable
    {
        private Map map;
        internal readonly AtmosphericScriber scriber;

        public AtmosphericMapInfo AtmosphericMapInfo => map.GetMapInfo<AtmosphericMapInfo>();

        public AtmosphericCache(Map map)
        {
            this.map = map;
            scriber = new AtmosphericScriber(map);
        }

        public void ExposeData()
        {
            scriber.ScribeData();
        }
    }
}
