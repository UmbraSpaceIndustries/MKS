using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using USITools;


namespace KolonyTools
{
    public class MKSModule : PartModule
    {
        [KSPField]
        public bool calculateEfficiency = true;

        [KSPField] 
        public string efficiencyPart = "";

        [KSPField] 
        public int workSpace = 0;

        [KSPField] 
        public int livingSpace = 0;

        [KSPField] 
        public bool hasGenerators = true;

        [KSPField] 
        public string PrimarySkill = "Engineer";

        [KSPField]
        public string SecondarySkill = "Scientist";
        
        [KSPField(guiActive = true, guiName = "Efficiency")]
        public string efficiency = "Unknown";

        [KSPField] 
        public float CrewBonus = 0.1f;

        [KSPField] 
        public float MaxEfficiency = 2.5f;

        private bool _showGUI = true;
        private int _numConverters;
        private float _efficiencyRate;
        private double lastCheck;
        private double checkTime = 5f;
        private const int COLONY_RANGE = 100;

        private void EfficiencySetup()
        {
            _efficiencyRate = GetEfficiency();
        }

        public bool ShowGUI
        {
            get
            {
                return _showGUI;
            }

            set
            {
                _showGUI = value;
                //Hide/show MKSModule gui
                if (Fields["Efficiency"] != null)
                    Fields["Efficiency"].guiActive = _showGUI;
            }
        }

        private float GetEfficiency()
        {
            try
            {
                //Efficiency is based on various factors.  These come in three
                //categories: 
                //  * Part
                //  * Vessel
                //  * Colony
                //  - Vessel Workspaces         [numWorkspaces]
                //  - 25% Vessel Crew Capacity  [numWorkSpaces]
                //  - Vessel MKS Module count   [numModules]
                //  - Part Crew                 [modKerbalFactor]   (0.05 - 3.75 per Kerbal)
                //  - Vessel crew               [numWeightedKerbals]
                //  - Colony efficiency parts   [added to eff]
                //          Bonus equal to 100 * number of units - 1
                //  - Colony Living Space/Kerbal Happiness

                float numWorkspaces = GetKolonyWorkspaces(vessel);
                print("NumWorkspaces: " + numWorkspaces);

                //Plus 25% of Crew Cap as low efficiency workspaces
                numWorkspaces += vessel.GetCrewCapacity()*.25f; 
                print("AdjNumWorkspaces: " + numWorkspaces);

                //Number of active modules
                var numModules = GetActiveKolonyModules(vessel);
                print("numModules: " + numModules);

                //Kerbals in the module
                float modKerbalFactor = part.protoModuleCrew.Sum(k => GetKerbalFactor(k));
                print("modKerbalFactor: " + modKerbalFactor);
                modKerbalFactor *= GetCrewHappiness();
                print("HappymodKerbalFactor: " + modKerbalFactor);

                //Kerbals in the ship
                float numWeightedKerbals = vessel.GetVesselCrew().Sum(k => GetKerbalFactor(k));
                print("numWeightedKerbals: " + numWeightedKerbals);
                numWeightedKerbals *= GetCrewHappiness();
                print("HappynumWeightedKerbals: " + numWeightedKerbals);

                //Worst case, 25% (if crewed).  Uncrewed vessels will be at 0%
                //You need crew for these things, no robo ships.
                float eff = .0f;
                if (vessel.GetCrewCount() > 0)
                {
                    float WorkSpaceKerbalRatio = numWorkspaces / vessel.GetCrewCount();
                    if (WorkSpaceKerbalRatio > 3) WorkSpaceKerbalRatio = 3;
                    print("WorkSpaceKerbalRatio: " + WorkSpaceKerbalRatio);
                    //A module gets 100% bonus from Kerbals inside of it,
                    //in addition to a 10% bonus for Kerbals in the entire station.
                    float WorkUnits = WorkSpaceKerbalRatio * modKerbalFactor;
                    WorkUnits += WorkSpaceKerbalRatio*numWeightedKerbals*CrewBonus;
                    print("WorkUnits: " + WorkUnits);
                    eff = WorkUnits / numModules;
                    print("eff: " + eff);
                    if (eff > MaxEfficiency) eff = MaxEfficiency;
                    if (eff < .25) eff = .25f;
                }

                //Add in efficiencyParts 
                if (efficiencyPart != "")
                {
                    print("effpartname: " + efficiencyPart);
                    var validEffParts = new List<EffPart>();
                    var effPartBits = efficiencyPart.Split(',')
                        .Select(effPartName => effPartName.Trim().Replace('_', '.')).ToArray();

                    for(int i = 0; i < effPartBits.Count(); i +=2)
                    {
                        validEffParts.Add(new EffPart
                            {
                                Name = effPartBits[i],
                                Multiplier = float.Parse(effPartBits[i+1])
                            });
                    }

                    var effParts = 0f;
                    foreach (var vep in validEffParts)
                    {
                        var vList = LogisticsTools.GetNearbyVessels(EFF_RANGE, true, vessel, true);
                        var effPartList = new List<Part>();
                        foreach (var v in vList)
                        {
                            effPartList.AddRange(v.Parts.Where(p => p.name == vep.Name));
                        }

                        foreach (var ep in effPartList)
                        {
                            var mod = ep.FindModuleImplementing<USIAnimation>();
                            if (mod == null)
                            {
                                effParts += vep.Multiplier;
                            }
                            else
                            {
                                if (mod.isDeployed)
                                    effParts += vep.Multiplier;
                            }
                        }
                    }
                    print("effParts: " + effParts);
                    print("oldEff: " + eff);
                    eff += effParts;
                    print("newEff: " + eff); 
                    if (eff < 0.25)  
                        eff = 0.25f;  //We can go as low as 25% as these are almost mandatory.
                }

                if (!calculateEfficiency)
                {
                    eff = 1f;
                    efficiency = String.Format("100% [Fixed]");
                }

                efficiency = String.Format("{0}%", Math.Round((eff * 100), 1));

                //DEBUG DATA
                //DEBUG

                return eff;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in GetEfficiency - {0}", ex.Message));
                return 1f;
            }
        }

