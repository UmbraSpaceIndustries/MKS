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
                        var r = part.Resources["resource"];
                        var amountAvailable = r.amount;
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
