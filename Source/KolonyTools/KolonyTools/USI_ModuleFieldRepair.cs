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

        [KSPEvent(active = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Perform maintenance",
            unfocusedRange = 2f)]
        public void PerformMaintenance()
        {
            var kerbal = vessel.rootPart.protoModuleCrew[0];
            if (kerbal.experienceTrait.Title != "Engineer")
            {
                ScreenMessages.PostScreenMessage("Only Engineers can perform EVA Maintenance!", 5f,
                    ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            ScreenMessages.PostScreenMessage("You perform routine maintenance...", 5f, ScreenMessageStyle.UPPER_CENTER);
            GrabResources("SpareParts");
            GrabResources("EnrichedUranium");
            GrabResources("Machinery");
            PushResources("DepletedUranium");
        }

        private void PushResources(string resourceName)
        {
            var brokRes = part.Resources[resourceName];
            //Push to warehouses

            //Put remaining parts in warehouses
            foreach (var p in vessel.parts.Where(vp => vp != part && vp.Modules.Contains("USI_ModuleCleaningBin")))
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
            var brokRes = part.Resources[resourceName];
            var needed = brokRes.maxAmount - brokRes.amount;
            //Pull in from warehouses

            var whpList = vessel.parts.Where(p => p.Modules.Contains("USI_ModuleResourceWarehouse"));
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
