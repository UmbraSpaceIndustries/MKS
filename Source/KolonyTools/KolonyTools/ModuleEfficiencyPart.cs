using System;
using System.Text;

namespace KolonyTools
{
    public class ModuleEfficiencyPart : ModuleResourceConverter
    {
        [KSPField(isPersistant = true)]
        public bool PartIsActive = false;

        [KSPField(isPersistant = true)]
        public double CurrentEfficiency;

        [KSPField]
        public double eMultiplier = 1d;

        [KSPField]
        public string eTag = "";

        protected override void PostProcess(ConverterResults result, double deltaTime)
        {
            PartIsActive = Math.Abs(deltaTime - result.TimeFactor) < ResourceUtilities.FLOAT_TOLERANCE;
            CurrentEfficiency = result.TimeFactor/deltaTime * eMultiplier;
        }
    }
}