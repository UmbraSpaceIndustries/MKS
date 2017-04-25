using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KolonyTools
{ 
    public class KolonyACOptions : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Customized Kerbonauts", toolTip = "If enabled, allows the customization of new Kerbonauts.", autoPersistance = true)]
        public bool CustomKerbonauts = true;

        [GameParameters.CustomParameterUI("Enable Kolonist Hiring", toolTip = "If enabled, allows the hiring of MKS specific Kerbonauts.", autoPersistance = true)]
        public bool KolonistHiring = true;

        [GameParameters.CustomParameterUI("Enable Kolonist Rescues", toolTip = "If enabled, allows MKS specific Kerbonauts to be included in rescue contracts.", autoPersistance = true)]
        public bool KolonistRescue = false;

        [GameParameters.CustomParameterUI("Veteran Rescue Kerbals", toolTip = "If enabled, rescued Kerbals have a chance of having one or more stars.", autoPersistance = true)]
        public bool VeteranRescue = true;

        [GameParameters.CustomParameterUI("Alternate Core Kerbonaut Costs", toolTip = "If enabled, overrides the stock Kerbonaut cost calculations with a new base cost for pilots, engineers, and scientists.", autoPersistance = true)]
        public bool AlternateCoreCost = true;

        [GameParameters.CustomIntParameterUI("Core Kerbonaut Cost", toolTip = "Base cost for Engineers, Pilots, and Scientitst", autoPersistance = true, minValue = 0, maxValue = 1000000, stepSize = 1000)]
        public int CoreCost = 50000;

        [GameParameters.CustomParameterUI("Alternate Kolonist Costs", toolTip = "If enabled, overrides the stock Kerbonaut cost calculations with a new base cost for Kolonists.", autoPersistance = true)]
        public bool AlternateKolonistCost = true;

        [GameParameters.CustomIntParameterUI("Kolonist Cost", toolTip = "Base cost for secondary professions", autoPersistance = true, minValue = 0, maxValue = 1000000, stepSize = 1000)]
        public int KolonistCost = 1000;

        [GameParameters.CustomParameterUI("Enable Hiring Cost Cap", toolTip = "If enabled, puts a hard cap on the cost of a new hire", autoPersistance = true)]
        public bool CostCap = true;

        [GameParameters.CustomIntParameterUI("Max Hire Cost", toolTip = "The maximum cost of any hire", autoPersistance = true, minValue = 0, maxValue = 1000000, stepSize = 1000)]
        public int MaxCost = 500000;

        public static bool CustomKerbonautsEnabled
        {
            get
            {
                KolonyACOptions options = HighLogic.CurrentGame.Parameters.CustomParams<KolonyACOptions>();
                return options.CustomKerbonauts;
            }
        }

        public static bool KolonistHiringEnabled
        {
            get
            {
                KolonyACOptions options = HighLogic.CurrentGame.Parameters.CustomParams<KolonyACOptions>();
                return options.KolonistHiring;
            }
        }

        public static bool KolonistRescueEnabled
        {
            get
            {
                KolonyACOptions options = HighLogic.CurrentGame.Parameters.CustomParams<KolonyACOptions>();
                return options.KolonistRescue;
            }
        }

        public static bool VeteranRescueEnabled
        {
            get
            {
                KolonyACOptions options = HighLogic.CurrentGame.Parameters.CustomParams<KolonyACOptions>();
                return options.VeteranRescue;
            }
        }


        public static bool AlternateCoreCostEnabled
        {
            get
            {
                KolonyACOptions options = HighLogic.CurrentGame.Parameters.CustomParams<KolonyACOptions>();
                return options.AlternateCoreCost;
            }
        }

        public static int GetCoreCost
        {
            get
            {
                KolonyACOptions options = HighLogic.CurrentGame.Parameters.CustomParams<KolonyACOptions>();
                return options.CoreCost;
            }
        }


        public static bool AlternateKolonistCostEnabled
        {
            get
            {
                KolonyACOptions options = HighLogic.CurrentGame.Parameters.CustomParams<KolonyACOptions>();
                return options.AlternateCoreCost;
            }
        }


        public static int GetKolonistCost
        {
            get
            {
                KolonyACOptions options = HighLogic.CurrentGame.Parameters.CustomParams<KolonyACOptions>();
                return options.KolonistCost;
            }
        }

        public static bool CostCapEnabled
        {
            get
            {
                KolonyACOptions options = HighLogic.CurrentGame.Parameters.CustomParams<KolonyACOptions>();
                return options.CostCap;
            }
        }

        public static int GetMaxCost
        {
            get
            {
                KolonyACOptions options = HighLogic.CurrentGame.Parameters.CustomParams<KolonyACOptions>();
                return options.MaxCost;
            }
        }

        public override string Section
        {
            get
            {
                return "Kolonization";
            }
        }

        public override string Title
        {
            get
            {
                return "Astronaut Complex";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 0;
            }
        }

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            base.SetDifficultyPreset(preset);
        }

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }
    }
}
