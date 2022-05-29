using System;
using System.Collections.Generic;
using System.Linq;
using USITools;

namespace KolonyTools
{
    public class AutoConverterConfig : PartModule
    {
        public string InputResource;
        public string OutputResource;
        public float yield;
    }

    public class MKS_BulkConverterSwapOption : USI_ConverterSwapOption
    {
        [KSPField] 
        public float Yield = 1f;

        [KSPField] 
        public float MinAbundance = 0f;

        private static List<string> _blackList = new List<string> { "Dirt", "ResourceLode" };
        private int _lastLoadedPlanet = -1;
        private List<ResourceRatio> _resourceYields;

        public override ConversionRecipe PrepareRecipe(ConversionRecipe recipe)
        {
            base.PrepareRecipe(recipe);

            CheckYieldsAreLoaded(recipe.Inputs);
            var staticOutputs = GetResourceNames(recipe.Outputs);

            var count = _resourceYields.Count;
            for (int i = 0; i < count; ++i)
            {
                var resourceOutput = _resourceYields[i];
                if (!staticOutputs.Contains(resourceOutput.ResourceName))
                    recipe.Outputs.Add(new ResourceRatio(resourceOutput.ResourceName, resourceOutput.Ratio, true));
            }

            return recipe;
        }

        private void CheckYieldsAreLoaded(List<ResourceRatio> inputs)
        {
            if (vessel.mainBody.flightGlobalsIndex != _lastLoadedPlanet || _resourceYields == null)
            {
                LoadPlanetYields(inputs);
            }
        }

        private void LoadPlanetYields(List<ResourceRatio> inputs)
        {
            _lastLoadedPlanet = vessel.mainBody.flightGlobalsIndex;
            List<string> inputResourcesNames = GetResourceNames(inputs);
            Dictionary<string, double> planetResourceAbundance = GetResourceAbundance(vessel.mainBody.flightGlobalsIndex, inputResourcesNames);

            var totalAbundance = planetResourceAbundance.Sum(r => r.Value);

            _resourceYields = new List<ResourceRatio>();
            
            foreach (var res in planetResourceAbundance)
            {
                if (res.Value > MinAbundance)
                {
                    var resourceYield =  Yield * res.Value / totalAbundance;
                    var newYield = new ResourceRatio {
                        FlowMode = ResourceFlowMode.ALL_VESSEL,
                        Ratio = resourceYield,
                        ResourceName = res.Key,
                        DumpExcess = true };

                    var con = KolonizationSetup.Instance.AutoConverters.Where(c => c.InputResource == res.Key).ToArray();
                    var count = con.Count();
                    if (count > 0)
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            var thisCon = con[i];
                            var conYield = new ResourceRatio
                            {
                                FlowMode = ResourceFlowMode.ALL_VESSEL,
                                Ratio = newYield.Ratio * thisCon.yield,
                                ResourceName = thisCon.OutputResource,
                                DumpExcess = true
                            };
                            _resourceYields.Add(conYield);
                        }
                    }
                    else
                    {
                        _resourceYields.Add(newYield);
                    }
                }
            }
        }

        private List<string> GetResourceNames(List<ResourceRatio> inputs)
        {
            var resNames = new List<string>();
            var count = inputs.Count;
            for (int i = 0; i < count; ++i)
            {
                resNames.Add(inputs[i].ResourceName);
            }
            return resNames;
        }

        private static Dictionary<string, double> GetResourceAbundance(int planetBodyIndex, List<string> inputResourcesNames)
        {
            Dictionary<string, double> planetResourceAbundance = new Dictionary<string, double>();
            foreach (var res in ResourceCache.Instance.AbundanceCache)
            {
                if (res.BodyId == planetBodyIndex)
                {
                    if (inputResourcesNames.Contains(res.ResourceName))
                        continue;
                    if (_blackList.Contains(res.ResourceName))
                        continue;

                    if (!planetResourceAbundance.ContainsKey(res.ResourceName))
                        planetResourceAbundance.Add(res.ResourceName, 0d);

                    planetResourceAbundance[res.ResourceName] += res.Abundance;
                }
            }
            return planetResourceAbundance;
        }
    }
}
