using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KolonyTools
{
    public class USI_ModuleFieldRepair : PartModule
    {
        //Very simple module.  Just lets you transfer spare parts in via an EVA.
        //super hacky.  Don't judge me.

        [KSPField]
        public float EVARange = 5f;

        [KSPEvent(active = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Perform maintenance",
            unfocusedRange = 5f)]
        public void PerformMaintenance()
        {
            var kerbal = FlightGlobals.ActiveVessel.rootPart.protoModuleCrew[0];
            if (kerbal.experienceTrait.Title != "Engineer")
            {
                ScreenMessages.PostScreenMessage("Only Engineers can perform EVA Maintenance!", 5f,
                    ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            ScreenMessages.PostScreenMessage("You perform routine maintenance...", 5f, ScreenMessageStyle.UPPER_CENTER);
            GrabResources("Machinery");
            GrabResources("EnrichedUranium");
            PushResources("Recyclables");
            PushResources("DepletedFuel");
        }

        public override void OnStart(StartState state)
        {
            Events["PerformMaintenance"].unfocusedRange = EVARange;
            base.OnStart(state);
        }


        private void PushResources(string resourceName)
        {
            var brokRes = part.Resources[resourceName];
            //Put remaining parts in warehouses
            foreach (var p in LogisticsTools.GetRegionalWarehouses(vessel, "USI_ModuleCleaningBin"))
            {
                if (p.Resources.Contains(resourceName))
                {
                    var partRes = p.Resources[resourceName];
                    var partNeed = partRes.maxAmount - partRes.amount;
                    if (partNeed > 0 && brokRes.amount > 0)
                    {
                        if (partNeed > brokRes.amount)
                        {
                            partNeed = brokRes.amount;
                        }
                        partRes.amount += partNeed;
                        brokRes.amount -= partNeed;
                    }
                }
            }
        }

        private void GrabResources(string resourceName)
        {
            if (!part.Resources.Contains(resourceName))
                return;

            var brokRes = part.Resources[resourceName];
            var needed = brokRes.maxAmount - brokRes.amount;
            //Pull in from warehouses

            var whpList = LogisticsTools.GetRegionalWarehouses(vessel, "USI_ModuleResourceWarehouse");
            foreach (var whp in whpList)
            {
                if (whp.Resources.Contains(resourceName))
                {
                    var res = whp.Resources[resourceName];
                    if (res.amount >= needed)
                    {
                        brokRes.amount += needed;
                        res.amount -= needed;
                        needed = 0;
                        break;
                    }
                    else
                    {
                        brokRes.amount += res.amount;
                        needed -= res.amount;
                        res.amount = 0;
                    }
                }
            }
        }
    }
}
