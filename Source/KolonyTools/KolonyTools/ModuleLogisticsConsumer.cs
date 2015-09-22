using System;
using System.Collections.Generic;
using System.Linq;

namespace KolonyTools
{
    public class ModuleLogisticsConsumer : PartModule
    {
        private double lastCheck;
        private double checkTime = 5f;

        public override void OnFixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (Math.Abs(lastCheck - Planetarium.GetUniversalTime()) < checkTime)
                return;

            lastCheck = Planetarium.GetUniversalTime();

            foreach (var con in part.FindModulesImplementing<ModuleResourceConverter>())
            {
                if (con.inputList != null)
                    CheckLogistics(con.inputList);
            }

            //Special for USI-LS/TAC-LS
            if (vessel.GetCrewCount() > 0)
            {
                CheckLogistics(new List<ResourceRatio>
                {
                    new ResourceRatio {ResourceName = "Supplies"},
                    new ResourceRatio {ResourceName = "Food"},
                    new ResourceRatio {ResourceName = "Water"},
                    new ResourceRatio {ResourceName = "Oxygen"},
                });
            }

            // Special for ExtraPlanetary LaunchPads
            if (part.Modules.Contains("ExWorkshop"))
            {
                CheckLogistics(new List<ResourceRatio>
                {
                    new ResourceRatio {ResourceName = "RocketParts"}
                });
            }

            //Always check for power!
            CheckLogistics(new List<ResourceRatio>
            {
                new ResourceRatio {ResourceName = "ElectricCharge"}
            });


        }

        private void CheckLogistics(List<ResourceRatio> resList)
        {
            //Surface only
            if (!vessel.LandedOrSplashed)
                return;

            var hasDepot = LogisticsAvailable();
            var hasPDU = PowerAvailable();

            //The konverter will scan for missing resources and
            //attempt to pull them in from nearby ships.

            //Find what we need!
            foreach (var res in resList)
            {
                //There are certain exeptions - specifically, anything for field repair.
                if (res.ResourceName == "Machinery")
                    continue;

                if (res.ResourceName != "ElectricCharge")
                {
                    //A logistics module (ILM, etc.) must be nearby
                    if (!hasDepot)
                        continue;
                }
                else
                {
                    //A PDU must be nearby
                    if (!hasPDU)
                        continue;
                }
                //How many do we have in our ship
                var pRes = PartResourceLibrary.Instance.GetDefinition(res.ResourceName);
                var maxAmount = 0d;
                var curAmount = 0d;
                foreach (var p in vessel.parts.Where(pr => pr.Resources.Contains(res.ResourceName)))
                {
                    var rr = p.Resources[res.ResourceName];
                    maxAmount += rr.maxAmount;
                    curAmount += rr.amount;
                }
                double fillPercent = curAmount / maxAmount; //We use this to equalize things cross-ship as a percentage.
                if (fillPercent < 0.5d) //We will not attempt a fillup until we're at less than half capacity
                {
                    //Keep changes small - 10% per tick.  So we should hover between 50% and 60%
                    var deficit = maxAmount * .1d;
                    double receipt = FetchResources(deficit, pRes, fillPercent, maxAmount);
                    //Put these in our vessel
                    StoreResources(receipt, pRes);
                }
            }
        }

        private const int LOG_RANGE = 750;
        private const int DEPOT_RANGE = 150;

        public bool LogisticsAvailable()
        {
            var vList = LogisticsTools.GetNearbyVessels(DEPOT_RANGE, true, vessel, true);
            foreach (var v in vList.Where(v => v.GetTotalMass() <= 3f))
            {
                if (v.Parts.Any(p => p.FindModuleImplementing<ModuleResourceDistributor>() != null && HasCrew(p, "Pilot")))
                    return true;
            }
            return false;
        }

        public bool PowerAvailable()
        {
            return GetPowerDistributors(vessel).Count > 0;
        }


        public List<Vessel> GetPowerDistributors(Vessel thisVessel)
        {
            bool hasRelay = false;
            var pList = new List<Vessel>();
            var vList = LogisticsTools.GetNearbyVessels(20000, true, thisVessel, true);

            foreach (var v in vList)
            {
                var gParts = v.parts.Where(p => p.FindModuleImplementing<ModulePowerDistributor>() != null && HasCrew(p, "Engineer"));
                if (gParts != null)
                {
                    foreach (var p in gParts)
                    {
                        var mod = p.FindModuleImplementing<ModulePowerDistributor>();
                        var posCur = vessel.GetWorldPos3D();
                        var posNext = v.GetWorldPos3D();
                        var distance = Vector3d.Distance(posCur, posNext);
                        if (distance < mod.PowerDistributionRange)
                        {
                            pList.Add(v);
                        }
                    }
                }

                if (!hasRelay)
                {
                    var dParts = v.parts.Where(p => p.FindModuleImplementing<ModulePowerCoupler>() != null);
                    if (dParts != null)
                    {
                        foreach (var p in dParts)
                        {
                            var mod = p.FindModuleImplementing<ModulePowerCoupler>();
                            var posCur = vessel.GetWorldPos3D();
                            var posNext = v.GetWorldPos3D();
                            var distance = Vector3d.Distance(posCur, posNext);
                            if (distance < mod.PowerCouplingRange)
                            {
                                hasRelay = true;
                            }
                        }
                    }
                }
            }

            if (hasRelay)
            {
                return pList;
            }

            else
            {
                return new List<Vessel>();
            }
        }



