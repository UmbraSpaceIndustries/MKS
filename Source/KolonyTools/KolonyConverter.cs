using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tac;

namespace KolonyTools
{
    public class KolonyConverter : TacGenericConverter
    {
        [KSPField]
        public int LogisticsRange = 100;

        [KSPField]
        public bool LogisticsEnabled = true;

        [KSPField]
        public string RequiredResources = "";
        
        [KSPField]
        public bool SurfaceOnly = true;

        [KSPField]
        public bool AllowsEfficiency = true;
        
        [KSPField(guiActive = true, guiName = "Workspaces")]
        public string efficiencyStats = "Unknown";
        [KSPField(guiActive = true, guiName = "Efficiency")]
        public string efficiency = "Unknown";

        private List<ResourceRatio> inputResourceList;
        private List<ResourceRatio> outputResourceList;
        private static List<string> LSResources = new List<string> { "Food", "Water", "Oxygen" };
        private int _numConverters;
        private float baseConversionRate;
        private float EfficiencyRate; 

        private static char[] delimiters = { ' ', ',', '\t', ';' };

        public override void OnFixedUpdate()
        {
            try
            { 
                baseConversionRate = conversionRate;
                var curConverters = GetActiveKolonyModules(vessel);
                if(curConverters != _numConverters)
                {
                    _numConverters = curConverters;
                    EfficiencySetup();
                }
                if(AllowsEfficiency)
                {
                    conversionRate = conversionRate * EfficiencyRate;
                }

                if (SurfaceOnly && !vessel.Landed)
                {
                    converterStatus = "Cannot operate while in flight";
                    return;
                }

                var missingFixedResources = GetMissingFixedResources();
                if (!String.IsNullOrEmpty(missingFixedResources))
                {
                    converterStatus = "Missing " + missingFixedResources;
                    return;
                }
                if(LogisticsEnabled && converterEnabled) GetMissingResources();
                base.OnFixedUpdate();
                if (LogisticsEnabled && converterEnabled) StoreExcessResources();

                conversionRate = baseConversionRate;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in OnFixedUpdate - {0}",ex.Message));
                throw;
            }
        }

        private void GetMissingResources()
        {
            try
            {
                //Only works when landed
                if (!vessel.Landed) return;
                foreach (var r in inputResourceList)
                {
                    var rName = r.resource.name;
                    var curAmount = part.Resources[rName].amount;
                    var maxAmount = part.Resources[rName].maxAmount;
                    var storageSpace = maxAmount - curAmount;
                    var needAmount = (r.ratio * 2) - curAmount;
                    if (needAmount > storageSpace) needAmount = storageSpace;
                    needAmount = Math.Ceiling(needAmount); //Get a bit extra for good measure
                    if (needAmount > 0)
                    {
                        TransferResources(r.resource, needAmount, TransferType.TakeResources);
                    }
                }
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in GetMissingResources - {0}",ex.Message));
                throw;
            }
        }

        private void StoreExcessResources()
        {
            try
            { 
                //Only works when landed
                if (!vessel.Landed) return;
                foreach (var r in outputResourceList)
                {
                    var rName = r.resource.name;
                    var curAmount = part.Resources[rName].amount;
                    var maxAmount = part.Resources[rName].maxAmount;

                    //490+(20*2)-500 = 30  [store]
                    //450+(20*2)-500 = -10 [nostore]
                    var storeAmount = curAmount + (r.ratio * 2) - maxAmount;
                    storeAmount = Math.Floor(storeAmount) -1; //Leave a bit inside
                    if (storeAmount > 0)
                    {
                        //Start by moving stuff to crewed areas if appropriate.  
                        if (LSResources.Contains(rName))
                        {
                            storeAmount = TransferLifeSupport(rName, storeAmount);
                        }
                        //Any excess is handled normally.
                        TransferResources(r.resource, storeAmount, TransferType.StoreResources);
                    }
                }
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in StoreExcessResources - {0}",ex.Message));
                throw;
            }

        }

