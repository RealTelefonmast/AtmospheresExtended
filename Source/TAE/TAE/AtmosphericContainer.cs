using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE
{
    public class AtmosphericContainer
    {
        //
        private RoomComponent_Atmospheric parentComp;
        private float _capacity;

        //
        private float totalStoredCache;
        private HashSet<AtmosphericDef> storedTypeCache;

        //
        private Dictionary<AtmosphericDef, float> storedValues;

        public DefValueStack<AtmosphericDef> ValueStack { get; private set; }
        public float Capacity => _capacity;
        public float TotalStored => totalStoredCache;

        public RoomComponent_Atmospheric Parent => parentComp;
        public bool HasParentRoom => parentComp != null;

        //Dynamic State Getters
        public float CapacityOf(AtmosphericDef def)
        {
            return _capacity * def.maxSaturation;
        }

        public float TotalStoredOf(AtmosphericDef def)
        {
            return storedValues.GetValueOrDefault(def, 0);
        }

        public float StoredPercentOf(AtmosphericDef def)
        {
            return TotalStoredOf(def) / (_capacity * def.maxSaturation);
        }

        public float StoredPercentRelative(AtmosphericDef def)
        {
            return TotalStoredOf(def) / totalStoredCache;
        }

        public bool FullFor(AtmosphericDef def)
        {
            return TotalStoredOf(def) > _capacity;
        }

        //
        public AtmosphericDef MainDef => storedValues.MaxBy(x => x.Value).Key;
        public Dictionary<AtmosphericDef, float> StoredValuesByType => storedValues;
        public HashSet<AtmosphericDef> AllStoredTypes
        {
            get { return storedTypeCache ??= new HashSet<AtmosphericDef>(); }
        }

        public AtmosphericContainer(RoomComponent_Atmospheric parent)
        {
            this.parentComp = parent;
            storedValues = new Dictionary<AtmosphericDef, float>();
            storedTypeCache = new HashSet<AtmosphericDef>();
        }

        public void Data_Clear()
        {
            for (int i = storedValues.Count - 1; i >= 0; i--)
            {
                var keyValuePair = storedValues.ElementAt(i);
                TryRemoveValue(keyValuePair.Key, keyValuePair.Value, out _);
            }

            //
            Notify_ContainerStateChanged();
        }

        public void Data_LoadFromStack(DefValueStack<AtmosphericDef> values)
        {
            Data_Clear();
            foreach (var value in values.values)
            {
                TryAddValue(value.Def, value.Value, out _);
            }
        }

        public void Notify_RoomChanged(RoomComponent_Atmospheric parent, int roomCells)
        {
            parentComp = parent;
            _capacity = roomCells * AtmosphericMapInfo.CELL_CAPACITY;
        }

        internal void Notify_ContainerStateChanged(bool updateMetaData = false)
        {
            //Set Stack
            ValueStack = new DefValueStack<AtmosphericDef>(storedValues);
            
            //Update metadata
            if (updateMetaData)
            {
                totalStoredCache = ValueStack.TotalValue;
                AllStoredTypes.AddRange(ValueStack.AllTypes);
            }
            //colorInt = Color.clear;

            /*
            if (storedValues.Count > 0)
            {
                foreach (var value in storedValues)
                {
                    //colorInt += value.Key.valueColor * (value.Value / Capacity);
                }
            }
            */
        }

        public bool CanFullyTransferTo(AtmosphericContainer other, AtmosphericDef valueType, float value)
        {
            return other.TotalStoredOf(valueType) + value <= other.CapacityOf(valueType);
        }

        public bool TryTransferTo(AtmosphericContainer other, AtmosphericDef valueType, float value)
        {
            //Attempt to transfer a weight to another container
            //Check if anything of that type is stored, check if transfer of weight is possible without loss, try remove the weight from this container
            //if (!other.AcceptsType(valueType)) return false;
            if (storedValues.TryGetValue(valueType) >= value && CanFullyTransferTo(other, valueType, value) && TryRemoveValue(valueType, value, out float actualValue))
            {
                //If passed, try to add the actual weight removed from this container, to the other.
                other.TryAddValue(valueType, actualValue, out float actualAddedValue);
                return true;
            }
            return false;
        }

        public bool TryTransferTo(NetworkContainer networkContainer, AtmosphericDef valueType, float value)
        {
            //TODO....
            return false;
        }

        public void TransferAllTo(AtmosphericContainer other)
        {
            foreach (var type in AllStoredTypes)
            {
                TryTransferTo(other, type, TotalStoredOf(type));
            }
        }

        //
        public void Notify_Full()
        {
            //Parent?.Notify_ContainerFull();
        }

        public void Notify_AddedValue(AtmosphericDef def, float value)
        {
            totalStoredCache += value;
            //ParentStructure?.ContainerSet?.Notify_AddedValue(def, value, ParentStructure.NetworkPart);
            AllStoredTypes.Add(def);

            //Update stack state
            Notify_ContainerStateChanged();
        }

        public void Notify_RemovedValue(AtmosphericDef def, float value)
        {
            totalStoredCache -= value;
            //ParentStructure?.ContainerSet?.Notify_RemovedValue(valueType, value, ParentStructure.NetworkPart);
            if (AllStoredTypes.Contains(def) && TotalStoredOf(def) <= 0)
                AllStoredTypes.RemoveWhere(v => v == def);

            //Update stack state
            Notify_ContainerStateChanged();
        }

        //Value Manipulation
        public bool TryAddValue(AtmosphericDef def, float wantedValue, out float actualValue)
        {
            //If we add more than we can contain, we have an excess weight
            var excessValue = Mathf.Clamp((TotalStoredOf(def) + wantedValue) - CapacityOf(def), 0, float.MaxValue);
            //The actual added weight is the wanted weight minus the excess
            actualValue = wantedValue - excessValue;

            //If the container is full, or doesnt accept the type, we dont add anything
            if (FullFor(def))
            {
                Notify_Full();
                return false;
            }

            //if (!AcceptsType(valueType))
            //return false;

            //If the weight type is already stored, add to it, if not, make a new entry
            if (storedValues.ContainsKey(def))
                storedValues[def] += actualValue;
            else
                storedValues.Add(def, actualValue);

            Notify_AddedValue(def, actualValue);

            //If this adds the last drop, notify full
            if (FullFor(def))
                Notify_Full();

            return true;
        }

        public bool TryRemoveValue(AtmosphericDef def, float wantedValue, out float actualValue)
        {
            //Attempt to remove a certain weight from the container
            actualValue = wantedValue;
            var value = TotalStoredOf(def);
            if (value > 0)
            {
                if (value >= wantedValue)
                    //If we have stored more than we need to pay, remove the wanted weight
                    storedValues[def] -= wantedValue;
                else if (value > 0)
                {
                    //If not enough stored to "pay" the wanted weight, remove the existing weight and set actual removed weight to removed weight 
                    storedValues[def] = 0;
                    actualValue = value;
                }
            }

            if (storedValues[def] <= 0)
            {
                storedValues.Remove(def);
            }

            Notify_RemovedValue(def, actualValue);
            return actualValue > 0;
        }

        //

        public string RoomReadout()
        {
            return $"{0}ppc"; //parts per cell
        }
    }
}