        private bool HasCrew(Part p, string skill)
        {
            if (p.CrewCapacity > 0)
            {
                return (p.protoModuleCrew.Any(c => c.experienceTrait.TypeName == skill));
            }
            else
            {
                return (p.vessel.GetVesselCrew().Any(c => c.experienceTrait.TypeName == skill));
            }
        }

        private void StoreResources(double amount, PartResourceDefinition resource)
        {
            try
            {
                var transferAmount = amount;
                var partList = vessel.Parts.Where(
                    p => p.Resources.Contains(resource.name));
                foreach (var p in partList)
                {
                    var pr = p.Resources[resource.name];
                    var storageSpace = pr.maxAmount - pr.amount;
                    if (storageSpace >= transferAmount)
                    {
                        pr.amount += transferAmount;
                        break;
                    }
                    else
                    {
                        transferAmount -= storageSpace;
                        pr.amount = pr.maxAmount;
                    }
                }
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in StoreResources - {0}", ex.StackTrace));
            }
        }

        private double FetchResources(double amount, PartResourceDefinition resource, double fillPercent, double targetMaxAmount)
        {
            double demand = amount;
            double fetched = 0d;
            try
            {
                var rangeFactor = LOG_RANGE;
                var nearVessels = LogisticsTools.GetNearbyVessels(rangeFactor, false, vessel, true);

                if (resource.name == "ElectricCharge")
                {
                    nearVessels = GetPowerDistributors(vessel);
                }

                foreach (var v in nearVessels)
                {
                    if (demand <= ResourceUtilities.FLOAT_TOLERANCE) break;
                    //Is this a valid target?
                    var maxToSpare = GetAmountOfResourcesToSpare(v, resource, fillPercent + fetched / targetMaxAmount, targetMaxAmount);
                    if (maxToSpare < ResourceUtilities.FLOAT_TOLERANCE)
                        continue;
                    //Can we find what we're looking for?
                    var partList = v.Parts.Where(p => p.Resources.Contains(resource.name));
                    foreach (var p in partList)
                    {
                        //Special case - EC can only come from a PDU
                        if (resource.name == "ElectricCharge" && !p.Modules.Contains("ModulePowerDistributor"))
                            continue;
                        var pr = p.Resources[resource.name];


                        if (pr.amount >= demand)
                        {
                            if (maxToSpare >= demand)
                            {
                                pr.amount -= demand;
                                fetched += demand;
                                demand = 0;
                                break;
                            }
                            else
                            {
                                pr.amount -= maxToSpare;
                                fetched += maxToSpare;
                                demand -= maxToSpare;
                                break;
                            }
                        }
                        else
                        {
                            if (maxToSpare >= pr.amount)
                            {
                                demand -= pr.amount;
                                fetched += pr.amount;
                                maxToSpare -= pr.amount;
                                pr.amount = 0;
                            }
                            else
                            {
                                demand -= maxToSpare;
                                fetched += maxToSpare;
                                pr.amount -= maxToSpare;
                                break;
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in FetchResources - {0}", ex.StackTrace));
            }
            return fetched;
        }

        private double GetAmountOfResourcesToSpare(Vessel v, PartResourceDefinition resource, double targetPercent, double targetMaxAmount)
        {
            var maxAmount = 0d;
            var curAmount = 0d;
            foreach (var p in v.parts.Where(pr => pr.Resources.Contains(resource.name)))
            {
                var rr = p.Resources[resource.name];
                maxAmount += rr.maxAmount;
                curAmount += rr.amount;
            }
            double fillPercent = maxAmount < ResourceUtilities.FLOAT_TOLERANCE ? 0 : curAmount / maxAmount;
            if (fillPercent > targetPercent)
            {
                //If we're in better shape, they can take some of our stuff.
                var targetCurrentAmount = targetMaxAmount * targetPercent;
                targetPercent = (curAmount + targetCurrentAmount) / (targetMaxAmount + maxAmount);
                return curAmount - maxAmount * targetPercent;
            }
            else
            {
                return 0;
            }
        }

    }
}