        private double TransferLifeSupport(string name, double amount)
        {
            try
            { 
                //Start finding crewed modules.  Keep adding life support until our amount is depleted.
                var nearVessels = GetNearbyVessels();
                var transferAmount = amount;

                foreach (var v in nearVessels)
                {
                    if (transferAmount == 0) break;

                    //Can we find what we're looking for?
                    var partList = v.Parts.Where(
                        p => p.Resources.Contains(name)
                        && p != part
                        && p.protoModuleCrew.Count > 0);
                    foreach (var p in partList)
                    {
                        var pr = p.Resources[name];
                        var storageSpace = pr.maxAmount - pr.amount;

                        if (storageSpace >= transferAmount)
                        {
                            // SS: 100
                            // RemotePartAmount:        400/500 -> 450/500
                            // LocalPartAmount:         100     -> 50
                            // TransferAmount:          50      -> 0
                            pr.amount += transferAmount;
                            part.Resources[name].amount -= transferAmount;
                            transferAmount = 0;
                            break;
                        }
                        else
                        {
                            // SS:10
                            // RemotePartAmount:        490/500 -> 500/500
                            // LocalPartAmount:         100     -> 90
                            // TransferAmount:          50      -> 40
                            transferAmount -= storageSpace;
                            part.Resources[name].amount -= storageSpace;
                            pr.amount = pr.maxAmount;
                        }
                    }
                }
                return transferAmount;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in TransferLifeSupport - {0}",ex.Message));
                throw;
            }
        }

        private enum TransferType
        {
            TakeResources,
            StoreResources
        }

        private List<Vessel> GetNearbyVessels()
        {
            try
            { 
                var vessels = new List<Vessel>();
                foreach (var v in FlightGlobals.Vessels.Where(
                    x => x.mainBody == vessel.mainBody 
                    && x.Landed))
                {
                    var posCur = vessel.GetWorldPos3D();
                    var posNext = v.GetWorldPos3D();
                    var distance = Vector3d.Distance(posCur, posNext);
                    if (distance < LogisticsRange)
                    {
                        vessels.Add(v);
                    }
                }
                return vessels;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in GetNearbyVessels - {0}", ex.Message));
                throw;
            }
        }

