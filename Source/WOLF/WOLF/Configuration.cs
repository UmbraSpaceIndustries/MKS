using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WOLF
{
    public class ConfigurationFromFile
    {
        public string AllowedHarvestableResources { get; set; }
        public string BlacklistedHomeworldResources { get; set; }
        public string RefinedResourcesFilter { get; set; }
        public string AssembledResourcesFilter { get; set; }
        public string LifeSupportResourcesFilter { get; set; }
        public int CrewRequiredLifeSupport { get; set; }
        public int CrewRequiredHabitation { get; set; }
        public int CrewCO2Output { get; set; }
        public int CrewMulchOutput { get; set; }
        public int CrewWasteWaterOutput { get; set; }
        public int CrewMinimumEffectiveStars { get; set; }
        public int CrewStarMultiplier { get; set; }
    }

    public class Configuration
    {
        private static readonly Regex _sanitizeRegex = new Regex(@"\s+");

        public static readonly List<string> DefaultHarvestableResources = new List<string>
        {
            "Dirt", "ExoticMinerals", "Gypsum", "Hydrates", "MetallicOre", "Minerals",
            "Ore", "Oxygen", "RareMetals", "Silicates", "Substrate", "Water", "XenonGas"
        };

        public static readonly List<string> DefaultRefinedResources = new List<string>
        {
            "Chemicals", "Fertilizer", "Fuel", "Metals", "Polymers", "RefinedExotics", "Silicon"
        };

        public static readonly List<string> DefaultAssembledResources = new List<string>
        {
            "Alloys", "Electronics", "Machinery", "Maintenance", "MaterialKits",
            "Prototypes", "Robotics", "SpecializedParts", "Synthetics", "TransportCredits"
        };

        public static readonly List<string> DefaultLifeSupportResources = new List<string>
        {
            "CarbonDioxide", "Food", "Habitation", "Lab", "LifeSupport", "Mulch",
            "Oxygen", "Power", "WasteWater", "Water"
        };

        public static readonly int DefaultCrewRequiredLifeSupport = 1;
        public static readonly int DefaultCrewRequiredHabitation = 1;
        public static readonly int DefaultCrewCO2Output = 0;
        public static readonly int DefaultCrewMulchOutput = 0;
        public static readonly int DefaultCrewWasteWaterOutput = 0;
        public static readonly int DefaultCrewMinimumEffectiveStars = 1;
        public static readonly int DefaultCrewStarMultiplier = 2;

        private List<string> _allowedHarvestableResources;
        private List<string> _allowedHarvestableResourcesOnHomeworld;
        private List<string> _blacklistedHomeworldResources;
        private List<string> _refinedResourcesFilter;
        private List<string> _assembledResourcesFilter;
        private List<string> _lifeSupportResourcesFilter;

        private int _crewRequiredLifeSupport = -1;
        private int _crewRequiredHabitation = -1;
        private int _crewCO2Output = -1;
        private int _crewMulchOutput = -1;
        private int _crewWasteWaterOutput = -1;
        private int _crewMinimumEffectiveStars = -1;
        private int _crewStarMultiplier = -1;

        public List<string> AllowedHarvestableResources
        {
            get
            {
                if (_allowedHarvestableResources == null || _allowedHarvestableResources.Count < 1)
                {
                    _allowedHarvestableResources = DefaultHarvestableResources;
                }

                return _allowedHarvestableResources;
            }
        }

        public List<string> AllowedHarvestableResourcesOnHomeworld
        {
            get
            {
                if (_allowedHarvestableResourcesOnHomeworld == null)
                {
                    _allowedHarvestableResourcesOnHomeworld = AllowedHarvestableResources
                        .Where(r => !_blacklistedHomeworldResources.Contains(r))
                        .ToList();
                }

                return _allowedHarvestableResourcesOnHomeworld;
            }
        }

        public List<string> RefinedResourcesFilter
        {
            get
            {
                if (_refinedResourcesFilter == null)
                {
                    _refinedResourcesFilter = DefaultRefinedResources;
                }

                return _refinedResourcesFilter;
            }
        }

        public List<string> AssembledResourcesFilter
        {
            get
            {
                if (_assembledResourcesFilter == null)
                {
                    _assembledResourcesFilter = DefaultAssembledResources;
                }

                return _assembledResourcesFilter;
            }
        }

        public List<string> LifeSupportResourcesFilter
        {
            get
            {
                if (_lifeSupportResourcesFilter == null)
                {
                    _lifeSupportResourcesFilter = DefaultLifeSupportResources;
                }

                return _lifeSupportResourcesFilter;
            }
        }

        public int CrewRequiredLifeSupport
        {
            get
            {
                if (_crewRequiredLifeSupport < 0)
                {
                    _crewRequiredLifeSupport = DefaultCrewRequiredLifeSupport;
                }
                return _crewRequiredLifeSupport;
            }
        }

        public int CrewRequiredHabitation
        {
            get
            {
                if (_crewRequiredHabitation < 0)
                {
                    _crewRequiredHabitation = DefaultCrewRequiredHabitation;
                }
                return _crewRequiredHabitation;
            }
        }

        public int CrewCO2Output
        {
            get
            {
                if (_crewCO2Output < 0)
                {
                    _crewCO2Output = DefaultCrewCO2Output;
                }
                return _crewCO2Output;
            }
        }

        public int CrewMulchOutput
        {
            get
            {
                if (_crewMulchOutput < 0)
                {
                    _crewMulchOutput = DefaultCrewMulchOutput;
                }
                return _crewMulchOutput;
            }
        }

        public int CrewWasteWaterOutput
        {
            get
            {
                if (_crewWasteWaterOutput < 0)
                {
                    _crewWasteWaterOutput = DefaultCrewWasteWaterOutput;
                }
                return _crewWasteWaterOutput;
            }
        }

        public int CrewMinimumEffectiveStars
        {
            get
            {
                if (_crewMinimumEffectiveStars < 0)
                {
                    _crewMinimumEffectiveStars = DefaultCrewMinimumEffectiveStars;
                }
                return _crewMinimumEffectiveStars;
            }
        }

        public int CrewStarMultiplier
        {
            get
            {
                if (_crewStarMultiplier < 0)
                {
                    _crewStarMultiplier = DefaultCrewStarMultiplier;
                }
                return _crewStarMultiplier;
            }
        }

        public static List<string> ParseHarvestableResources(string resources)
        {
            if (string.IsNullOrEmpty(resources))
            {
                return new List<string>();
            }
            else
            {
                var sanitizedList = _sanitizeRegex.Replace(resources, string.Empty);
                var tokens = sanitizedList.Split(',');
                return tokens
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();
            }
        }

        public void SetAssembledResourcesFilter(List<string> resources)
        {
            if (resources != null)
            {
                _assembledResourcesFilter = resources
                    .Distinct()
                    .OrderBy(r => r)
                    .ToList();
            }
        }

        public void SetAssembledResourcesFilter(string resourceList)
        {
            _assembledResourcesFilter = ParseHarvestableResources(resourceList);
        }

        public void SetBlacklistedHomeworldResources(List<string> resources)
        {
            if (resources == null)
            {
                _blacklistedHomeworldResources = new List<string>();
            }
            else
            {
                _blacklistedHomeworldResources = resources
                    .Distinct()
                    .OrderBy(r => r)
                    .ToList();
            }
        }

        public void SetBlacklistedHomeworldResources(string resourceList)
        {
            _blacklistedHomeworldResources = ParseHarvestableResources(resourceList);
        }

        public void SetHarvestableResources(List<string> resources)
        {
            if (resources != null)
            {
                _allowedHarvestableResources = resources
                    .Distinct()
                    .OrderBy(r => r)
                    .ToList();
            }
        }

        public void SetHarvestableResources(string resourceList)
        {
            _allowedHarvestableResources = ParseHarvestableResources(resourceList);
        }

        public void SetLifeSupportResourcesFilter(List<string> resources)
        {
            if (resources != null)
            {
                _lifeSupportResourcesFilter = resources
                    .Distinct()
                    .OrderBy(r => r)
                    .ToList();
            }
        }

        public void SetLifeSupportResourcesFilter(string resourceList)
        {
            _lifeSupportResourcesFilter = ParseHarvestableResources(resourceList);
        }

        public void SetRefinedResourcesFilter(List<string> resources)
        {
            if (resources != null)
            {
                _refinedResourcesFilter = resources
                    .Distinct()
                    .OrderBy(r => r)
                    .ToList();
            }
        }

        public void SetRefinedResourcesFilter(string resourceList)
        {
            _refinedResourcesFilter = ParseHarvestableResources(resourceList);
        }

        public void SetCrewRequiredLifeSupport(int lifeSupport)
        {
            _crewRequiredLifeSupport = lifeSupport;
        }

        public void SetCrewRequiredHabitation(int habitation)
        {
            _crewRequiredHabitation = habitation;
        }

        public void SetCrewCO2Output(int co2Output)
        {
            _crewCO2Output = co2Output;
        }

        public void SetCrewMulchOutput(int mulchOutput)
        {
            _crewMulchOutput = mulchOutput;
        }

        public void SetCrewWasteWaterOutput(int wasteWaterOutput)
        {
            _crewWasteWaterOutput = wasteWaterOutput;
        }

        public void SetCrewMinimumEffectiveStars(int minimumEffectiveStars)
        {
            _crewMinimumEffectiveStars = minimumEffectiveStars;
        }

        public void SetCrewStarMultiplier(int resourceMultiplier)
        {
            _crewStarMultiplier = resourceMultiplier;
        }
    }
}
