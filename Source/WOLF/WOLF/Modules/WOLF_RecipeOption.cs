using KSP.Localization;
using System.Text;

namespace WOLF
{
    [KSPModule("Recipe")]
    public class WOLF_RecipeOption : PartModule
    {
        private static string NEEDS_TEXT = "#autoLOC_USI_WOLF_NEEDS"; // Needs
        private static string PROVIDES_TEXT = "#autoLOC_USI_WOLF_PROVIDES"; // Provides

        [KSPField]
        public string RecipeDisplayName = "WOLF Recipe Option";

        [KSPField]
        public string InputResources = string.Empty;

        [KSPField]
        public string OutputResources = string.Empty;

        public override string GetInfo()
        {
            var info = new StringBuilder();
            info
                .AppendLine(RecipeDisplayName)
                .AppendLine();

            if (!string.IsNullOrEmpty(InputResources))
            {
                var inputs = WOLF_AbstractPartModule.ParseRecipeIngredientList(InputResources);
                info.AppendFormat("<color=#99FF00>{0}:</color>", NEEDS_TEXT);
                info.AppendLine();
                foreach (var resource in inputs)
                {
                    info
                        .Append(" - ")
                        .Append(resource.Key)
                        .Append(": ")
                        .AppendFormat("{0:D}", resource.Value)
                        .AppendLine();
                }
            }
            if (!string.IsNullOrEmpty(OutputResources))
            {
                var outputs = WOLF_AbstractPartModule.ParseRecipeIngredientList(OutputResources);
                info.AppendFormat("<color=#99FF00>{0}:</color>", PROVIDES_TEXT);
                info.AppendLine();
                foreach (var resource in outputs)
                {
                    info
                        .Append(" - ")
                        .Append(resource.Key)
                        .Append(": ")
                        .AppendFormat("{0:D}", resource.Value)
                        .AppendLine();
                }
            }

            return info.ToString();
        }

        public override void OnAwake()
        {
            base.OnAwake();

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_NEEDS", out string needsText))
            {
                NEEDS_TEXT = needsText;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_PROVIDES", out string providesText))
            {
                PROVIDES_TEXT = providesText;
            }
        }
    }
}