        private void TransferResources(PartResourceDefinition resource, double amount, TransferType transferType)
        {
            try
            {
                var transferAmount = amount;
                var nearVessels = GetNearbyVessels();

                foreach (var v in nearVessels)
                {
                    if (transferAmount == 0) break;

                    //Can we find what we're looking for?
                    var partList = v.Parts.Where(
                        p => p.Resources.Contains(resource.name) 
                        && p!= part);
                    foreach(var p in partList)
                    {
                        var pr = p.Resources[resource.name];
                        if (transferType == TransferType.TakeResources)
                        {
                            // RemotePartAmount:       200    -> 150
                            // LocalPartAmount:         10    -> 60
                            // TransferAmount:          50    -> 0
                            //  
                            if (pr.amount >= transferAmount)
                            {
                                pr.amount -= transferAmount;
                                part.Resources[resource.name].amount += transferAmount;
                                transferAmount = 0;
                                break;
                            }
                            else
                            {
                                // RemotePartAmount:        10    -> 0
                                // LocalPartAmount:         10    -> 20
                                // TransferAmount:          50    -> 40
                                // 
                                transferAmount -= pr.amount;
                                part.Resources[resource.name].amount += pr.amount;
                                pr.amount = 0;
                            }
                        }
                        else
                        {
                            var storageSpace = pr.maxAmount - pr.amount;

                            if (storageSpace >= transferAmount)
                            {
                                // SS: 100
                                // RemotePartAmount:        400/500 -> 450/500
                                // LocalPartAmount:         100     -> 50
                                // TransferAmount:          50      -> 0
                                pr.amount += transferAmount;
                                part.Resources[resource.name].amount -= transferAmount;
                                transferAmount = 0;
                                break;
                            }
                            else
                            {
                                // SS:10
                                // RemotePartAmount:        490/500 -> 500/500
                                // LocalPartAmount:         100     -> 90
                                // TransferAmount:          50      -> 40
                                transferAmount -= storageSpace;
                                part.Resources[resource.name].amount -= storageSpace;
                                pr.amount = pr.maxAmount;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in TransferResources - {0}", ex.Message));
                throw;
            }
        }

        public override void OnAwake()
        {
            ResourceSetup();
            base.OnAwake();
        }

        public override void OnLoad(ConfigNode node)
        {
            ResourceSetup(); 
            base.OnLoad(node);
        }

        private void ResourceSetup()
        {
            inputResourceList = UpdateResourceList(inputResources);
            outputResourceList = UpdateResourceList(outputResources);
        }

        private void EfficiencySetup()
        {
            EfficiencyRate = GetEfficiency();
        }

        private float GetEfficiency()
        {
            try
            { 
                //Efficiency is a function of:
                //  - Crew Capacity (any module will work for this)
                //  - Crew Count
                //  - Active MKS Module count

                var numWorkspaces = vessel.GetCrewCapacity();
                var numModules = GetActiveKolonyModules(vessel);

                //  Part (x1.5):   0   2   1   
                //  Ship (x0.5):   2   0   1
                //  Total:         1   3   2

                float modKerbalFactor = 0;
                foreach(var k in part.protoModuleCrew)
                {
                    //A range from 1 to 2, average should be 1.5
                    modKerbalFactor += 2;
                    modKerbalFactor -= k.stupidity;
                }
                var numModuleKerbals = part.protoModuleCrew.Count();
                var numShipKerbals = vessel.GetCrewCount() - numModuleKerbals;
            
                float numKerbals = (numShipKerbals * 0.5f) + modKerbalFactor;
            
                //Worst case, 25%
                float eff = .1f;

                if (numKerbals > 0)
                {
                    float WorkSpaceKerbalRatio = numWorkspaces / numKerbals;
                    if (WorkSpaceKerbalRatio > 2) WorkSpaceKerbalRatio = 2;

                    float WorkUnits = WorkSpaceKerbalRatio * numKerbals;
                    eff = WorkUnits / numModules;
                    if (eff > 2) eff = 2;
                    if (eff < .25) eff = .1f;
                }
                efficiency = String.Format("{0}%", Math.Round((eff * 100),1));
                efficiencyStats = String.Format("ker:{0} spc:{1} mod:{2}", Math.Round(numKerbals,1), numWorkspaces, numModules);
                return eff;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in GetEfficiency - {0}", ex.Message));
                throw;
            }
        }

        private int GetActiveKolonyModules(Vessel v)
        {
            try 
            { 
                var numMods = 0;
                var pList = v.parts.Where(p => p.Modules.Contains("KolonyConverter"));
                foreach(var p in pList)
                {
                    var mods = p.Modules.OfType<KolonyConverter>();
                    foreach(var pm in mods)
                    {
                        if (pm.converterEnabled) 
                            numMods++;
                    }
                }
                return numMods;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in GetActiveKolonyModules - {0}", ex.Message));
                throw;
            }
        }

        private List<ResourceRatio> UpdateResourceList(string resString)
        {
            try
            {
                var resources = new List<ResourceRatio>();
                string[] tokens = resString.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < (tokens.Length - 1); i += 2)
                {
                    PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(tokens[i]);
                    double ratio;
                    if (resource != null && double.TryParse(tokens[i + 1], out ratio))
                    {
                        resources.Add(new ResourceRatio(resource, ratio));
                    }
                    else
                    {
                        this.Log("Cannot parse \"" + inputResources + "\", something went wrong.");
                    }
                }

                var ratios = resources.Aggregate("", (result, value) => result + value.resource.name + ", " + value.ratio + ", ");
                this.Log("Resources parsed: " + ratios + "\nfrom " + inputResources);
                return resources;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in UpdateResourceList - {0}", ex.Message));
                throw;
            }
        }
        private string GetMissingFixedResources()
        {
            try
            {
                var missingResources = new List<string>();

                string[] tokens = RequiredResources.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < (tokens.Length - 1); i += 2)
                {
                    PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(tokens[i]);
                    double numRequired;
                    if (resource != null && double.TryParse(tokens[i + 1], out numRequired))
                    {
                        var amountAvailable = part.IsResourceAvailable(resource, numRequired);
                        if (amountAvailable < numRequired) missingResources.Add(resource.name); 
                    }
                }
                return string.Join(",", missingResources.ToArray());
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in GetMissingFixedResources - {0}", ex.Message));
                throw;
            }
        }
    }
}
