using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

        private List<USI_ModuleResourceWarehouse> _warehouseList;
        private double lastWHCheck = 0; // TOFIX ? actually never updated at the moment

        public void FixedUpdate()
        {
            try
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return;

                if (!vessel.LandedOrSplashed)
                    return;

                bool hasSkill = LogisticsTools.NearbyCrew(vessel, 500, "LogisticsSkill");
                
                //Periodic refresh of the warehouses due to vessel change, etc.
                if (Planetarium.GetUniversalTime() > lastWHCheck + CheckFrequency)
                    _warehouseList = null;

                //PlanLog grabs all things attached to this vessel.
                if (_warehouseList == null)
                    _warehouseList = vessel.FindPartModulesImplementing<USI_ModuleResourceWarehouse>();

                if (_warehouseList != null)
                {
                    foreach (var mod in _warehouseList)
                    {
                        if (!mod.soiTransferEnabled)
                            continue;

                        var rCount = mod.part.Resources.Count;
                        for (int i = 0; i < rCount; ++i)
                        {
                            var res = mod.part.Resources[i];
                            if (_blackList.Contains(res.resourceName))
                                continue;
                            LevelResources(mod.part, res.resourceName, hasSkill);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                print("ERROR IN ModulePlanetaryLogistics -> FixedUpdate: " + ex.StackTrace);
            }
        }

        private List<String> _blackList = new List<string> { "EnrichedUranium", "DepletedFuel", "Construction", "ReplacementParts", "ElectricCharge" };


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