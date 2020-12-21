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
            "Machinery", "Maintenance", "MaterialKits", "SpecializedParts", "TransportCredits"
        };

        public static readonly List<string> DefaultLifeSupportResources = new List<string>
        {
            "CarbonDioxide", "Food", "Habitation", "Lab", "LifeSupport", "Mulch",
            "Oxygen", "Power", "WasteWater", "Water"
        };

        private List<string> _allowedHarvestableResources;
        private List<string> _allowedHarvestableResourcesOnHomeworld;
        private List<string> _blacklistedHomeworldResources;
        private List<string> _refinedResourcesFilter;
        private List<string> _assembledResourcesFilter;
        private List<string> _lifeSupportResourcesFilter;

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
    }
}