        private float GetKerbalFactor(ProtoCrewMember k)
        {
            var kerbalFactor = k.experienceLevel / 2f;
            //A level 0 Kerbal is not quite zero - it.s 0.1
            if (kerbalFactor < 0.1)
                kerbalFactor = 0.1f;
            
            // Level 0 Pilot:       0.05
            // Level 0 Engineer:    0.15
            // Level 1 Pilot:       0.25
            // Level 1 Engineer:    0.75
            // Level 2 Pilot:       0.50
            // Level 2 Engineer:    1.50
            // Level 5 Pilot:       1.25
            // Level 5 engineer:    3.75

            //(0.05 - 3.75)
            if (k.experienceTrait.Title == PrimarySkill)
            {
                kerbalFactor *= 1.5f;
            }
            else if (k.experienceTrait.Title == SecondarySkill)
            {
                kerbalFactor *= 1f;
            }
            else 
            {
                kerbalFactor *= 0.5f;
            }
            return kerbalFactor;
        }

        private float GetCrewHappiness()
        {
            //Crew Happiness is a function of the ratio of living space to Kerbals.
            //These are COLONY-WIDE.
            var kShips = LogisticsTools.GetNearbyVessels(COLONY_RANGE, true, vessel, true);
            float ls = GetKolonyLivingSpace(kShips);
            //We can add in a limited number for crew capacity - 10%
            ls += GetKolonyCrewCap(kShips) * .1f;

            var totKerbs = GetKolonyInhabitants(kShips);
            var hap = 0f;
            if(totKerbs > 0)
                hap = ls/totKerbs;

            //Range is 50% - 150% for crowding and extra space.
            //This is calculated before loneliness.
            if (hap < .5f) hap = .5f;
            if (hap > 1.5f) hap = 1.5f;

            //Kerbals hate being alone.  Any fewer than five Kerbals incurs a pretty significant penalty.
            if (totKerbs < 5)
            {
                //20% - 80%
                hap *= (totKerbs * .2f);
            }
            return hap;
        }

        private float GetKolonyCrewCap(List<Vessel> vlist)
        {
            var cc = 0f;
            foreach (var v in vlist)
            {
                cc += v.GetCrewCapacity();
            }
            return cc;
        }

        private float GetKolonyInhabitants(List<Vessel> vlist)
        {
            var cc = 0f;
            foreach (var v in vlist)
            {
                cc += v.GetCrewCount();
            }
            return cc;
        }


        private int GetActiveKolonyModules(Vessel v)
        {
            try
            {
                var numMods = 0;
                var pList = v.parts.Where(p => p.Modules.Contains("ModuleResourceConverter"));
                foreach (var p in pList)
                {
                    var mods = p.Modules.OfType<ModuleResourceConverter>();
                    numMods += mods.Count(pm => pm.IsActivated);
                }
                return numMods;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in GetActiveKolonyModules - {0}", ex.Message));
                return 0;
            }
        }

