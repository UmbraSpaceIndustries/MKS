using System.Collections.Generic;
using WOLF.Tests.Unit.Mocks;
using Xunit;

namespace WOLF.Tests.Unit
{
    public class When_exploring_depots
    {
        [Fact]
        public void Should_have_no_resource_streams_by_default()
        {
            var depot = new TestDepot();

            Assert.NotNull(depot.Resources);
            Assert.Empty(depot.Resources);
        }

        [Fact]
        public void Should_not_be_established_by_default()
        {
            var depot = new TestDepot();

            Assert.False(depot.IsEstablished);
        }

        [Fact]
        public void Should_not_be_surveyed_by_default()
        {
            var depot = new TestDepot();

            Assert.False(depot.IsSurveyed);
        }

        [Fact]
        public void Can_establish_depot()
        {
            var depot = new TestDepot();

            depot.Establish();

            Assert.True(depot.IsEstablished);
        }

        [Fact]
        public void Can_survey_depot()
        {
            var depot = new TestDepot();

            depot.Survey();

            Assert.True(depot.IsSurveyed);
        }

        [Fact]
        public void Can_show_resource_streams()
        {
            var depot = new TestDepot();
            var resourceName = "ElectricCharge";
            var providedQuantity = 10;
            var requestedQuantity = 3;
            var expectedRemainingQuantity = 7;
            var providedResources = new Dictionary<string, int>
            {
                { resourceName, providedQuantity }
            };
            var consumedResources = new Dictionary<string, int>
            {
                { resourceName, requestedQuantity }
            };
            depot.NegotiateProvider(providedResources);
            depot.NegotiateConsumer(consumedResources);

            var streams = depot.GetResources();

            Assert.NotNull(streams);
            Assert.NotEmpty(streams);
            Assert.Contains(streams, s => s.ResourceName == resourceName
                && s.Incoming == providedQuantity
                && s.Outgoing == requestedQuantity
                && s.Available == expectedRemainingQuantity);
        }

        [Fact]
        public void Can_negotiate_a_provider_relationship()
        {
            var depot = new TestDepot();
            var expectedResource = "ElectricCharge";
            var expectedQuantity = 10;
            var providedResources = new Dictionary<string, int>
            {
                { expectedResource, expectedQuantity }
            };

            var result = depot.NegotiateProvider(providedResources);

            Assert.IsType<OkNegotiationResult>(result);
            Assert.NotEmpty(depot.Resources);
            var stream = depot.Resources[expectedResource];
            Assert.NotNull(stream);
            Assert.Equal(expectedResource, stream.ResourceName);
            Assert.Equal(expectedQuantity, stream.Incoming);
            Assert.Equal(expectedQuantity, stream.Available);
        }

        [Fact]
        public void Can_negotiate_a_consumer_relationship()
        {
            var depot = new TestDepot();
            var resourceName = "ElectricCharge";
            var providedQuantity = 10;
            var requestedQuantity = 3;
            var expectedRemainingQuantity = 7;
            var providedResources = new Dictionary<string, int>
            {
                { resourceName, providedQuantity }
            };
            var consumedResources = new Dictionary<string, int>
            {
                { resourceName, requestedQuantity }
            };
            depot.NegotiateProvider(providedResources);

            var result = depot.NegotiateConsumer(consumedResources);

            Assert.IsType<OkNegotiationResult>(result);
            var stream = depot.Resources[resourceName];
            Assert.Equal(requestedQuantity, stream.Outgoing);
            Assert.Equal(expectedRemainingQuantity, stream.Available);
        }

