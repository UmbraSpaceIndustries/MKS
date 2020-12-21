using System.Linq;

namespace WOLF
{
    [KSPModule("Harvester")]
    public class WOLF_HarvesterModule : WOLF_ConverterModule
    {
        private int CalculateAbundance(string resourceName)
        {
            return 1;
        }

        public override string GetInfo()
        {
            if (part.FindModulesImplementing<WOLF_RecipeOption>().Any())
            {
                return string.Empty;
            }
            else
            {
                return base.GetInfo();
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (WolfRecipe.OutputIngredients.Count > 0)
            {
                var resources = new string[WolfRecipe.OutputIngredients.Count];
                WolfRecipe.OutputIngredients.Keys.CopyTo(resources, 0);
                foreach (var resource in resources)
                {
                    var abundance = CalculateAbundance(resource);

                    WolfRecipe.OutputIngredients[resource] *= abundance;
                }
            }
        }
    }
}
