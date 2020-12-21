using System.Collections.Generic;

namespace WOLF
{
    public class WOLF_CrewModule : VesselModule
    {
        private static readonly int REQUIRED_LIFE_SUPPORT = 1;
        private static readonly int REQUIRED_HABITATION = 1;
        private static readonly int CO2_OUTPUT = 0;
        private static readonly int MULCH_OUTPUT = 0;
        private static readonly int WASTEWATER_OUTPUT = 0;
        private static readonly string RESOURCE_NAME_LIFESUPPORT = "LifeSupport";
        private static readonly string RESOURCE_NAME_HABITATION = "Habitation";
        private static readonly string RESOURCE_NAME_CO2 = "CarbonDioxide";
        private static readonly string RESOURCE_NAME_MULCH = "Mulch";
        private static readonly string RESOURCE_NAME_WASTEWATER = "WasteWater";

        public static readonly string CREW_RESOURCE_SUFFIX = "CrewPoint";
        public static readonly int CREW_RESOURCE_MULTIPLIER = 2;

        public IRecipe GetCrewRecipe()
        {
            return GetCrewRecipe(vessel.GetVesselCrew());
        }

        public static IRecipe GetCrewRecipe(List<ProtoCrewMember> roster)
        {
            if (roster.Count < 1)
                return new Recipe();

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
                inputs[RESOURCE_NAME_LIFESUPPORT] += REQUIRED_LIFE_SUPPORT;
                inputs[RESOURCE_NAME_HABITATION] += REQUIRED_HABITATION;

                outputs[RESOURCE_NAME_CO2] += CO2_OUTPUT;
                outputs[RESOURCE_NAME_MULCH] += MULCH_OUTPUT;
                outputs[RESOURCE_NAME_WASTEWATER] += WASTEWATER_OUTPUT;

                var resourceName = kerbal.trait + CREW_RESOURCE_SUFFIX;
                var stars = kerbal.experienceLevel * CREW_RESOURCE_MULTIPLIER;
                if (stars < 1)
                {
                    stars = 1;
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
