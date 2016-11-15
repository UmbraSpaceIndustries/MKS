using System;
using System.Text;
using USITools;

namespace KolonyTools
{
    public class ModuleEfficiencyPart : ModuleResourceConverter_USI
    {
        [KSPField(isPersistant = true)]
        public bool PartIsActive = false;

        [KSPField]
        public double eMultiplier = 1d;
        
        [KSPField]
        public string eTag = "";

        [KSPField(guiName = "Governor", isPersistant = true, guiActive = true, guiActiveEditor = false), UI_FloatRange(stepIncrement = 0.5f, maxValue = 1f, minValue = 0f)]
        public float Governor = 0.5f;

        private double _currEff;

        public double CurrentEfficiency
        {
            get
            {
                if (HighLogic.LoadedSceneIsEditor)
                    return eMultiplier * Governor;
                if (!PartIsActive || !IsActivated)
                    _currEff = 0d;
                return _currEff * Governor;
            }
            set
            {
                _currEff = value;
            }
        }

        protected override void PostProcess(ConverterResults result, double deltaTime)
        {
            PartIsActive = Math.Abs(deltaTime - result.TimeFactor) < ResourceUtilities.FLOAT_TOLERANCE;
            CurrentEfficiency = result.TimeFactor/deltaTime * eMultiplier;
        }
    }
}