        [Fact]
        public void Can_negotiate_a_relationship_for_a_recipe()
        {
            var depot = new TestDepot();
            var consumedResource1 = "ElectricCharge";
            var consumedResource2 = "Ore";
            var providedResource1 = "LiquidFuel";
            var consumedQuantity1 = 10;
            var consumedQuantity2 = 10;
            var providedQuantity1 = 5;
            var startingResources = new Dictionary<string, int>
            {
                { consumedResource1, 15 },
                { consumedResource2, 10 }
            };
            depot.NegotiateProvider(startingResources);
            var recipe = new Recipe();
            recipe.InputIngredients.Add(consumedResource1, consumedQuantity1);
            recipe.InputIngredients.Add(consumedResource2, consumedQuantity2);
            recipe.OutputIngredients.Add(providedResource1, providedQuantity1);
            var expectedRemainingEC = 5;
            var expectedRemainingOre = 0;

            var result = depot.Negotiate(recipe);

            Assert.IsType<OkNegotiationResult>(result);
            var ecStream = depot.Resources[consumedResource1];
            var oreStream = depot.Resources[consumedResource2];
            var lfStream = depot.Resources[providedResource1];
            Assert.Equal(consumedQuantity1, ecStream.Outgoing);
            Assert.Equal(expectedRemainingEC, ecStream.Available);
            Assert.Equal(consumedQuantity2, oreStream.Outgoing);
            Assert.Equal(expectedRemainingOre, oreStream.Available);
            Assert.Equal(providedQuantity1, lfStream.Incoming);
            Assert.Equal(providedQuantity1, lfStream.Available);
        }

        [Fact]
        public void Can_negotiate_a_relationship_for_multiple_dependent_recipes()
        {
            var depot = new TestDepot();
            var consumedResource1 = "ElectricCharge";
            var providedQuantity1 = 20;
            var consumedQuantity1a = 8;
            var consumedQuantity1b = 10;
            var expectedRemaining1 = 2;
            var consumedResource2 = "Ore";
            var providedQuantity2 = 15;
            var consumedQuantity2 = 10;
            var expectedRemaining2 = 5;
            var providedResource3 = "LiquidFuel";
            var providedQuantity3 = 5;
            // Setup a 'refinery' recipe
            var recipe1 = new Recipe();
            recipe1.InputIngredients.Add(consumedResource1, consumedQuantity1b);
            recipe1.InputIngredients.Add(consumedResource2, consumedQuantity2);
            recipe1.OutputIngredients.Add(providedResource3, providedQuantity3);
            // Setup a 'drill' recipe
            var recipe2 = new Recipe();
            recipe2.InputIngredients.Add(consumedResource1, consumedQuantity1a);
            recipe2.OutputIngredients.Add(consumedResource2, providedQuantity2);
            // Setup a 'solar panel' recipe
            var recipe3 = new Recipe();
            recipe3.OutputIngredients.Add(consumedResource1, providedQuantity1);
            // Note: The recipes are arranged in this order to make sure we aren't
            //       accidentally passing the test.
            var recipes = new List<IRecipe>
            {
                recipe1,
                recipe2,
                recipe3
            };

            var result = depot.Negotiate(recipes);

            Assert.IsType<OkNegotiationResult>(result);
            Assert.Equal(3, depot.Resources.Count);
            var streams = depot.Resources;
            Assert.True(streams.ContainsKey(consumedResource1));
            Assert.True(streams.ContainsKey(consumedResource2));
            Assert.True(streams.ContainsKey(providedResource3));
            var resource1 = streams[consumedResource1];
            Assert.Equal(expectedRemaining1, resource1.Available);
            var resource2 = streams[consumedResource2];
            Assert.Equal(expectedRemaining2, resource2.Available);
            var resource3 = streams[providedResource3];
            Assert.Equal(providedQuantity3, resource3.Available);
        }

        [Theory]
        [InlineData("Ore", 1, 2, true)]
        [InlineData("Ore", 0, 2, false)]
        public void Should_not_allow_consumer_negotiation_if_resources_are_not_available(string resourceName, int availableQuantity, int requestedQuantity, bool resourceExists)
        {
            var depot = new TestDepot();
            if (resourceExists)
            {
                var providedResources = new Dictionary<string, int>
                {
                    { resourceName, availableQuantity }
                };
                depot.NegotiateProvider(providedResources);
            }
            var consumedResources = new Dictionary<string, int>
            {
                { resourceName, requestedQuantity }
            };

            var result = depot.NegotiateConsumer(consumedResources);

            var failedResult = Assert.IsType<FailedNegotiationResult>(result);
            Assert.Contains(failedResult.MissingResources, r => r.Key == resourceName && r.Value == requestedQuantity - availableQuantity);
        }
    }
}
