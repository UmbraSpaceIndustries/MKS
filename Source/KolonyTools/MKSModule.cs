using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace KolonyTools
{
    public class MKSModule : PartModule
    {
        [KSPField]
        public bool calculateEfficiency = true;

        [KSPField] 
        public string efficiencyPart = "";

        [KSPField(guiActive = true, guiName = "Efficiency")]
        public string efficiency = "Unknown";

        [KSPEvent(guiActive = true, guiName = "Governor", active = true)]
        public void ToggleGovernor()
        {
            _governorActive = !_governorActive;
            EfficiencySetup();
        }

        private bool _governorActive;
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
                //  - Crew Count
                //  - Active MKS Module count
                //  - module bonuses

                //  - efficiency parts
                //          Bonus equal to 100 * number of units - 1

                var numWorkspaces = vessel.GetCrewCapacity();
                var numModules = GetActiveKolonyModules(vessel);

                //  Part (x1.5):   0   2   1   
                //  Ship (x0.5):   2   0   1
                //  Total:         1   3   2

                float modKerbalFactor = 0;
                foreach (var k in part.protoModuleCrew)
                {
                    //A range from 1 to 2, average should be 1.5
                    modKerbalFactor += 2;
                    modKerbalFactor -= k.stupidity;
                }
                var numModuleKerbals = part.protoModuleCrew.Count();
                var numShipKerbals = vessel.GetCrewCount() - numModuleKerbals;

                float numKerbals = (numShipKerbals * 0.5f) + modKerbalFactor;

                //Worst case, 50% crewed, 25% uncrewed
                float eff = .25f;

                if (numKerbals > 0)
                {
                    //Switch this to three workspaces max per Kerbal so that WS makes sense
                    float WorkSpaceKerbalRatio = numWorkspaces / numKerbals;
                    if (WorkSpaceKerbalRatio > 3) WorkSpaceKerbalRatio = 3;

                    float WorkUnits = WorkSpaceKerbalRatio * numKerbals;
                    eff = WorkUnits / numModules;
                    if (eff > 2.5) eff = 2.5f;
                    if (eff < .5) eff = .5f;
                }

                //Add in efficiencyParts 
                if (efficiencyPart != "")
                {
                    var genParts = vessel.Parts.Count(p => p.name == part.name);
                    var effParts = vessel.Parts.Count(p => p.name == efficiencyPart);

                    effParts = (effParts - genParts) / genParts;
                    eff += effParts;
                    if (eff < 0.25)  
                        eff = 0.25f;  //We can go as low as 25% as these are almost mandatory.
                }

                if (!calculateEfficiency)
                {
                    eff = 1f;
                    efficiency = String.Format("100% [Fixed]", Math.Round((eff * 100), 1), Math.Round(numKerbals, 1), numWorkspaces, numModules);
                }
                else if (_governorActive)
                {
                    if (eff > 1f) eff = 1f;
                    efficiency = String.Format("G:{0}% [{1}k/{2}s/{3}m]", Math.Round((eff * 100), 1), Math.Round(numKerbals, 1), numWorkspaces, numModules);
                }
                else
                {
                    efficiency = String.Format("{0}% [{1}k/{2}s/{3}m]", Math.Round((eff * 100), 1), Math.Round(numKerbals, 1), numWorkspaces, numModules);
                }
                return eff;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in GetEfficiency - {0}", ex.Message));
                return 1f;
            }
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

    }
}
