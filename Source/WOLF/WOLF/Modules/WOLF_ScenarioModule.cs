using System.Collections.Generic;
using USITools;

namespace WOLF
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION)]
    public class WOLF_ScenarioModule : ScenarioModule
    {
        private Configuration _configuration;
        public Configuration Configuration
        {
            get
            {
                return _configuration ?? (_configuration = GetConfiguration());
            }
        }

        public ServiceManager ServiceManager { get; private set; }

        private Configuration GetConfiguration()
        {
            var configNodes = GameDatabase.Instance.GetConfigNodes("WOLF_CONFIGURATION");
            var allowedResources = new List<string>();
            var blacklistedResources = new List<string>();
            var assembledResourcesFilter = new List<string>();
            var lifeSupportResourcesFilter = new List<string>();
            var refinedResourcesFilter = new List<string>();
            var crewRequiredLifeSupport = 1;
            var crewRequiredHabitation = 1;
            var crewCO2Output = 0;
            var crewMulchOutput = 0;
            var crewWasteWaterOutput = 0;
            var crewMinimumEffectiveStars = 1;
            var crewStarMultiplier = 2;

            foreach (var node in configNodes)
            {
                var configFromFile = ResourceUtilities.LoadNodeProperties<ConfigurationFromFile>(node);
                var harvestableResources = Configuration.ParseHarvestableResources(configFromFile.AllowedHarvestableResources);
                var blacklist = Configuration.ParseHarvestableResources(configFromFile.BlacklistedHomeworldResources);
                var assembledResources = Configuration.ParseHarvestableResources(configFromFile.AssembledResourcesFilter);
                var lifeSupportResources = Configuration.ParseHarvestableResources(configFromFile.LifeSupportResourcesFilter);
                var refinedResources = Configuration.ParseHarvestableResources(configFromFile.RefinedResourcesFilter);

                int newValue;
                if (node.HasValue("CrewRequiredLifeSupport") && int.TryParse(node.GetValue("CrewRequiredLifeSupport"), out newValue))
                {
                    crewRequiredLifeSupport = newValue;
                }
                if (node.HasValue("CrewRequiredHabitation") && int.TryParse(node.GetValue("CrewRequiredHabitation"), out newValue))
                {
                    crewRequiredHabitation = newValue;
                }
                if (node.HasValue("CrewCO2Output") && int.TryParse(node.GetValue("CrewCO2Output"), out newValue))
                {
                    crewCO2Output = newValue;
                }
                if (node.HasValue("CrewMulchOutput") && int.TryParse(node.GetValue("CrewMulchOutput"), out newValue))
                {
                    crewMulchOutput = newValue;
                }
                if (node.HasValue("CrewWasteWaterOutput") && int.TryParse(node.GetValue("CrewWasteWaterOutput"), out newValue))
                {
                    crewWasteWaterOutput = newValue;
                }
                if (node.HasValue("CrewMinimumEffectiveStars") && int.TryParse(node.GetValue("CrewMinimumEffectiveStars"), out newValue))
                {
                    crewMinimumEffectiveStars = newValue;
                }
                if (node.HasValue("CrewStarMultiplier") && int.TryParse(node.GetValue("CrewStarMultiplier"), out newValue))
                {
                    crewStarMultiplier = newValue;
                }

                allowedResources.AddRange(harvestableResources);
                blacklistedResources.AddRange(blacklist);
                assembledResourcesFilter.AddRange(assembledResources);
                lifeSupportResourcesFilter.AddRange(lifeSupportResources);
                refinedResourcesFilter.AddRange(refinedResources);
            }

            var config = new Configuration();
            config.SetHarvestableResources(allowedResources);
            config.SetBlacklistedHomeworldResources(blacklistedResources);
            config.SetAssembledResourcesFilter(assembledResourcesFilter);
            config.SetLifeSupportResourcesFilter(lifeSupportResourcesFilter);
            config.SetRefinedResourcesFilter(refinedResourcesFilter);
            config.SetCrewRequiredLifeSupport(crewRequiredLifeSupport);
            config.SetCrewRequiredHabitation(crewRequiredHabitation);
            config.SetCrewCO2Output(crewCO2Output);
            config.SetCrewMulchOutput(crewMulchOutput);
            config.SetCrewWasteWaterOutput(crewWasteWaterOutput);
            config.SetCrewMinimumEffectiveStars(crewMinimumEffectiveStars);
            config.SetCrewStarMultiplier(crewStarMultiplier);

            return config;
        }

        public override void OnAwake()
        {
            base.OnAwake();

            // Setup dependency injection for WOLF services
            var services = new ServiceCollection();
            services.AddSingletonService<IRegistryCollection, ScenarioPersister>();
            services.AddSingletonService<WOLF_GuiFilters>();
            services.AddService<WOLF_PlanningMonitor>();
            services.AddService<WOLF_RouteMonitor>();

            ServiceManager = new ServiceManager(services);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            var persister = ServiceManager.GetService<IRegistryCollection>();
            persister.OnLoad(node);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            var persister = ServiceManager.GetService<IRegistryCollection>();
            persister.OnSave(node);
        }
    }
}
