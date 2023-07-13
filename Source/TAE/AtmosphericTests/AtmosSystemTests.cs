using System.Collections.Generic;
using NUnit.Framework;
using RimWorld;
using TAE.Atmosphere.Rooms;
using Verse;

namespace AtmosphericTests
{
    [TestFixture]
    public class AtmosSystemTests
    {
        [Test]
        public void Test()
        {
            var system = new AtmosphericSystem(10*10);
            system.Notify_AddRoomComp(new RoomComponent_Atmosphere());   
            //hmmm...
        }
    }
}