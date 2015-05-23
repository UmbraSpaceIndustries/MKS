//using System;
//using System.Linq;
//using System.Runtime.InteropServices;

//namespace KolonyTools
//{
//    public class ModulePowerDistributor : PartModule
//    {

//    }

//    public class ModuleResourceDistributor : PartModule
//    {

//    }

//public class Konverter : ModuleResourceConverter
//    {
//        public const int LOG_RANGE = 750;
//        public const int DEPOT_RANGE = 150;
//        public const int POWER_RANGE = 2000;

//        public bool LogisticsAvailable()
//        {
//            var vList = LogisticsTools.GetNearbyVessels(DEPOT_RANGE, true, vessel, true);
//            foreach (var v in vList)
//            {
//                if(v.Parts.Any(p => p.FindModuleImplementing<ModuleResourceDistributor>() != null && HasCrew(p,"Pilot")))
//                    return true;
//            }
//            return false;
//        }

//        public bool PowerAvailable()
//        {
//            var vList = LogisticsTools.GetNearbyVessels(POWER_RANGE, true, vessel, true);
//            foreach (var v in vList)
//            {
//                if (v.Parts.Any(p => p.FindModuleImplementing<ModulePowerDistributor>() != null && HasCrew(p, "Engineer")))
//                    return true;
//            }
//            return false;
//        }

//        private bool HasCrew(Part p, string skill)
//        {
//            return (p.protoModuleCrew.Any(c => c.experienceTrait.TypeName == skill));
//        }



//        //public void FixedUpdate()
//        //{
//        //    return;

//        //    if (!HighLogic.LoadedSceneIsFlight)
//        //        return;

//        //    if (this.inputList == null)
//        //        return;

//        //    //Surface only
//        //    if (!vessel.LandedOrSplashed)
//        //        return;

//        //    var hasDepot = false; //LogisticsAvailable();
//        //    var hasPDU = false; //PowerAvailable();

//        //    //The konverter will scan for missing resources and
//        //    //attempt to pull them in from nearby ships.
            
//        //    //Find what we need!
//        //    foreach (var res in this.inputList)
//        //    {
//        //        if (res.ResourceName != "ElectricCharge")
//        //        {
//        //            //A logistics module (ILM, etc.) must be nearby
//        //            if (!hasDepot)
//        //                continue;
//        //        }
//        //        else
//        //        {
//        //            //A PDU must be nearby
//        //            if (!hasPDU)
//        //                continue;
//        //        }
                
//        //        //How many do we have in our ship
//        //        var pRes = PartResourceLibrary.Instance.GetDefinition(res.ResourceName);
//        //        var maxAmount = 0d;
//        //        var curAmount = 0d;
//        //        foreach (var p in vessel.parts.Where(pr=>pr.Resources.Contains(res.ResourceName)))
//        //        {
//        //            var rr = p.Resources[res.ResourceName];
//        //            maxAmount += rr.maxAmount;
//        //            curAmount += rr.amount;
//        //        }
//        //        var deficit = Math.Floor(maxAmount - curAmount);
//        //        if (deficit > 1d)
//        //        {
//        //            double receipt = FetchResources(deficit, pRes);
//        //            //Put these in our vessel
//        //            StoreResources(receipt, pRes);
//        //        }
//        //    }
//        //}

//        private void StoreResources(double amount, PartResourceDefinition resource)
//        {
//            try
//            {
//                var transferAmount = amount;
//                var partList = vessel.Parts.Where(
//                    p => p.Resources.Contains(resource.name));
//                foreach (var p in partList)
//                {
//                    var pr = p.Resources[resource.name];
//                    var storageSpace = pr.maxAmount - pr.amount;
//                    if (storageSpace >= transferAmount)
//                    {
//                        pr.amount += transferAmount;
//                        break;
//                    }
//                    else
//                    {
//                        transferAmount -= storageSpace;
//                        pr.amount = pr.maxAmount;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                print(String.Format("[MKS] - ERROR in StoreResources - {0}", ex.StackTrace));
//            }
//        }


//        private double FetchResources(double amount, PartResourceDefinition resource)
//        {
//            double demand = amount;
//            double fetched = 0d;
//            try
//            {
//                var rangeFactor = LOG_RANGE;
//                if (resource.name == "ElectricCharge")
//                    rangeFactor = POWER_RANGE;

//                var nearVessels = LogisticsTools.GetNearbyVessels(rangeFactor, false, vessel, true);
//                foreach (var v in nearVessels)
//                {
//                    if (demand <= ResourceUtilities.FLOAT_TOLERANCE) break;
//                    //Can we find what we're looking for?
//                    var partList = v.Parts.Where(
//                        p => p.Resources.Contains(resource.name));
//                    foreach (var p in partList)
//                    {
//                        //Special case - EC can only come from a PDU
//                        if (resource.name == "ElectricCharge" && !p.Modules.Contains("ModulePowerDistributor"))
//                            continue;

//                        var pr = p.Resources[resource.name];
//                        if (pr.amount >= demand)
//                        {
//                            pr.amount -= demand;
//                            fetched += demand;
//                            demand = 0;
//                            break;
//                        }
//                        else
//                        {
//                            demand -= pr.amount;
//                            fetched += pr.amount;
//                            pr.amount = 0;
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                print(String.Format("[MKS] - ERROR in FetchResources - {0}", ex.StackTrace));
//            }
//            return fetched;
//        }

//    }
//}