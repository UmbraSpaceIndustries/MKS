using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tac;

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
    
    public class KolonyConverter : TacGenericConverter
    {
        [KSPField]
        public string requiredResources = "";
        
        [KSPField]
        public bool SurfaceOnly = true;

        private List<ResourceRatio> inputResourceList;
        private List<ResourceRatio> outputResourceList;
        private List<ResourceRatio> requiredResourceList;
        private float _baseConversionRate;
        private MKSModule _mks;

        private static char[] delimiters = { ' ', ',', '\t', ';' };

        public override void OnFixedUpdate()
        {
            try
            {
                _baseConversionRate = conversionRate;
                var newMessage = "";
                var eff = _mks.GetEfficiencyRate();
                conversionRate = conversionRate * eff;

                if (SurfaceOnly && !vessel.Landed)
                {
                    newMessage = "Cannot operate while in flight";
                    conversionRate = 0.0001f;
                }

                var missingFixedResources = GetMissingFixedResources();
                if (!String.IsNullOrEmpty(missingFixedResources))
                {
                    newMessage = "Missing " + missingFixedResources;
                    conversionRate = 0.0001f;
                }
                base.OnFixedUpdate();
                conversionRate = _baseConversionRate;
                if (!String.IsNullOrEmpty(newMessage)) converterStatus = newMessage;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in OnFixedUpdate - {0}",ex.Message));
            }
        }

        public override string GetInfo()
        {
            var sb = new StringBuilder();
            sb.Append("Generator: ");
            sb.Append(converterName);
            sb.AppendLine();
            getRateGroupInfo(sb, "Inputs", inputResourceList);
            getRateGroupInfo(sb, "Outputs", outputResourceList);
            getReqInfo(sb, "Required", requiredResourceList);
            return sb.ToString();
        }

        private static void getRateGroupInfo(StringBuilder sb, String heading, IEnumerable<ResourceRatio> rates)
        {
            sb.Append("<b><color=#99ff00ff>");
            sb.Append(heading);
            sb.AppendLine(":</color></b>");
            sb.Append("<color=#99ff00ff>");
            sb.Append("(kerbin days - 6h)");
            sb.AppendLine(":</color>");
            foreach (var rate in rates)
            {
                var rstr = (rate.ratio * 4).ToString();
                if (rate.ratio >= 2500)
                {
                    rstr = Math.Round(rate.ratio/250, 0) + "k";
                }
                sb.AppendFormat("- <b>{0}</b>: {1:N2}/d", rate.resource.name, rstr);
                sb.AppendLine();
            }
        }

        private static void getReqInfo(StringBuilder sb, String heading, IEnumerable<ResourceRatio> rates)
        {
            sb.Append("<b><color=#99ff00ff>");
            sb.Append(heading);
            sb.AppendLine(":</color></b>");
            sb.Append("<color=#99ff00ff>");
            sb.Append("(Fixed - not consumed)");
            sb.AppendLine(":</color>");
            foreach (var rate in rates)
            {
                var rstr = (rate.ratio).ToString();
                if (rate.ratio >= 10000)
                {
                    rstr = Math.Round(rate.ratio / 1000, 0) + "k";
                }
                sb.AppendFormat("- <b>{0}</b>: {1:N2}", rate.resource.name, rstr);
                sb.AppendLine();
            }
        }


        public override void OnAwake()
        {
            try
            {
                print("[MKS] Awake!");
                ResourceSetup();
                base.OnAwake();
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in OnAwake - {0}", ex.Message));
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                ResourceSetup();
                base.OnLoad(node);
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in OnLoad - {0}", ex.Message));
            }
        }

        private void ResourceSetup()
        {
            try
            {
                inputResourceList = UpdateResourceList(inputResources,2);
                outputResourceList = UpdateResourceList(outputResources,3);
                requiredResourceList = UpdateResourceList(requiredResources,2);
                _mks = part.Modules.OfType<MKSModule>().Any() 
                    ? part.Modules.OfType<MKSModule>().First() 
                    : new MKSModule();
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in ResourceSetup - {0}", ex.Message));
            }
        }

        private List<ResourceRatio> UpdateResourceList(string resString, int skip)
        {
            try
            {
                var resources = new List<ResourceRatio>();
                string[] tokens = resString.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < (tokens.Length - 1); i += skip)
                {
                    PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(tokens[i]);
                    double ratio;
                    if (resource != null && double.TryParse(tokens[i + 1], out ratio))
                    {
                        resources.Add(new ResourceRatio(resource, ratio));
                    }
                    else
                    {
                        this.Log("Cannot parse \"" + resString + "\", something went wrong.");
                    }
                }

                var ratios = resources.Aggregate("", (result, value) => result + value.resource.name + ", " + value.ratio + ", ");
                this.Log("Resources parsed: " + ratios + "\nfrom " + inputResources);
                return resources;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in UpdateResourceList - {0}", ex.Message));
                return new List<ResourceRatio>();
            }
        }
        private string GetMissingFixedResources()
        {
            try
            {
                var missingResources = new List<string>();

                string[] tokens = requiredResources.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

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
                return "";
            }
        }
    }
}
