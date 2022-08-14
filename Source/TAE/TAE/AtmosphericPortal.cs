using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore;
using Verse;

namespace TAE
{
    public struct AtmosphericPortal
    {
        private readonly Building connector;
        private readonly RoomComponent_Atmospheric[] connections;
        private readonly Rot4[] connectionDirections;
        private Rot4 flowDirection;

        private bool _isFlowing;

        public bool ConnectsToOutside => connections[0].IsOutdoors || connections[1].IsOutdoors;
        public bool IsOutdoors => connections[0].IsOutdoors && connections[1].IsOutdoors;

        public bool IsTransfering => _isFlowing;
        public bool IsValid => connector != null;
        public Thing Thing => connector;

        internal AtmosphericPortal(Building building, RoomComponent_Atmospheric roomA, RoomComponent_Atmospheric roomB)
        {
            connector = building;
            connections = new[] {roomA, roomB};
            connectionDirections = new Rot4[2];
            flowDirection = Rot4.Invalid;
            _isFlowing = false;

            //Get Directions
            for (int i = 0; i < 4; i++)
            {
                var cell = building.Position + GenAdj.CardinalDirections[i];
                var room = cell.GetRoomFast(building.Map);
                if(room == null) continue;
                if (roomA.Room == room)
                    connectionDirections[0] = cell.Rot4Relative(building.Position);
                if (roomB.Room == room)
                    connectionDirections[1] = cell.Rot4Relative(building.Position);
            }
        }

        public void TryEqualize()
        {
            _isFlowing = false;

            var from = connections[0].RoomContainer;
            var to = connections[1].RoomContainer;

            if (ConnectsToOutside)
            {
                var outsideConn = connections[0].IsOutdoors ? connections[0] : connections[1];
                var otherConn = Opposite(outsideConn);
                if (otherConn.IsOutdoors) return;
                from = outsideConn.OutsideContainer;
                to = otherConn.RoomContainer;
            }

            //
            foreach (var atmosDef in from.AllStoredTypes.Concat(to.AllStoredTypes))
            {
                var transferWorker = atmosDef.TransferWorker;
                if (transferWorker.TryTransferVia(this, from, to, atmosDef))
                {
                    _isFlowing = true; 
                    //flowDirection = positiveFlow ? connectionDirections[0].Opposite : connectionDirections[1].Opposite;
                }
            }
        }

        //
        public RoomComponent_Atmospheric Opposite(RoomComponent_Atmospheric other)
        {
            return other == connections[0] ? connections[1] : connections[0];
        }

        public bool Connects(RoomComponent_Atmospheric toThis)
        {
            Log.Message($"Checking connection to {toThis.Room.ID} between {connections[0].Room.ID} and {connections[1].Room.ID}");
            return toThis == connections[0] || toThis == connections[1];
        }
        public override string ToString()
        {
            return $"{connections[0].Room.ID} -[{Thing}]-> {connections[1].Room.ID}";
        }
    }
}
