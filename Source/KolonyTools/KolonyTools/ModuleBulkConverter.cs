using System;
using System.Collections.Generic;
using System.Linq;

namespace KolonyTools
{
    public class ModuleBulkConverter : ModuleResourceConverter
    {
        [KSPField] 
        public float Yield = 1f;

        [KSPField] 
        public float MinAbundance = 0f;

        private List<ResourceRatio> GetPlanetResourceList(List<string> inputs)
        {
            var prs = new List<ResourceRatio>();
            var resList = ResourceCache.Instance.AbundanceCache.Where(a => a.BodyId == vessel.mainBody.flightGlobalsIndex
                                                                           && a.HarvestType == HarvestTypes.Planetary);

            var resNames = ResourceMap.Instance.FetchAllResourceNames(HarvestTypes.Planetary);

            var allRes = resNames.Where(rn => !inputs.Contains(rn));
            foreach (var res in allRes)
            {
                var abundanceList = resList.Where(rn => rn.ResourceName == res);
                if (abundanceList.Any())
                {
                    var abundance = abundanceList.Average(ra => ra.Abundance);
                    if (abundance > MinAbundance)
                    {
                        print(String.Format("Adding {0} {1} ",abundance,res));
                        prs.Add(new ResourceRatio { FlowMode = ResourceFlowMode.ALL_VESSEL, Ratio = abundance * Yield, ResourceName = res, DumpExcess = true });
                    }
                }
            }
            return prs;
        }

        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {
            if (!IsActivated)
                return new ConversionRecipe();

            var recipe = base.PrepareRecipe(deltatime);
            recipe.Outputs.AddRange(GetPlanetResourceList(recipe.Outputs.Select(o=>o.ResourceName).ToList()));
            return recipe;
        }
    }
}