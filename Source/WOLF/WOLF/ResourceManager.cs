using System;
using System.Collections.Generic;
using System.Linq;

namespace WOLF
{
    public static class ResourceManager
    {
        public const int RESOURCE_ABUNDANCE_CEILING = 1000;
        public const int RESOURCE_ABUNDANCE_FLOOR = 1;
        public const double RESOURCE_ABUNDANCE_MULTIPLIER = 100d;
        public const double RESOURCE_ABUNDANCE_RADIUS_MULT = 250d;

        public static Dictionary<string, int> GetResourceAbundance(
            int bodyIndex,
            double altitude,
            double latitude,
            double longitude,
            HarvestTypes[] harvestTypes,
            Configuration config,
            bool forHomeworld = false)
        {
            if (config == null)
            {
                config = new Configuration();
            }

            if (config.AllowedHarvestableResources == null
                || config.AllowedHarvestableResources.Count < 1)
            {
                config.SetHarvestableResources("");
            }

            var allowedResources = forHomeworld
                ? config.AllowedHarvestableResourcesOnHomeworld
                : config.AllowedHarvestableResources;
            var abundanceRequest = new AbundanceRequest
            {
                Altitude = altitude,
                BodyId = bodyIndex,
                CheckForLock = false,
                Latitude = latitude,
                Longitude = longitude
            };
            var radiusMultiplier = Math.Sqrt(FlightGlobals.Bodies[bodyIndex].Radius) / RESOURCE_ABUNDANCE_RADIUS_MULT;
            var bodyMultiplier = 5;
            if (!FlightGlobals.currentMainBody.isHomeWorld)
            {
                bodyMultiplier = Math.Max(5, WOLF_GameParameters.ResourceAbundanceMultiplierValue);
            }

            var resourceList = new Dictionary<string, int>();
            foreach (var harvestType in harvestTypes.Distinct())
            {
                abundanceRequest.ResourceType = harvestType;

                var harvestableResources = ResourceMap.Instance.FetchAllResourceNames(harvestType);
                foreach (var resource in harvestableResources)
                {
                    abundanceRequest.ResourceName = resource;

                    var baseAbundance = ResourceMap.Instance.GetAbundance(abundanceRequest);
                    int abundance = (int)Math.Round(
                        baseAbundance * RESOURCE_ABUNDANCE_MULTIPLIER * radiusMultiplier * bodyMultiplier,
                        MidpointRounding.AwayFromZero
                    );
                    if (abundance > RESOURCE_ABUNDANCE_FLOOR)
                    {
                        abundance = Math.Min(abundance, RESOURCE_ABUNDANCE_CEILING);
                    }
                    else
                    {
                        abundance = 0;
                    }

                    // Make abundance a multiple of 5
                    if (abundance > 0 && abundance % 5 != 0)
                    {
                        abundance = 5 * (int)Math.Round(abundance / 5d, MidpointRounding.AwayFromZero);
                    }

                    if (allowedResources.Contains(resource))
                    {
                        var wolfResourceName = resource + WOLF_DepotModule.HARVESTABLE_RESOURCE_SUFFIX;
                        if (resourceList.ContainsKey(wolfResourceName))
                        {
                            resourceList[wolfResourceName] += abundance;
                        }
                        else
                        {
                            resourceList.Add(wolfResourceName, abundance);
                        }
                    }
                }
            }

            return resourceList;
        }
    }
}