        private int GetKolonyWorkspaces(Vessel v)
        {
            try
            {
                var numWS = 0;
                var pList = v.parts.Where(p => p.Modules.Contains("MKSModule"));
                foreach (var p in pList)
                {
                    var mods = p.Modules.OfType<MKSModule>();
                    numWS += mods.Sum(pm => pm.workSpace);
                }
                return numWS;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in GetKolonyWorkspaces - {0}", ex.Message));
                return 0;
            }
        }
        private int GetKolonyLivingSpace(List<Vessel> vList)
        {
            try
            {
                var numLS = 0;
                foreach (var v in vList)
                {
                    var pList = v.parts.Where(p => p.Modules.Contains("MKSModule"));
                    foreach (var p in pList)
                    {
                        var mods = p.Modules.OfType<MKSModule>();
                        foreach (var pm in mods)
                        {
                            if (p.Modules.Contains("USIAnimation"))
                            {
                                var am = p.Modules.OfType<USIAnimation>().First();
                                if (am.isDeployed)
                                {
                                    numLS += pm.livingSpace;
                                }
                            }
                            else
                            {
                                numLS += pm.livingSpace;
                            }
                        }
                    }
                }
                return numLS;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in GetKolonyWorkspaces - {0}", ex.Message));
                return 0;
            }
        }
        
        public virtual float GetEfficiencyRate()
        {
            var curConverters = GetActiveKolonyModules(vessel);
            if (curConverters != _numConverters)
            {
                _numConverters = curConverters;
                EfficiencySetup();
            }
            return _efficiencyRate;
        }

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                if (!hasGenerators)
                {
                    Fields["efficiency"].guiActive = false;
                }
            }
            catch (Exception ex)
            {
                print("ERROR IN MKSModuleOnLoad - " + ex.Message);
            }
        }

        public override void OnStart(StartState state)
        {
            part.force_activate();
        }

        public override void OnFixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (Math.Abs(lastCheck - Planetarium.GetUniversalTime()) < checkTime)
                return;

            lastCheck = Planetarium.GetUniversalTime();

            var conEff = GetEfficiencyRate();
            foreach (var con in part.FindModulesImplementing<ModuleResourceConverter>())
            {
                con.EfficiencyBonus = conEff;
                if(con.inputList != null)
                    CheckLogistics(con.inputList);
            }
           
            //Special for USI-LS/TAC-LS
            if(vessel.GetCrewCount() > 0)
            {
                CheckLogistics(new List<ResourceRatio>
                               {
                                   new ResourceRatio { ResourceName = "Supplies" }, 
                                   new ResourceRatio { ResourceName = "Food" }, 
                                   new ResourceRatio { ResourceName = "Water" }, 
                                   new ResourceRatio { ResourceName = "Oxygen" }, 
                                   new ResourceRatio { ResourceName = "ElectricCharge" }
                               });
            }
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
                double fillPercent = curAmount/maxAmount; //We use this to equalize things cross-ship as a percentage.
                if (fillPercent < 0.5d) //We will not attempt a fillup until we're at less than half capacity
                {
                    //Keep changes small - 10% per tick.  So we should hover between 50% and 60%
                    var deficit = Math.Floor(maxAmount * .1d);
                    double receipt = FetchResources(deficit, pRes, fillPercent, maxAmount);
                    //Put these in our vessel
                    StoreResources(receipt, pRes);
                }
            }
        }

        private const int LOG_RANGE = 750;
        private const int DEPOT_RANGE = 150;
        private const int POWER_RANGE = 2000;
        private const int EFF_RANGE = 500;

        public bool LogisticsAvailable()
        {
            var vList = LogisticsTools.GetNearbyVessels(DEPOT_RANGE, true, vessel, true);
            foreach (var v in vList.Where(v=>v.GetTotalMass() <= 3f))
            {
                if (v.Parts.Any(p => p.FindModuleImplementing<ModuleResourceDistributor>() != null && HasCrew(p, "Pilot")))
                    return true;
            }
            return false;
        }

        public bool PowerAvailable()
        {
            var vList = LogisticsTools.GetNearbyVessels(POWER_RANGE, true, vessel, true);
            foreach (var v in vList)
            {
                if (v.Parts.Any(p => p.FindModuleImplementing<ModulePowerDistributor>() != null && HasCrew(p, "Engineer")))
                    return true;
            }
            return false;
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
                if (resource.name == "ElectricCharge")
                    rangeFactor = POWER_RANGE;

                var nearVessels = LogisticsTools.GetNearbyVessels(rangeFactor, false, vessel, true);
                foreach (var v in nearVessels)
                {
                    if (demand <= ResourceUtilities.FLOAT_TOLERANCE) break;
                    //Is this a valid target?
                    var maxToSpare = GetAmountOfResourcesToSpare(v, resource, fillPercent, targetMaxAmount);
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
            double fillPercent = curAmount/maxAmount;
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


        private struct EffPart
        {
            public string Name { get; set; }
            public float Multiplier { get; set; }
        }
    }
}
