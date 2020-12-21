using System.Collections.Generic;
using System.Linq;
using WOLF.Tests.Unit.Mocks;
using Xunit;

namespace WOLF.Tests.Unit
{
    public class When_exploring_the_depot_registry
    {
        [Fact]
        public void Can_add_depot_to_registry()
        {
            var registry = new TestPersister();
            var expectedBody = "Mun";
            var expectedBiome = "East Crater";

            var depot = registry.CreateDepot(expectedBody, expectedBiome);

            Assert.Contains(depot, registry.Depots);
        }

        [Fact]
        public void Can_find_a_depot()
        {
            var registry = new TestPersister();
            var expectedBody = "Mun";
            var expectedBiome = "East Crater";
            var createdDepot = registry.CreateDepot(expectedBody, expectedBiome);
            createdDepot.Establish();

            var hasDepot = registry.HasEstablishedDepot(expectedBody, expectedBiome);
            var depot = registry.GetDepot(expectedBody, expectedBiome);

            Assert.True(hasDepot);
            Assert.Equal(expectedBody, depot.Body);
            Assert.Equal(expectedBiome, depot.Biome);
        }

        [Fact]
        public void Can_find_all_depots()
        {
            var registry = new TestPersister();
            var expectedBody = "Mun";
            var expectedBiome1 = "East Crater";
            var expectedBiome2 = "Farside Crater";
            var expectedDepot1 = registry.CreateDepot(expectedBody, expectedBiome1);
            var expectedDepot2 = registry.CreateDepot(expectedBody, expectedBiome2);
            expectedDepot1.Establish();
            expectedDepot2.Establish();

            var depots = registry.GetDepots();

            // Should return a copy of its depot list, not its internal list
            // Note: XUnit's Equal assertion is List-aware, so we need to use the NotSame
            //       assertion to do a referential comparison.
            Assert.NotSame(registry.Depots, depots);
            Assert.Equal(registry.Depots, depots);
        }

        [Fact]
        public void Should_not_allow_multiple_depots_in_the_same_biome()
        {
            var registry = new TestPersister();
            var expectedBody = "Mun";
            var expectedBiome = "East Crater";
            var firstDepot = registry.CreateDepot(expectedBody, expectedBiome);

            var secondDepot = registry.CreateDepot(expectedBody, expectedBiome);

            Assert.Same(firstDepot, secondDepot);
            var depots = registry.Depots.Where(d => d.Body == expectedBody && d.Biome == expectedBiome);
            Assert.True(depots.Count() == 1);
        }
    }
}
