using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KolonyTools
{
    public class USI_ModuleResourceDistributor : PartModule
    {
        [KSPField] 
        public string BrokeredResource;



        public override void OnFixedUpdate()
        {
            if (part.Resources.Contains(BrokeredResource) && part.protoModuleCrew.Count == part.CrewCapacity)
            {
                var brokRes = part.Resources[BrokeredResource];
                var needed = brokRes.maxAmount - brokRes.amount;
                //Pull in from warehouses

                var whpList = LogisticsTools.GetRegionalWarehouses(vessel,"USI_ModuleResourceWarehouse");
                foreach (var whp in whpList)
                {
                    if (whp.Resources.Contains(BrokeredResource))
                    {
                        var res = whp.Resources[BrokeredResource];
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
                
                //Push to all parts needing this resource
                foreach (var p in vessel.parts.Where(vp => vp != part && !vp.Modules.Contains("USI_ModuleResourceWarehouse")))
                {
                    if (p.Resources.Contains(BrokeredResource))
                    {
                        var partRes = p.Resources[BrokeredResource];
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
                //Put remaining parts in warehouses
                foreach (var p in vessel.parts.Where(vp => vp != part && vp.Modules.Contains("USI_ModuleResourceWarehouse")))
                {
                    if (p.Resources.Contains(BrokeredResource))
                    {
                        var partRes = p.Resources[BrokeredResource];
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
        }

        public override void OnStart(StartState state)
        {
            part.force_activate();
            base.OnStart(state);
        }

        
    }
}
