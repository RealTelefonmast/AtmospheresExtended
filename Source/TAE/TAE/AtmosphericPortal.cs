using System;
using System.Collections.Generic;
using System.Linq;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE
{
    public enum AtmosPortalFlow
    {
        None,
        Positive,
        Negative,
    }
    
    public struct AtmosphericPortal
    {
        private readonly Building connector;
        private readonly RoomComponent_Atmospheric[] connections;
        private readonly Rot4[] connectionDirections;

        private Dictionary<AtmosphericDef, FlowResult> lastResultByDef = new();

        public bool ConnectsToOutside => connections[0].IsOutdoors || connections[1].IsOutdoors;
        public bool ConnectsToSame => connections[0].IsOutdoors && connections[1].IsOutdoors || connections[0] == connections[1];

        public bool IsValid => connector != null;
        public Thing Thing => connector;

        public RoomComponent_Atmospheric this[int i] => connections[i];

        internal AtmosphericPortal(Building building, RoomComponent_Atmospheric roomA, RoomComponent_Atmospheric roomB)
        {
            connector = building;
            connections = new[] {roomA, roomB};
            connectionDirections = new Rot4[2];

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

        public bool NeedsEqualizing(AtmosphericDef def, out AtmosPortalFlow flow, out float diffAbs)
        {
            flow = AtmosPortalFlow.None;
            diffAbs = 0f;
            if (connections[0].IsOutdoors && connections[1].IsOutdoors) return false;
            var fromTotal = connections[0].CurrentContainer.TotalStoredOf(def);
            var toTotal = connections[1].CurrentContainer.TotalStoredOf(def);

            var fromPct = connections[0].CurrentContainer.StoredPercentOf(def);
            var toPct = connections[1].CurrentContainer.StoredPercentOf(def);

            var totalDiff = Mathf.Abs(fromTotal - toTotal);
            var diffPct = fromPct - toPct;

            flow = diffPct switch
            {
                > 0 => AtmosPortalFlow.Positive,
                < 0 => AtmosPortalFlow.Negative,
                _ => AtmosPortalFlow.None
            };
            diffAbs = Mathf.Abs(diffPct);
            return diffAbs > 0 && totalDiff >= 1;
        }

        private bool PreventFlowBack(RoomComponent_Atmospheric to, AtmosphericDef ofDef)
        {
            int checkIndex = IndexOf(to);
            if (lastResultByDef.TryGetValue(ofDef, out var result))
            {
                return result.FlowsToOther && result.FromIndex == checkIndex;
            }
            return false;
        }

        public void TryEqualize()
        {
            //Ignore connections to same
            if (ConnectsToSame) return;

            //Select containers
            var from = connections[0].CurrentContainer;
            var to = connections[1].CurrentContainer;

            //Go through all common types
            //var tempTypes = from.AllStoredTypes.Union(to.AllStoredTypes).ToArray();
            foreach (var atmosDef in from.AllStoredTypes.Union(to.AllStoredTypes))
            {
                var transferWorker = atmosDef.TransferWorker;

                var flowResult = transferWorker.TryTransferVia(this, from, to, atmosDef);
                if (flowResult.FlowsToOther)
                {
                    SetFlowFor(atmosDef, flowResult);

                    if (flowResult.ToIndex < 0) continue;
                    var connDir = connectionDirections[flowResult.ToIndex];
                    transferWorker.ProcessFlow(connDir, Thing.Position + connDir.FacingCell, connections[flowResult.ToIndex]);
                }
            }
        }

        private void SetFlowFor(AtmosphericDef def, FlowResult result)
        {
            if (lastResultByDef.TryGetValue(def, out _))
            {
                lastResultByDef[def] = result;
                return;
            }

            //
            lastResultByDef.Add(def, result);
        }

        //
        public int IndexOf(RoomComponent_Atmospheric comp)
        {
            return connections[0] == comp ? 0 : 1;
        }

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
