using System.Collections.Generic;
using System.Linq;
using TeleCore;
using TeleCore.FlowCore;

namespace TAE
{
    public class AtmosphericContainer : ValueContainer<AtmosphericDef, IContainerHolderRoom<AtmosphericDef>>
    {
        private RoomComponent_Atmospheric parentComp;
        private bool isOutdoorsContainer;
        private readonly HashSet<AtmosphericDef> _mapSourceTypes;

        public RoomComponent_Atmospheric AtmosParent => Holder.RoomComponent as RoomComponent_Atmospheric;
        public bool ParentIsDoorWay => Holder?.RoomComponent.IsDoorway ?? false;
        public bool HasParentRoom => parentComp != null;
        public bool IsSourceContainer => _mapSourceTypes.Count > 0;
        
        public bool IsOutdoors => isOutdoorsContainer || (HasParentRoom && AtmosParent.IsOutdoors);
        
        public bool IsSourceType(AtmosphericDef def)
        {
            return _mapSourceTypes.Contains(def);
        }
        
        public AtmosphericContainer(RoomComponent_Atmospheric parent, ContainerConfig config, bool isOutdoor = false) : base(config, parent)
        {
            parentComp = parent;
            isOutdoorsContainer = isOutdoor;
            _mapSourceTypes = new HashSet<AtmosphericDef>();
        }
        
        public override bool CanReceiveValue(AtmosphericDef valueDef)
        {
            if (!base.CanReceiveValue(valueDef)) return false;
            
            var totalPct = StoredPercentOf(valueDef);
            foreach (var value in storedValues)
            {
                var valDef = value.Key;
                if (valDef.displaceTags != null && valDef.displaceTags.Contains(valueDef.atmosphericTag))
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

            //TODO: Check displacement between gasses
            //TODO: Figure out liquid behaviours
            //Tag Processing
            if (def.displaceTags != null)
            {
                var newPct = StoredPercentOf(def);
                var fittingTypes = StoredDefs.Where(t => def.displaceTags.Contains(t.atmosphericTag)).ToArray();
                for (var i = 0; i < fittingTypes.Length; i++)
                {
                    //var valPct = StoredPercentOf(fittingTypes[i]);
                    if (StoredPercentOf(fittingTypes[i]) > 1 - newPct)
                       _ = TryRemoveValue(fittingTypes[i], value / fittingTypes.Length);
                }
            }
        }

        //
        public override void Notify_ContainerStateChanged(NotifyContainerChangedArgs<AtmosphericDef> stateChangeArgs)
        {
            base.Notify_ContainerStateChanged(stateChangeArgs);
            if (Holder?.RoomComponent?.Map?.GetMapInfo<AtmosphericMapInfo>()?.Renderer == null)
            {
                TLog.Warning($"Could not SetDirty AtmosphericMap Renderer because parent chain is null:" +
                             $"\nHolder: {Holder != null}" +
                             $"\nRoomComp: {Holder?.RoomComponent != null}" +
                             $"\nMap: {Holder?.RoomComponent?.Map != null}" +
                             $"\nMapInfo: {Holder?.RoomComponent?.Map?.GetMapInfo<AtmosphericMapInfo>() != null}" +
                             $"\nRenderer: {Holder?.RoomComponent?.Map?.GetMapInfo<AtmosphericMapInfo>()?.Renderer != null}");
                return;
            }
            Holder.RoomComponent.Map?.GetMapInfo<AtmosphericMapInfo>()?.Renderer?.Drawer_SetDirty();
        }

        //
        public void Data_RegisterSourceType(AtmosphericDef sourceType)
        {
            _mapSourceTypes.Add(sourceType);
        }

        public void Notify_RoomChanged(RoomComponent_Atmospheric parent, int roomCells)
        {
            parentComp = parent;
            ChangeCapacity(roomCells * AtmosMath.CELL_CAPACITY);
        }
        
        //
        public bool TryReceiveFrom(NetworkContainer networkContainer, AtmosphericDef valueType, int value)
        {
            if (valueType.networkValue == null)
            {
                TLog.Warning($"AtmosphericDef: {valueType} does not have any NetworkValueDef defined to receive from {networkContainer?.Holder?.NetworkPart?.Network}");
                return false;
            }

            if (!networkContainer.StoredDefs.Contains(valueType.networkValue))
            {
                return false;
            }
            if (networkContainer.TryRemoveValue(valueType.networkValue, value, out var actual))
            {
                TryAddValue(valueType, actual.ActualAmount, out _);
                return true;
            }

            return false;
        }

        
        public bool TryTransferTo(NetworkContainer networkContainer, AtmosphericDef valueType, float value)
        {
            if (valueType.networkValue == null)
            {
                TLog.Warning($"AtmosphericDef: {valueType} does not have any NetworkValueDef defined to transfer into {networkContainer?.Holder?.NetworkPart?.Network}");
                return false;
            }

            if (TryRemoveValue(valueType, value, out var actual))
            {
                networkContainer.TryAddValue(valueType.networkValue, actual.ActualAmount, out _);
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
