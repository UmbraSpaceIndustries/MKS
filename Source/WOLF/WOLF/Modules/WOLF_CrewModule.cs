using System.Collections.Generic;

namespace WOLF
{
    public class WOLF_CrewModule : VesselModule
    {
        protected static WOLF_ScenarioModule _scenario;

        private static readonly string RESOURCE_NAME_LIFESUPPORT = "LifeSupport";
        private static readonly string RESOURCE_NAME_HABITATION = "Habitation";
        private static readonly string RESOURCE_NAME_CO2 = "CarbonDioxide";
        private static readonly string RESOURCE_NAME_MULCH = "Mulch";
        private static readonly string RESOURCE_NAME_WASTEWATER = "WasteWater";

        public static readonly string CREW_RESOURCE_SUFFIX = "CrewPoint";

        public IRecipe GetCrewRecipe()
        {
            return GetCrewRecipe(vessel.GetVesselCrew());
        }

        public static IRecipe GetCrewRecipe(List<ProtoCrewMember> roster)
        {
            if (roster.Count < 1)
                return new Recipe();
            _scenario = FindObjectOfType<WOLF_ScenarioModule>();
            var inputs = new Dictionary<string, int>
            {
                { RESOURCE_NAME_LIFESUPPORT, 0 },
                { RESOURCE_NAME_HABITATION, 0 }
            };
            var outputs = new Dictionary<string, int>
            {
                { RESOURCE_NAME_CO2, 0 },
                { RESOURCE_NAME_MULCH, 0 },
                { RESOURCE_NAME_WASTEWATER, 0 }
            };
            foreach (var kerbal in roster)
            {
                inputs[RESOURCE_NAME_LIFESUPPORT] += _scenario.Configuration.CrewRequiredLifeSupport;
                inputs[RESOURCE_NAME_HABITATION] += _scenario.Configuration.CrewRequiredHabitation;

                outputs[RESOURCE_NAME_CO2] += _scenario.Configuration.CrewCO2Output;
                outputs[RESOURCE_NAME_MULCH] += _scenario.Configuration.CrewMulchOutput;
                outputs[RESOURCE_NAME_WASTEWATER] += _scenario.Configuration.CrewWasteWaterOutput;

                var resourceName = kerbal.trait + CREW_RESOURCE_SUFFIX;
                var stars = kerbal.experienceLevel * _scenario.Configuration.CrewStarMultiplier;
                if (stars < _scenario.Configuration.CrewMinimumEffectiveStars)
                {
                    stars = _scenario.Configuration.CrewMinimumEffectiveStars;
                }
                if (!outputs.ContainsKey(resourceName))
                {
                    outputs.Add(resourceName, stars);
                }
                else
                {
                    outputs[resourceName] += stars;
                }
            }

            return new Recipe(inputs, outputs);
        }

        /// <summary>
        /// Determine if crew members are eligible for WOLF
        /// </summary>
        /// <returns></returns>
        public bool IsCrewEligible()
        {
            var roster = vessel.GetVesselCrew();
            if (roster.Count < 1)
                return true;

            //return !roster.Any(c => c.experienceLevel < 1);
            return true;
        }
    }
}
