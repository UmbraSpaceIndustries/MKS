using System.Text;
using USITools;

namespace WOLF
{
    [KSPModule("Hopper Option")]
    public class WOLF_HopperSwapOption : USI_ConverterSwapOption
    {
        [KSPField]
        public string InputResources = string.Empty;

        public override void ApplyConverterChanges(USI_Converter converter)
        {
            if (converter is WOLF_HopperModule)
            {
                var hopper = converter as WOLF_HopperModule;
                hopper.InputResources = InputResources;
                hopper.ParseWolfRecipe();
            }

            base.ApplyConverterChanges(converter);
        }

        public override string GetInfo()
        {
            StringBuilder output = new StringBuilder();
            output
                .AppendLine(ConverterName)
                .AppendLine();

            if (!string.IsNullOrEmpty(InputResources))
            {
                var resources = WOLF_AbstractPartModule.ParseRecipeIngredientList(InputResources);

                output.AppendLine("<color=#99FF00>Inputs:</color>");
                foreach (var resource in resources)
                {
                    output
                        .Append(" - WOLF ")
                        .Append(resource.Key)
                        .Append(": ")
                        .AppendLine(resource.Value.ToString());
                }
            }
            if (outputList.Count > 0)
            {
                output.AppendLine("<color=#99FF00>Outputs:</color>");
                foreach (var resource in outputList)
                {
                    output
                        .Append(" - ")
                        .Append(resource.ResourceName)
                        .Append(": ");

                    if (resource.ResourceName == "ElectricCharge")
                        output
                            .AppendFormat("{0:F2}/sec", resource.Ratio)
                            .AppendLine();
                    else
                        output
                            .AppendFormat("{0:F2}/day", resource.Ratio * KSPUtil.dateTimeFormatter.Day)
                            .AppendLine();
                }
            }

            return output.ToString();
        }
    }
}
