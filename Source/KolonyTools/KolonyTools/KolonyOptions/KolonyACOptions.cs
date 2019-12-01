using KSP.Localization;
namespace KolonyTools
{
    public class KolonyACOptions : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("#LOC_USI_KolonyOptions_CustomKerbonauts", toolTip = "#LOC_USI_KolonyOptions_CustomKerbonauts_desc", autoPersistance = true)]//Customized Kerbonauts//If enabled, allows the customization of new Kerbonauts.
        public bool CustomKerbonauts = true;

        [GameParameters.CustomParameterUI("#LOC_USI_KolonyOptions_KolonistHiring", toolTip = "#LOC_USI_KolonyOptions_KolonistHiring_desc", autoPersistance = true)]//Enable Kolonist Hiring//If enabled, allows the hiring of MKS specific Kerbonauts.
        public bool KolonistHiring = true;

        [GameParameters.CustomParameterUI("#LOC_USI_KolonyOptions_KolonistRescue", toolTip = "#LOC_USI_KolonyOptions_KolonistRescue_desc", autoPersistance = true)]//Enable Kolonist Rescues//If enabled, allows MKS specific Kerbonauts to be included in rescue contracts.
        public bool KolonistRescue = false;

        [GameParameters.CustomParameterUI("#LOC_USI_KolonyOptions_VeteranRescue", toolTip = "#LOC_USI_KolonyOptions_VeteranRescue_desc", autoPersistance = true)]//Veteran Rescue Kerbals//If enabled, rescued Kerbals have a chance of having one or more stars.
        public bool VeteranRescue = true;

        [GameParameters.CustomParameterUI("#LOC_USI_KolonyOptions_AlternateCoreCost", toolTip = "#LOC_USI_KolonyOptions_AlternateCoreCost_desc", autoPersistance = true)]//Alternate Core Kerbonaut Costs//If enabled, overrides the stock Kerbonaut cost calculations with a new base cost for pilots, engineers, and scientists.
        public bool AlternateCoreCost = true;

        [GameParameters.CustomIntParameterUI("#LOC_USI_KolonyOptions_CoreCost", toolTip = "#LOC_USI_KolonyOptions_CoreCost_desc", autoPersistance = true, minValue = 0, maxValue = 1000000, stepSize = 1000)]//Core Kerbonaut Cost//Base cost for Engineers, Pilots, and Scientitst
        public int CoreCost = 50000;

        [GameParameters.CustomParameterUI("#LOC_USI_KolonyOptions_AlternateKolonistCost", toolTip = "#LOC_USI_KolonyOptions_AlternateKolonistCost_desc", autoPersistance = true)]//Alternate Kolonist Costs//If enabled, overrides the stock Kerbonaut cost calculations with a new base cost for Kolonists.
        public bool AlternateKolonistCost = true;

        [GameParameters.CustomIntParameterUI("#LOC_USI_KolonyOptions_KolonistCost", toolTip = "#LOC_USI_KolonyOptions_KolonistCost_desc", autoPersistance = true, minValue = 0, maxValue = 1000000, stepSize = 1000)]//Kolonist Cost//Base cost for secondary professions
        public int KolonistCost = 25000;

        [GameParameters.CustomParameterUI("#LOC_USI_KolonyOptions_CostCap", toolTip = "#LOC_USI_KolonyOptions_CostCap_desc", autoPersistance = true)]//Enable Hiring Cost Cap//If enabled, puts a hard cap on the cost of a new hire
        public bool CostCap = true;

        [GameParameters.CustomIntParameterUI("#LOC_USI_KolonyOptions_MaxCost", toolTip = "#LOC_USI_KolonyOptions_MaxCost_desc", autoPersistance = true, minValue = 0, maxValue = 1000000, stepSize = 1000)]//Max Hire Cost//The maximum cost of any hire
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
                return options.AlternateKolonistCost;
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

        public override string DisplaySection
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
                return Localizer.Format("#LOC_USI_KolonyOptions_title");//"Astronaut Complex"
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
