using System;
using System.Collections.Generic;
using System.Linq;
using KolonyTools;
using USITools;
using USITools.Logistics;

namespace PlanetaryLogistics
{
    public class ModulePlanetaryLogistics : PartModule
    {
        [KSPField]
        public double CheckFrequency = 12d;
        
        [KSPField]
        public double LowerTrigger = .25d;

        [KSPField]
        public double UpperTrigger = .75d;

        [KSPField]
        public double FillGoal = .5d;

        [KSPField] 
        public double ResourceTax = 0.05d;

        private double lastCheck;
        private List<USI_ModuleResourceWarehouse> _warehouseList;

        public void FixedUpdate()
        {
            try
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return;

                if (!vessel.LandedOrSplashed)
                    return;

                //PlanLog grabs all things attached to this vessel.
                if(_warehouseList == null)
                    _warehouseList = vessel.FindPartModulesImplementing<USI_ModuleResourceWarehouse>();

                foreach (var mod in _warehouseList)
                {
                    bool hasSkill = LogisticsTools.NearbyCrew(vessel, 500, "LogisticsSkill");

                    if (!mod.transferEnabled)
                        continue;

                    var rCount = mod.part.Resources.Count;
                    for (int i = 0; i < rCount; ++i)
                    {
                        var res = mod.part.Resources[i];
                        LevelResources(mod.part, res.resourceName,hasSkill);
                    }
                }
            }
            catch (Exception ex)
            {
                print("ERROR IN ModulePlanetaryLogistics -> FixedUpdate");
            }
        }


        private void LevelResources(Part rPart, string resource, bool hasSkill)
        {
            var res = rPart.Resources[resource];
            var body = vessel.mainBody.flightGlobalsIndex;

            if (!res.flowState)
            {
                if (res.amount <= ResourceUtilities.FLOAT_TOLERANCE)
                    return;

                if (!PlanetaryLogisticsManager.Instance.DoesLogEntryExist(resource, body))
                    return;
                
                var logEntry = PlanetaryLogisticsManager.Instance.FetchLogEntry(resource, body);
                logEntry.StoredQuantity += (res.amount * (1d - ResourceTax));
                res.amount = 0;

                PlanetaryLogisticsManager.Instance.TrackLogEntry(logEntry);
                
                return;
            }

            var fillPercent = res.amount / res.maxAmount;
            if (fillPercent < LowerTrigger)
            {
                if (!hasSkill)
                    return;

                var amtNeeded = (res.maxAmount * FillGoal) - res.amount;
                if (!(amtNeeded > 0)) 
                    return;

                if (!PlanetaryLogisticsManager.Instance.DoesLogEntryExist(resource, body)) 
                    return;
                
                var logEntry = PlanetaryLogisticsManager.Instance.FetchLogEntry(resource, body);
                if (logEntry.StoredQuantity > amtNeeded)
                {
                    logEntry.StoredQuantity -= amtNeeded;
                    res.amount += amtNeeded;
                }
                else
                {
                    res.amount += logEntry.StoredQuantity;
                    logEntry.StoredQuantity = 0;
                }
                PlanetaryLogisticsManager.Instance.TrackLogEntry(logEntry);
            }

            else if (fillPercent > UpperTrigger)
            {
                var strAmt = res.amount - (res.maxAmount * FillGoal);
                if (!(strAmt > 0)) 
                    return;
                
                var logEntry = PlanetaryLogisticsManager.Instance.FetchLogEntry(resource, body);
                logEntry.StoredQuantity += (strAmt * (1d-ResourceTax));
                res.amount -= strAmt;
                PlanetaryLogisticsManager.Instance.TrackLogEntry(logEntry);
            }
        }
    }
}