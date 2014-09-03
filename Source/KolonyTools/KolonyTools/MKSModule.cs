using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
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

        [KSPField(guiActive = true, guiName = "Efficiency")]
        public string efficiency = "Unknown";

        [KSPEvent(guiActive = true, guiName = "Governor", active = true)]
        public void ToggleGovernor()
        {
            governorActive = !governorActive;
            EfficiencySetup();
        }

        [KSPField(isPersistant = true)]
        public bool governorActive;

        private int _numConverters;
        private float _efficiencyRate;
        private void EfficiencySetup()
        {
            _efficiencyRate = GetEfficiency();
        }

        private float GetEfficiency()
        {
            try
            {
                //Efficiency is a function of:
                //  - Crew Capacity (any module will work for this)
                //  - Workspaces
                //  - Crew Count
                //  - Active MKS Module count
                //  - module bonuses

                //  - efficiency parts
                //          Bonus equal to 100 * number of units - 1

                float numWorkspaces = GetKolonyWorkspaces(vessel);
                print("NumWorkspaces: " + numWorkspaces);
                //Plus 25% of Crew Cap as low efficiency workspaces
                numWorkspaces += vessel.GetCrewCapacity()*.25f; 
                print("AdjNumWorkspaces: " + numWorkspaces);
                var numModules = GetActiveKolonyModules(vessel);
                print("numModules: " + numModules);

                //  Part (x1.5):   0   2   1   
                //  Ship (x0.5):   2   0   1
                //  Total:         1   3   2

                float modKerbalFactor = 0;
                foreach (var k in part.protoModuleCrew)
                {
                    //A range from 1 to 2, average should be 1.5
                    modKerbalFactor += 2;
                    modKerbalFactor -= k.stupidity;
                    //modKerbalFactor -= .5f;
                }
                print("modKerbalFactor: " + modKerbalFactor);

                var numModuleKerbals = part.protoModuleCrew.Count();
                print("NumModuleKerbals: " + numModuleKerbals);

                var numShipKerbals = vessel.GetCrewCount() - numModuleKerbals;
                print("ShipKerbals: " + numShipKerbals);

                float numWeightedKerbals = (numShipKerbals * 0.5f) + modKerbalFactor;
                print("ShipKerbals: " + numShipKerbals);

                print("numWeightedKerbals: " + numWeightedKerbals);
                numWeightedKerbals *= GetCrewHappiness();
                print("numWeightedKerbals: " + numWeightedKerbals);

                //Worst case, 50% crewed, 25% uncrewed
                float eff = .25f;

                if (vessel.GetCrewCount() > 0)
                {
                    //TODO:Switch this to three workspaces max per Kerbal so that WS makes sense
                    float WorkSpaceKerbalRatio = numWorkspaces / vessel.GetCrewCount();
                    if (WorkSpaceKerbalRatio > 3) WorkSpaceKerbalRatio = 3;
                    print("WorkSpaceKerbalRatio: " + WorkSpaceKerbalRatio);

                    float WorkUnits = WorkSpaceKerbalRatio * numWeightedKerbals;
                    print("WorkUnits: " + WorkUnits);
                    eff = WorkUnits / numModules;
                    print("eff: " + eff);
                    if (eff > 2.5) eff = 2.5f;
                    if (eff < .5) eff = .5f;
                }

                print("effpartname: " + efficiencyPart);
                //Add in efficiencyParts 
                if (efficiencyPart != "")
                {
                    var genParts = vessel.Parts.Count(p => p.name == part.name);
                    var effParts = vessel.Parts.Count(p => p.name == (efficiencyPart.Replace('_','.')));

                    effParts = (effParts - genParts) / genParts;
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
                else if (governorActive)
                {
                    if (eff > 1f) eff = 1f;
                    efficiency = String.Format("G:{0}% [{1}k/{2}s/{3}m/{4}c]", Math.Round((eff * 100), 1), numShipKerbals,numWorkspaces, numModules,Math.Round(numWeightedKerbals,1));
                }
                else
                {
                    efficiency = String.Format("{0}% [{1}k/{2}s/{3}m/{4}c]", Math.Round((eff * 100), 1), numShipKerbals, numWorkspaces, numModules, Math.Round(numWeightedKerbals, 1));
                }
                return eff;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in GetEfficiency - {0}", ex.Message));
                return 1f;
            }
        }


        private float GetCrewHappiness()
        {
            //Prototype.  Crew Happiness is a function of the ratio of living space to Kerbals.
            float ls = GetKolonyLivingSpace(vessel);
            //We can add in a limited number for crew capacity - 10%
            ls += vessel.GetCrewCapacity()*.1f;

            var hap = ls/vessel.GetCrewCount();
            //Range is 50% - 150%
            if (hap < .5f) hap = .5f;
            if (hap > 1.5f) hap = 1.5f;
            return hap;
        }
        private int GetActiveKolonyModules(Vessel v)
        {
            try
            {
                var numMods = 0;
                var pList = v.parts.Where(p => p.Modules.Contains("KolonyConverter"));
                foreach (var p in pList)
                {
                    var mods = p.Modules.OfType<KolonyConverter>();
                    foreach (var pm in mods)
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
                    foreach (var pm in mods)
                    {
                        numWS += pm.workSpace;
                    }
                }
                return numWS;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in GetKolonyWorkspaces - {0}", ex.Message));
                return 0;
            }
        }


        private int GetKolonyLivingSpace(Vessel v)
        {
            try
            {
                var numLS = 0;
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
                return numLS;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in GetKolonyWorkspaces - {0}", ex.Message));
                return 0;
            }
        }
        

        public float GetEfficiencyRate()
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
                    Events["ToggleGovernor"].active = false;
                }
            }
            catch (Exception ex)
            {
                print("ERROR IN MKSModuleOnLoad - " + ex.Message);
            }
        }
    }
}
