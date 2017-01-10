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

        private List<string> _blackList = new List<string>{"Dirt","ResourceLode"};

        private int _planet = -1;
        private Dictionary<string, double> _abundance;
        private List<string> _resNames;

        private void LoadAbundance(List<ResourceRatio> inputs)
        {
            if(_resNames == null)
                _resNames = GetResourceNames(inputs);

            _planet = vessel.mainBody.flightGlobalsIndex;
            _abundance = new Dictionary<string, double>();
            foreach (var res in ResourceCache.Instance.AbundanceCache)
            {
                if (res.BodyId == vessel.mainBody.flightGlobalsIndex)
                {
                    if (_resNames.Contains(res.ResourceName))
                        continue;
                    if (_blackList.Contains(res.ResourceName))
                        continue;

                    if(!_abundance.ContainsKey(res.ResourceName))
                        _abundance.Add(res.ResourceName,0d);

                    _abundance[res.ResourceName] += res.Abundance;
                }
            }

            var totAb = _abundance.Sum(r => r.Value);
            var abRat = 1f / totAb;

            _prs = new List<ResourceRatio>();
            foreach (var res in _abundance)
            {
                if (res.Value > MinAbundance)
                {
                    //print(String.Format("Adding {0} {1} ",res.Value,res.Key));
                    _prs.Add(new ResourceRatio { FlowMode = ResourceFlowMode.ALL_VESSEL, Ratio = res.Value * abRat * Yield, ResourceName = res.Key, DumpExcess = true });
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

        private List<ResourceRatio> _prs;

        private void CheckPlanetResourceList(List<ResourceRatio> inputs)
        {
            if (vessel.mainBody.flightGlobalsIndex != _planet || _prs == null)
            {
                LoadAbundance(inputs);
            }
        }

        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {
            if (!IsActivated)
                return new ConversionRecipe();

            var recipe = base.PrepareRecipe(deltatime);

            CheckPlanetResourceList(recipe.Inputs);
            var outputList = GetResourceNames(recipe.Outputs);

            var count = _prs.Count;
            for (int i = 0; i < count; ++i)
            {
                var op = _prs[i];
                if (!outputList.Contains(op.ResourceName))
                    recipe.Outputs.Add(new ResourceRatio(op.ResourceName, op.Ratio, true));
            }
            return recipe;
        }
    }
}