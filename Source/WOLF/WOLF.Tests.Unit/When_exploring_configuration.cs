using Xunit;

namespace WOLF.Tests.Unit
{
    public class When_exploring_configuration
    {
        [Fact]
        public void Has_default_harvestable_resources()
        {
            Assert.NotEmpty(Configuration.DefaultHarvestableResources);
            Assert.NotEmpty(Configuration.DefaultHarvestableResources);
        }

        [Fact]
        public void Can_parse_resource_list()
        {
            var configuration = new Configuration();

            var resources = Configuration.ParseHarvestableResources(",Ore  , Oxygen,");

            Assert.NotNull(resources);
            Assert.NotEmpty(resources);
            Assert.Equal(2, resources.Count);
            Assert.Contains(resources, r => r == "Ore");
            Assert.Contains(resources, r => r == "Oxygen");
        }

        [Fact]
        public void Can_set_harvestable_resources()
        {
            var configuration = new Configuration();

            configuration.SetHarvestableResources("Ore,Oxygen");
            var resources = configuration.AllowedHarvestableResources;

            Assert.NotNull(resources);
            Assert.NotEmpty(resources);
            Assert.Equal(2, resources.Count);
            Assert.Contains(resources, r => r == "Ore");
            Assert.Contains(resources, r => r == "Oxygen");
        }

        [Fact]
        public void Can_blacklist_resources_on_homeworld()
        {
            var configuration = new Configuration();

            configuration.SetHarvestableResources("ExoticMinerals,Ore");
            configuration.SetBlacklistedHomeworldResources("ExoticMinerals");
            var resources = configuration.AllowedHarvestableResourcesOnHomeworld;

            Assert.NotNull(resources);
            Assert.NotEmpty(resources);
            Assert.Single(resources);
            Assert.Contains(resources, r => r == "Ore");
            Assert.DoesNotContain(resources, r => r == "ExoticMinerals");
        }
    }
}
