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
        public string RequiredResources = "";
        
        [KSPField]
        public bool SurfaceOnly = true;

        [KSPField]
        public bool AllowsEfficiency = true;

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
                base.OnFixedUpdate();
                conversionRate = baseConversionRate;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in OnFixedUpdate - {0}",ex.Message));
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
                efficiency = String.Format("{0}% [{1}k/{2}s/{3}m", Math.Round((eff * 100),1),Math.Round(numKerbals,1), numWorkspaces, numModules);
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
