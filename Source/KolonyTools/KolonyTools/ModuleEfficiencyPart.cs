using System;
using System.Collections.Generic;
using System.Text;
using USITools;

namespace KolonyTools
{
    [Obsolete("Use ModuleResourceConverter_USI instead.")]
    public class ModuleEfficiencyPart : ModuleResourceConverter_USI
    {
        public override string GetInfo()
        {
            if (string.IsNullOrEmpty(eTag))
                return base.GetInfo();
            var resourceConsumption = base.GetInfo();
            int index = resourceConsumption.IndexOf("\n"); // Strip the first line containing the etag
            resourceConsumption = resourceConsumption.Substring(index + 1);
            return "Boosts efficiency of converters benefiting from a " + eTag + "\n\n" +
                "Boost power: " + eMultiplier.ToString() + resourceConsumption;
        }

        protected override void PostProcess(ConverterResults result, double deltaTime)
        {
            base.PostProcess(result, deltaTime);
            EfficiencyMultiplier = result.TimeFactor / deltaTime;
        }
    }
}
