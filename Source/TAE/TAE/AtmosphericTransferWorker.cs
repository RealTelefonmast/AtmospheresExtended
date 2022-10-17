using System;
using System.Collections.Generic;
using RimWorld;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE
{
    public struct FlowResult
    {
        private bool hadFlow = false;
        private bool flowToOther = false;
        private bool isVoided = false;
        private AtmosPortalFlow flowDirection = AtmosPortalFlow.None;

        public bool NoFlow => !hadFlow;
        public bool FlowsToOther => flowToOther;
        public bool IsVoided => isVoided;

        public int FromIndex
        {
            get
            {
                return flowDirection switch
                {
                    AtmosPortalFlow.Positive => 0,
                    AtmosPortalFlow.Negative => 1,
                    _ => -1
                };
            }
        }

        public int ToIndex
        {
            get
            {
                return flowDirection switch
                {
                    AtmosPortalFlow.Positive => 1,
                    AtmosPortalFlow.Negative => 0,
                    _ => -1
                };
            }
        }

        public FlowResult() { }

        public void SetFlow(AtmosPortalFlow flowDir)
        {
            hadFlow = flowToOther = true;
            this.flowDirection = flowDir;
        }

        public static FlowResult None => new() {hadFlow = false};
        public static FlowResult ResultVoided => new() {isVoided = true, hadFlow = true };
        public static FlowResult ResultNormalFlow => new() {flowToOther = true, hadFlow = true};

        public override string ToString()
        {
            return $"HadFlow: {hadFlow}; FlowToOther: {flowToOther}; IsVoided: {isVoided}; FlowDir: {flowDirection} [{FromIndex} -> {ToIndex}]";
        }
    }
    
    public class AtmosphericTransferWorker
    {
        private AtmosphericDef def;
        
        public AtmosphericTransferWorker(SpreadingGasTypeDef def)
        {
            this.def = def.dissipateTo;
        }
        
        public AtmosphericTransferWorker(AtmosphericDef def)
        {
            this.def = def;
        }

        public float GetBaseTransferRate(Thing thing)
        {
            return FinalizeTransferRate(thing, DefaultAtmosphericPassPercent(thing));
        }

        protected virtual float FinalizeTransferRate(Thing thing, float baseTransferRate)
        {
            return baseTransferRate;
        }

        protected virtual FlowResult CustomTransferFunc(AtmosphericContainer @from, AtmosphericContainer to, AtmosphericDef valueType, float value)
        {
            return FlowResult.ResultNormalFlow;
        }

        //
        public static float DefaultAtmosphericPassPercent(Thing forThing)
        {
            if (forThing == null) return 1.0f;
            
            //Custom Worker Subroutine
            if (AtmosPortalData.TryGetWorkerFor(forThing.def, out var worker))
            {
                return worker.Worker.PassPercent(forThing);
            }

            //
            var fullFillage = forThing.def.Fillage == FillCategory.Full;
            var fillage = forThing.def.fillPercent;
            var flowPct = fullFillage ? 0.0f : 1.0f - fillage;
            return forThing switch
            {
                Building_Door door => door.Open ? 1 : FlowPctForDoor(door, fillage),
                Building_Vent vent => FlickUtility.WantsToBeOn(vent) ? 1.0f : 0.0f,
                Building_Cooler cooler => cooler.IsPoweredOn() ? 1.5f : 0.0f,
                { } b => flowPct,
                _ => 0.0f
            };
        }

        private static float FlowPctForDoor(Building_Door door, float fillage)
        {
            var categories = door.Stuff?.stuffProps?.categories;
            if (categories.NullOrEmpty()) return 1.0f - fillage;

            //TODO:Add Stat for gas permeability
            foreach (var stuffCategory in categories)
            {
                if (stuffCategory == StuffCategoryDefOf.Fabric)
                {
                    return (float)Math.Round(1 - (fillage * 0.1), 2);
                }

                if (stuffCategory == StuffCategoryDefOf.Leathery)
                {
                    return (float)Math.Round(1 - (fillage * 0.25), 2);
                }

                if (stuffCategory == StuffCategoryDefOf.Woody)
                {
                    return (float)Math.Round(1 - (fillage * 0.75f), 2);
                }

                if (stuffCategory == StuffCategoryDefOf.Stony)
                {
                    return (float)Math.Round(1 - (fillage * 0.95f), 2);
                }

                if (stuffCategory == StuffCategoryDefOf.Metallic)
                {
                    return 0;
                }
            }
            return 1 - fillage;
        }

        public static bool IsPassBuilding(Building building)
        {
            if (building == null) return false;
         
            //Custom PassBuilding Subroutine
            if (AtmosPortalData.IsPassBuilding(building.def))
            {
                return true;
            }

            var fullFillage = building.def.Fillage == FillCategory.Full;
            return building switch
            {
                Building_Door => true,
                Building_Vent => true,
                Building_Cooler => true,
                { } b => !fullFillage,
                _ => false
            };
        }

        public FlowResult TryTransferVia(AtmosphericPortal atmosphericPortal, AtmosphericContainer from, AtmosphericContainer to, AtmosphericDef atmosDef)
        {
            if (!atmosphericPortal.NeedsEqualizing(atmosDef, out var flow, out var diffAbs))
            {
                return FlowResult.None;
            }

            FlowResult flowResult = new FlowResult();
            flowResult.SetFlow(flow);

            var sender = flow == AtmosPortalFlow.Positive ? from : to;
            var receiver = flow == AtmosPortalFlow.Positive ? to : from;

            //Get base transfer part
            var fromComp = atmosphericPortal[flowResult.FromIndex];
            var value = (sender.TotalStoredOf(atmosDef) / fromComp.ConnectorCount) * 0.5f;
            value = Mathf.Clamp(value, 0, 100);

            //
            var flowAmount = value * diffAbs * GetBaseTransferRate(atmosphericPortal.Thing) * atmosDef.FlowRate;
            flowAmount = Mathf.Ceil(flowAmount);

            //
            if (sender.CanFullyTransferTo(receiver, atmosDef, flowAmount))
            {
                if (sender.TryRemoveValue(atmosDef, flowAmount, out float actualVal))
                {
                    FlowResult result = CustomTransferFunc(sender, receiver, atmosDef, actualVal);
                    if (result.FlowsToOther)
                    {
                        receiver.TryAddValue(atmosDef, actualVal, out _);
                    }
                    result.SetFlow(flow);
                    return result;
                }
            }
            return FlowResult.None;
        }

        public void ProcessFlow(Rot4 flowDir, IntVec3 flowOut, RoomComponent_Atmospheric flowInto)
        {
            FleckMaker.ThrowExplosionCell(flowOut, flowInto.Map, FleckDefOf.ExplosionFlash, Color.cyan);
        }
    }
}
