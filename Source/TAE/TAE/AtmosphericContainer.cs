using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE
{
    public class AtmosphericContainer : BaseContainer<AtmosphericDef>
    {
        private RoomComponent_Atmospheric parentComp;
        private bool isOutdoorsContainer;
        private readonly HashSet<AtmosphericDef> _mapSourceTypes;

        public RoomComponent_Atmospheric AtmosParent => RoomParent.RoomComponent as RoomComponent_Atmospheric;
        public bool ParentIsDoorWay => RoomParent?.RoomComponent.IsDoorway ?? false;
        public bool HasParentRoom => parentComp != null;
        public bool IsSourceContainer => _mapSourceTypes.Count > 0;
        
        public bool IsOutdoors => isOutdoorsContainer || (HasParentRoom && AtmosParent.IsOutdoors);
        
        public bool IsSourceType(AtmosphericDef def)
        {
            return _mapSourceTypes.Contains(def);
        }
        
        public AtmosphericContainer(RoomComponent_Atmospheric parent, bool isOutdoor = false) : base(parent)
        {
            parentComp = parent;
            isOutdoorsContainer = isOutdoor;
            _mapSourceTypes = new HashSet<AtmosphericDef>();
        }

        public override bool AcceptsValue(AtmosphericDef valueType)
        {
            return true;
        }

        public override bool CanAccept(AtmosphericDef def)
        {
            if (!base.CanAccept(def)) return false;
            
            var totalPct = StoredPercentOf(def);
            foreach (var value in _storedValues)
            {
                var valDef = value.Key;
                if (valDef.displaceTags != null && valDef.displaceTags.Contains(def.atmosphericTag))
                {
                    var valPct = value.Value / Capacity;
                    return (1 - valPct) > totalPct;
                }
            }
            return true;
        }
        
        //
        public override void Notify_AddedValue(AtmosphericDef def, float value)
        {
            base.Notify_AddedValue(def, value);
            //Parent?.Notify_AddedContainerValue(def, value);

            //Tag Processing
            if (def.displaceTags != null)
            {
                var newPct = StoredPercentOf(def);
                var fittingTypes = AllStoredTypes.Where(t => def.displaceTags.Contains(t.atmosphericTag)).ToArray();
                for (var i = 0; i < fittingTypes.Length; i++)
                {
                    //var valPct = StoredPercentOf(fittingTypes[i]);
                    if (StoredPercentOf(fittingTypes[i]) > 1 - newPct)
                        TryRemoveValue(fittingTypes[i], value / fittingTypes.Length, out _);
                }
            }
        }

        protected override bool ShouldRemoveValue(AtmosphericDef valueType, float wantedValue)
        {
            return true;
        }

        //
        public override void UpdateContainerState(bool updateMetaData = false)
        {
            base.UpdateContainerState(updateMetaData);
            RoomParent?.RoomComponent.Map?.GetMapInfo<AtmosphericMapInfo>()?.Renderer?.Drawer_SetDirty();
        }

        //
        public void Data_RegisterSourceType(AtmosphericDef sourceType)
        {
            _mapSourceTypes.Add(sourceType);
        }

        public void Notify_RoomChanged(RoomComponent_Atmospheric parent, int roomCells)
        {
            TLog.Message($"[{parent?.Room?.ID}]Room Changed");
            parentComp = parent;
            Data_ChangeCapacity(roomCells * AtmosMath.CELL_CAPACITY);
        }
        
        //
        public bool TryReceiveFrom(NetworkContainer networkContainer, AtmosphericDef valueType, int value)
        {
            if (valueType.networkValue == null)
            {
                TLog.Warning($"AtmosphericDef: {valueType} does not have any NetworkValueDef defined to receive from {networkContainer?.ParentNetworkPart?.NetworkPart?.Network}");
                return false;
            }

            if (!networkContainer.AllStoredTypes.Contains(valueType.networkValue))
            {
                return false;
            }
            if (networkContainer.TryRemoveValue(valueType.networkValue, value, out float actual))
            {
                TryAddValue(valueType, actual, out _);
                return true;
            }

            return false;
        }

        
        public bool TryTransferTo(NetworkContainer networkContainer, AtmosphericDef valueType, float value)
        {
            if (valueType.networkValue == null)
            {
                TLog.Warning($"AtmosphericDef: {valueType} does not have any NetworkValueDef defined to transfer into {networkContainer?.ParentNetworkPart?.NetworkPart?.Network}");
                return false;
            }

            if (TryRemoveValue(valueType, value, out var actual))
            {
                networkContainer.TryAddValue(valueType.networkValue, actual, out _);
                return true;
            }
            return false;
        }
        
        public string RoomReadout()
        {
            return $"{0}ppc"; //parts per cell
        }
    }
}
